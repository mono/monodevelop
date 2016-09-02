﻿//#define DEBUG_MESSAGES

using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

using System.Linq;
using System.Threading.Tasks;
using System.Text;
using MonoDevelop.Core.Assemblies;
using System.Reflection;

namespace MonoDevelop.Core.Execution
{
	public class RemoteProcessConnection: IDisposable
	{
		bool initializationDone;
		TaskCompletionSource<bool> processConnectedEvent = new TaskCompletionSource<bool> ();
		ManualResetEvent processDisconnectedEvent = new ManualResetEvent (false);
		ProcessAsyncOperation process;
		ConnectionStatus status;
		bool disposed;
		CancellationTokenSource mainCancelSource;
		List<BinaryMessage> messageQueue = new List<BinaryMessage> ();
		Dictionary<string, Type> messageTypes = new Dictionary<string, Type> ();

		List<MessageListener> listeners = new List<MessageListener> ();
		object listenersLock = new object ();

		string exePath;

		// This class will ping the remote process every PingPeriod milliseconds
		// If the remote process doesn't get a message in PingPeriod*2 it assumes
		// that XS has died and shutdowns itself.
		#if DEBUG_MESSAGES
		const int PingPeriod = 5000000;
		#else
		const int PingPeriod = 5000;
		#endif

		Timer pinger;
		object pingerLock = new object ();

		// Time this connection will wait for the process to connect back
		const int ProcessInitializationTimeout = 15000;

		#if DEBUG_MESSAGES
		internal static bool DebugMode = true;
		#else
		internal static bool DebugMode = false;
		#endif

		TcpListener listener;
		TcpClient connection;
		Stream connectionStream;
		SynchronizationContext syncContext;
		IExecutionHandler executionHandler;
		OperationConsole console;
		ConcurrentExclusiveSchedulerPair schedulerPair = new ConcurrentExclusiveSchedulerPair ();

		public event EventHandler<MessageEventArgs> MessageReceived;
		public event EventHandler StatusChanged;

		public RemoteProcessConnection (string exePath, IExecutionHandler executionHandler = null, OperationConsole console = null, SynchronizationContext syncContext = null)
		{
			if (executionHandler == null)
				executionHandler = Runtime.ProcessService.DefaultExecutionHandler;
			if (console == null)
				console = new ProcessHostConsole ();
			this.executionHandler = executionHandler;
			this.exePath = exePath;
			this.syncContext = syncContext;
			this.console = console;
			mainCancelSource = new CancellationTokenSource ();
		}

		public ConnectionStatus Status {
			get { return status; }
		}

		/// <summary>
		/// If true, the remote process is either connected or connecting
		/// </summary>
		public bool IsReachable {
			get {
				var s = status;
				return s != ConnectionStatus.ConnectionFailed && s != ConnectionStatus.Disconnected;
			}
		}

		public string StatusMessage { get; private set; }
		public Exception StatusException { get; private set; }

		internal IMessageInterceptor Interceptor {
			get;
			set;
		}

		void SetStatus (ConnectionStatus s, string message, Exception e = null)
		{
			status = s;
			StatusMessage = message;
			StatusException = e;
			var se = StatusChanged;
			if (se != null)
				se (this, EventArgs.Empty);
		}

		void PostSetStatus (ConnectionStatus s, string message, Exception e = null)
		{
			if (syncContext != null) {
				syncContext.Post (delegate {
					SetStatus (s, message, e);
				}, null);
			} else {
				SetStatus (s, message, e);
			}
		}

		public void RegisterMessageTypes (params Type[] types)
		{
			foreach (var t in types) {
				var a = (MessageDataTypeAttribute) Attribute.GetCustomAttribute (t, typeof (MessageDataTypeAttribute));
				if (a != null) {
					var name = a.Name ?? t.FullName;
					messageTypes [name] = t;
				}
			}
		}

		public void Dispose ()
		{
			Disconnect (false);
		}

		public void AddListener (MessageListener listener)
		{
			AddListener ((object)listener);
		}

		public void AddListener (object listener)
		{
			lock (listenersLock) {
				var lis = listener as MessageListener;
				if (lis == null)
					lis = new MessageListener (listener);

				var newList = new List<MessageListener> (listeners);
				newList.Add (lis);
				RegisterMessageTypes (lis.GetMessageTypes ());
				listeners = newList;
			}
		}

		public void Disconnect (bool waitUntilDone)
		{
			mainCancelSource.Cancel ();
			mainCancelSource = new CancellationTokenSource ();

			try {
				StopRemoteProcess ();
			} catch {
				// Ignore
			}
			if (waitUntilDone)
				processDisconnectedEvent.WaitOne (TimeSpan.FromSeconds (7));
		}

		public Task Connect ()
		{
			initializationDone = false;
			AbortPendingMessages ();
			if (listener != null && !disposed) {
				// Disconnect the current session and reconnect
				Disconnect (true);
			}
			return StartConnecting ();
		}

		Task StartConnecting ()
		{
			processDisconnectedEvent.Reset ();
			disposed = false;
			SetStatus (ConnectionStatus.Connecting, "Connecting");
			return DoConnect (mainCancelSource.Token);
		}

		async Task DoConnect (CancellationToken token)
		{
			if (disposed)
				return;

			try {
				if (listener != null) {
					listener.Stop ();
					listener = null;
				}

				listener = new TcpListener (IPAddress.Loopback, 0);
				listener.Start ();

				processConnectedEvent = new TaskCompletionSource<bool> ();

				listener.BeginAcceptTcpClient (OnConnected, listener);

				await InitializeRemoteProcessAsync (token).ConfigureAwait (false);
			} catch (Exception ex) {
				HandleRemoteConnectException (ex, token);
			}
		}

		async Task InitializeRemoteProcessAsync (CancellationToken token)
		{
			try {
				await StartRemoteProcess ().ConfigureAwait (false);

				token.ThrowIfCancellationRequested ();

				if (disposed)
					throw new Exception ("Could not start process");
				
				var timeout = Task.Delay (ProcessInitializationTimeout, token).ContinueWith (t => {
					if (t.IsCanceled)
						return;
					if (!processConnectedEvent.Task.IsCompleted)
						processConnectedEvent.SetException (new Exception ("Could not start process"));
				});

				await Task.WhenAny (timeout, processConnectedEvent.Task).ConfigureAwait (false);

				if (connectionStream == null || disposed)
					throw new Exception ("Process failed to start");

/*				var msg = new BinaryMessage ("Initialize", "Process").AddArgument ("MessageWaitTimeout", PingPeriod * 2);
				msg.BypassConnection = true;
				var cs = new TaskCompletionSource<BinaryMessage> ();
				PostMessage (msg, cs, false);
				token.ThrowIfCancellationRequested ();
				await cs.Task;
*/
				token.ThrowIfCancellationRequested ();

				SetStatus (ConnectionStatus.Connected, "Connected");
				initializationDone = true;
			
			} catch (Exception ex) {
				HandleRemoteConnectException (ex, token);
			}
		}

		void HandleRemoteConnectException (Exception ex, CancellationToken token)
		{
			LoggingService.LogError ("Connection failed", ex);
			token.ThrowIfCancellationRequested ();
			StopRemoteProcess ();
			SetStatus (ConnectionStatus.ConnectionFailed, ex.Message, ex);
			processDisconnectedEvent.Set ();
		}

		Task StartRemoteProcess ()
		{
			return Task.Run (() => {
				var cmd = Runtime.ProcessService.CreateCommand (exePath);
				cmd.Arguments = ((IPEndPoint)listener.LocalEndpoint).Port + " " + DebugMode;
				process = executionHandler.Execute (cmd, console);
				process.Task.ContinueWith (t => ProcessExited ());
			});
		}

		bool stopping;
		void ProcessExited ()
		{
			if (!stopping)
				AbortConnection (isAsync: true);
			processDisconnectedEvent.Set ();
		}

		public async Task<RT> SendMessage<RT> (BinaryMessage<RT> message) where RT:BinaryMessage
		{
			return (RT) await SendMessage ((BinaryMessage) message);
		}

		public Task<BinaryMessage> SendMessage (BinaryMessage message)
		{
			message.ReadCustomData ();
			var interceptor = Interceptor;
			if (interceptor != null && !interceptor.PreProcessMessage (message))
				return Task.FromResult (message.CreateErrorResponse ("Message was refused by interceptor"));

			var cs = new TaskCompletionSource<BinaryMessage> ();
			PostMessage (message, cs, true);
			return cs.Task;
		}

		public void PostMessage (BinaryMessage message)
		{
			message.ReadCustomData ();
			var interceptor = Interceptor;
			if (interceptor != null && !interceptor.PreProcessMessage (message))
				return;

			PostMessage (message, null, true);
		}

		public void FlushMessages (string target)
		{
			var msg = new BinaryMessage ("FlushMessages", target);
			SendMessage (msg);
		}

		void PostMessage (BinaryMessage message, TaskCompletionSource<BinaryMessage> cs, bool checkInitialized)
		{
			if (checkInitialized && !initializationDone)
				throw new RemoteProcessException ("Not connected");

			if (cs != null) {
				lock (messageWaiters) {
					messageWaiters [message.Id] = new MessageRequest {
						Request = message,
						TaskSource = cs
					};
				}
			}

			lock (messageQueue) {
				messageQueue.Add (message);
				if (!senderRunning) {
					senderRunning = true;
					ThreadPool.QueueUserWorkItem (delegate {
						SendMessages ();
					});
				}
			}
		}

		bool senderRunning;

		void SendMessages ()
		{
			while (true) {
				List<BinaryMessage> queueCopy;
				lock (messageQueue) {
					if (messageQueue.Count == 0) {
						senderRunning = false;
						return;
					}
					queueCopy = new List<BinaryMessage> (messageQueue);
					messageQueue.Clear ();
				}
				foreach (var m in queueCopy)
					PostMessageInternal (m);
			}
		}

		void PostMessageInternal (BinaryMessage message)
		{
			if ((status != ConnectionStatus.Connected || disposed) && !message.BypassConnection) {
				ProcessResponse (message.CreateErrorResponse ("Connection is closed"));
				return;
			}

			try {
				if (DebugMode)
					message.SentTime = DateTime.Now;

				// Now send the message. This one will need a response

				if (DebugMode)
					LogMessage (MessageType.Request, message);

				connectionStream.WriteByte ((byte)RequestType.QueueEnd);
				message.Write (connectionStream);

				connectionStream.Flush ();
			}	
			catch (Exception ex){
				if (connection == null || (!connection.Connected && status == ConnectionStatus.Connected)) {
					AbortConnection ("Disconnected from remote process due to a communication error", isAsync: true);
				} else
					ProcessResponse (message.CreateErrorResponse (ex.ToString ()));
			}
		}

		class MessageRequest
		{
			public BinaryMessage Request;
			public TaskCompletionSource<BinaryMessage> TaskSource;
		}

		Dictionary<int, MessageRequest> messageWaiters = new Dictionary<int, MessageRequest> ();

		void AbortConnection (string message = null, bool isAsync = false)
		{
			if (message == null)
				message = "Disconnected from layout renderer";
			AbortPendingMessages ();
			disposed = true;
			processConnectedEvent.TrySetResult (true);
			if (isAsync)
				PostSetStatus (ConnectionStatus.Disconnected, message);
			else
				SetStatus (ConnectionStatus.Disconnected, message);
		}

		void AbortPendingMessages ()
		{
			lock (messageWaiters) {
				foreach (var m in messageWaiters.Values)
					NotifyResponse (m, m.Request.CreateErrorResponse ("Connection closed"));
				messageWaiters.Clear ();
				messageQueue.Clear ();
			}
		}

		void StopRemoteProcess (bool isAsync = false)
		{
			if (process != null)
				stopping = true;

			AbortConnection (isAsync: isAsync);

			if (pinger != null) {
				pinger.Dispose ();
				pinger = null;
			}
			if (listener != null) {
				listener.Stop ();
				listener = null;
			}
			if (connectionStream != null) {
				connectionStream.Close ();
				connectionStream = null;
			}
			if (connection != null) {
				connection.Close ();
				connection = null;
			}
			process.Cancel ();
		}

		void OnConnected (IAsyncResult res)
		{
			if (disposed)
				return;
			var tcpl = (TcpListener) res.AsyncState;
			if (tcpl == listener) {
				try {
					connection = listener.EndAcceptTcpClient (res);
					connectionStream = connection.GetStream ();
					pinger = new Timer (PingConnection, null, PingPeriod, PingPeriod);
				} catch (Exception ex) {
					LoggingService.LogError ("Connection to layout renderer failed", ex);
					PostSetStatus (ConnectionStatus.ConnectionFailed, "Connection to layout renderer failed");
					return;
				}

				ReadMessages ();
			}
		}

		async void ReadMessages ()
		{
			byte[] buffer = new byte [1];

			while (!disposed && connectionStream != null)
			{
				BinaryMessage msg;
				byte type;

				try {
					int nr = await connectionStream.ReadAsync (buffer, 0, 1, mainCancelSource.Token).ConfigureAwait (false);
					if (nr == 0) {
						StopRemoteProcess (isAsync: true);
						return;
					}
					type = buffer [0];
					msg = BinaryMessage.Read (connectionStream);
				} catch (Exception ex) {
					if (disposed)
						return;
					LoggingService.LogError ("ReadMessage failed", ex);
					StopRemoteProcess (isAsync: true);
					PostSetStatus (ConnectionStatus.ConnectionFailed, "Connection to layout renderer failed.");
					return;
				}

				lock (pendingMessageTasks) {
					var t = Task.Factory.StartNew (() => {
						msg = LoadMessageData (msg);
						if (type == 0)
							ProcessResponse (msg);
						else
							ProcessRemoteMessage (msg);
					}, mainCancelSource.Token, TaskCreationOptions.None, schedulerPair.ExclusiveScheduler);
					t.ContinueWith (ta => {
						lock(pendingMessageTasks)
							pendingMessageTasks.Remove (ta);
					});
					pendingMessageTasks.Add (t);
				}
			}
		}

		List<Task> pendingMessageTasks = new List<Task> ();

		public Task ProcessPendingMessages ()
		{
			return Task.WhenAll (pendingMessageTasks.ToArray ());
		}

		BinaryMessage LoadMessageData (BinaryMessage msg)
		{
			Type type;
			if (messageTypes.TryGetValue (msg.Name, out type)) {
				var res = (BinaryMessage)Activator.CreateInstance (type);
				res.CopyFrom (msg);
				return res;
			}
			return msg;
		}

		void ProcessResponse (BinaryMessage msg)
		{
			DateTime respTime = DateTime.Now;

			lock (messageWaiters) {
				MessageRequest req;
				if (messageWaiters.TryGetValue (msg.Id, out req)) {
					messageWaiters.Remove (msg.Id);
					try {
						var rt = req.Request.GetResponseType ();
						if (rt != typeof (BinaryMessage)) {
							var resp = (BinaryMessage)Activator.CreateInstance (rt);
							resp.CopyFrom (msg);
							msg = resp;
						}
					} catch (Exception ex) {
						msg = msg.CreateErrorResponse (ex.ToString ());
					}

					if (DebugMode) {
						var time = (int)(respTime - req.Request.SentTime).TotalMilliseconds;
						LogMessage (MessageType.Response, msg, time);
					}

					if (!req.Request.OneWay)
						NotifyResponse (req, msg);
				}
				else if (DebugMode)
					LogMessage (MessageType.Response, msg, -1);
			}
		}

		void NotifyResponse (MessageRequest req, BinaryMessage res)
		{
			if (disposed || res == null) {
				req.TaskSource.SetException (new Exception ("Connection closed"));
			}
			else if (res.Name == "Error") {
				string msg = res.GetArgument<string> ("Message");
				if (res.GetArgument<bool> ("IsInternal") && !string.IsNullOrEmpty (msg)) {
					msg = "The operation failed due to an internal error: " + msg + ".";
				}
				req.TaskSource.SetException (new RemoteProcessException (msg) { ExtendedDetails = res.GetArgument<string> ("Log") });
			} else {
				req.TaskSource.SetResult (res);
			}
		}

		void ProcessRemoteMessage (BinaryMessage msg)
		{
			if (DebugMode)
				LogMessage (MessageType.Message, msg);

			if (msg.Name == "Connect") {
				processConnectedEvent.SetResult (true);
				return;
			}

			Runtime.RunInMainThread (delegate {
				if (MessageReceived != null)
					MessageReceived (null, new MessageEventArgs () { Message = msg });
			});

			try {
				foreach (var li in listeners) {
					li.ProcessMessage (msg);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Exception in message invocation: " + msg, ex);
			}
		}

		enum MessageType { Request, Response, Message }

		long tickBase = Environment.TickCount;

		void LogMessage (MessageType type, BinaryMessage msg, int time = -1)
		{
			Console.Write ("[" + (Environment.TickCount - tickBase) + "] ");

			if (type == MessageType.Request)
				Console.WriteLine ("[CLIENT] XS >> RP " + msg);
			else if (type == MessageType.Response) {
				if (time != -1)
					Console.WriteLine ("[CLIENT] XS << RP " + time + "ms " + msg);
				else
					Console.WriteLine ("[CLIENT] XS << RP " + msg);
			}
			else
				Console.WriteLine ("[CLIENT] XS <- RP " + msg);
		}

		void PingConnection (object state)
		{
			bool lockTaken = false;
			try {
				Monitor.TryEnter (pingerLock, ref lockTaken);
				if (!lockTaken)
					return;
				var msg = new BinaryMessage ("Ping", "Process");
				SendMessage (msg);
			} catch (Exception ex) {
				LoggingService.LogError ("Connection ping failed", ex);
			} finally {
				if (lockTaken)
					Monitor.Exit (pingerLock);
			}
		}
	}

	public class MessageEventArgs: EventArgs
	{
		public BinaryMessage Message { get; set; }
	}

	public enum ConnectionStatus
	{
		Connecting,
		Connected,
		ConnectionFailed,
		Disconnected
	}

	enum RequestType
	{
		QueueEnd = 1,
		Queued = 2
	}

	internal interface IMessageInterceptor
	{
		/// <summary>
		/// Give a chance to an implementor to peek at messages before they are sent.
		/// </summary>
		/// <returns><c>true</c>, if message should sent, <c>false</c> if it should be discarded.</returns>
		bool PreProcessMessage (BinaryMessage message);
	}
}

