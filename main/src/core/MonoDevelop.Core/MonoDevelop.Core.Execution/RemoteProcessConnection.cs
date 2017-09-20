//#define DEBUG_MESSAGES

using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MonoDevelop.Core.Execution
{
	public class RemoteProcessConnection: IDisposable
	{
		bool initializationDone;
		TaskCompletionSource<bool> processConnectedEvent = new TaskCompletionSource<bool> ();
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

		#if !DEBUG_MESSAGES
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
				return process != null && s != ConnectionStatus.ConnectionFailed && s != ConnectionStatus.Disconnected;
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
			Log ($"status={s} message={message} exception={e?.Message}");
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
			Log ();
			Disconnect ().Ignore ();
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

		[Obsolete ("Use Disconnect()")]
		public void Disconnect (bool waitUntilDone)
		{
			if (waitUntilDone)
				Disconnect ().Wait (TimeSpan.FromSeconds (7));
			else
				Disconnect ().Ignore ();
		}

		public async Task Disconnect ()
		{
			Log ();
			StopPinger ();
		
			if (process == null)
				return;
			
			try {
				// Send a stop message to try a graceful stop. Don't wait more than 2s for a response
				var timeout = Task.Delay (2000);
				Log ("await Stop Process timeout 2000");
				if (await Task.WhenAny (SendMessage (new BinaryMessage ("Stop", "Process")), timeout) != timeout) {
					// Wait for at most two seconds for the process to end
					timeout = Task.Delay (4000);
					Log ("await Process Exit timeout 4000");
					if (await Task.WhenAny (process.Task, timeout) != timeout)
						Log ("All done!");
						return; // All done!
				}
			} catch {
			}

			mainCancelSource.Cancel ();
			mainCancelSource = new CancellationTokenSource ();

			// The process did not gracefully stop. Kill the process.

			try {
				StopRemoteProcess ();
			} catch {
				// Ignore
			}

			Log ("await Process.Task");
			await process.Task;
		}

		public async Task Connect ()
		{
			Log ();
			initializationDone = false;
			AbortPendingMessages ();
			if (listener != null && !disposed) {
				// Disconnect the current session and reconnect
				await Disconnect ();
			}
			await StartConnecting ();
			Log ("Connect done");
		}

		Task StartConnecting ()
		{
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

				Log ("BeginAcceptTcpClient");
				listener.BeginAcceptTcpClient (OnConnected, listener);

				await InitializeRemoteProcessAsync (token).ConfigureAwait (false);
				Log ("done");
			} catch (Exception ex) {
				Log (ex);
				HandleRemoteConnectException (ex, token);
			}
		}

		async Task InitializeRemoteProcessAsync (CancellationToken token)
		{
			Log ();
			try {
				await StartRemoteProcess ().ConfigureAwait (false);

				token.ThrowIfCancellationRequested ();

				if (disposed)
					throw new Exception ("Could not start process");
				
				var timeout = Task.Delay (ProcessInitializationTimeout, token).ContinueWith (t => {
					if (t.IsCanceled) {
						Log ("t.IsCanceled; return");
						return;
					}

					if (processConnectedEvent.TrySetException (new Exception ("Could not start process"))) {
						Log ("TrySetException Could not start process");
					}
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
				Log (ex.Message);
				HandleRemoteConnectException (ex, token);
			}
		}

		void HandleRemoteConnectException (Exception ex, CancellationToken token)
		{
			Log (ex.Message);
			LoggingService.LogError ("Connection failed", ex);
			token.ThrowIfCancellationRequested ();
			StopRemoteProcess ();
			SetStatus (ConnectionStatus.ConnectionFailed, ex.Message, ex);
		}

		Task StartRemoteProcess ()
		{
			return Task.Run (() => {
				Log ();
				var cmd = Runtime.ProcessService.CreateCommand (exePath);
				cmd.Arguments = ((IPEndPoint)listener.LocalEndpoint).Port + " " + DebugMode;

				// Explicitly propagate the PATH var to the process. It ensures that tools required
				// to run XS are also in the PATH for remote processes.
				cmd.EnvironmentVariables ["PATH"] = Environment.GetEnvironmentVariable ("PATH");

				process = executionHandler.Execute (cmd, console);
				process.Task.ContinueWith (t => ProcessExited ());
			});
		}

		bool stopping;
		void ProcessExited ()
		{
			Log ();

			// any exception bubbling up from here would crash the process
			try {
				// somehow capture that the process has exited
				process = null;
				if (!stopping)
					AbortConnection (isAsync: true);
			}
			catch (Exception ex) {
				Log ("Caught " + ex.Message);
			}
		}

		public async Task<RT> SendMessage<RT> (BinaryMessage<RT> message) where RT:BinaryMessage
		{
			return (RT) await SendMessage ((BinaryMessage) message);
		}

		public Task<BinaryMessage> SendMessage (BinaryMessage message)
		{
			// do not attempt to send new messages if the process has already exited
			Log (message.ToString ());
			if (process == null || process.IsCompleted) {
				Log ("Process not running, not sending message");
				return Task.FromException<BinaryMessage> (new Exception ("Process not running"));
			}

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
			Log ();
			message.ReadCustomData ();
			var interceptor = Interceptor;
			if (interceptor != null && !interceptor.PreProcessMessage (message))
				return;

			PostMessage (message, null, true);
		}

		public void FlushMessages (string target)
		{
			Log (target);
			var msg = new BinaryMessage ("FlushMessages", target);
			SendMessage (msg);
		}

		void PostMessage (BinaryMessage message, TaskCompletionSource<BinaryMessage> cs, bool checkInitialized)
		{
			if (checkInitialized && !initializationDone) {
				Log ($"Not connected, checkInitialized={checkInitialized} initializationDone={initializationDone}");
				throw new RemoteProcessException ("Not connected");
			}

			if (cs != null) {
				Log ("Before lock messageWaiters");
				lock (messageWaiters) {
					Log ("In messageWaiters");
					messageWaiters [message.Id] = new MessageRequest {
						Request = message,
						TaskSource = cs
					};
					Log ($"messageWaiters[{message.Id}] assigned");
				}
			}

			lock (messageQueue) {
				Log ("In lock messageQueue");
				messageQueue.Add (message);
				if (!senderRunning) {
					senderRunning = true;
					Log ("Setting senderRunning to true");
					ThreadPool.QueueUserWorkItem (delegate {
						SendMessages ();
					});
				}
			}
		}

		bool senderRunning;

		void SendMessages ()
		{
			Log ();
			while (true) {
				List<BinaryMessage> queueCopy;
				lock (messageQueue) {
					if (messageQueue.Count == 0) {
						Log ("senderRunning = false");
						senderRunning = false;
						return;
					}
					queueCopy = new List<BinaryMessage> (messageQueue);
					messageQueue.Clear ();
					Log ("Cleared messageQueue");
				}
				foreach (var m in queueCopy)
					PostMessageInternal (m);
				Log ("Done SendMessages loop");
			}
		}

		void PostMessageInternal (BinaryMessage message)
		{
			Log ($"{message.Id} {message.Name}");

			if ((status != ConnectionStatus.Connected || disposed) && !message.BypassConnection) {
				ProcessResponse (message.CreateErrorResponse ("Connection is closed"));
				Log ($"Connection is closed, status={status} disposed={disposed}");
				return;
			}

			try {
				if (DebugMode)
					message.SentTime = DateTime.Now;

				Log ("Sending message");
				// Now send the message. This one will need a response

				if (DebugMode)
					LogMessage (MessageType.Request, message);

				connectionStream.WriteByte ((byte)RequestType.QueueEnd);
				message.Write (connectionStream);

				connectionStream.Flush ();
			}	
			catch (Exception ex){
				Log (ex);
				if (connection == null || (!connection.Connected && status == ConnectionStatus.Connected)) {
					Log ("PostMessageInteral calling AbortConnection");
					AbortConnection ("Disconnected from remote process due to a communication error", isAsync: true);
				} else
					Log ("Calling ProcessResponse");
					ProcessResponse (message.CreateErrorResponse (ex.ToString ()));
			}
		}

		class MessageRequest
		{
			public BinaryMessage Request;
			public TaskCompletionSource<BinaryMessage> TaskSource;

			public override string ToString ()
			{
				return $"Request Id={Request.Id} Name={Request.Name}";
			}
		}

		Dictionary<int, MessageRequest> messageWaiters = new Dictionary<int, MessageRequest> ();

		void AbortConnection (string message = null, bool isAsync = false)
		{
			Log ($"message={message} isAsync={isAsync}");
			if (message == null)
				message = "Disconnected from remote process";
			AbortPendingMessages ();
			disposed = true;
			Log ("processConnectedEvent TrySetResult true");
			processConnectedEvent.TrySetResult (true);
			if (isAsync)
				PostSetStatus (ConnectionStatus.Disconnected, message);
			else
				SetStatus (ConnectionStatus.Disconnected, message);
		}

		void AbortPendingMessages ()
		{
			Log ();
			lock (messageWaiters) {
				Log ("In lock messageWaiters");
				// capture the original values because they can change while we're enumerating
				var originalValues = messageWaiters.Values.ToArray ();
				foreach (var m in originalValues) {
					Log ("Original value: " + m.ToString ());
				}

				foreach (var m in originalValues) {
					Log ($"Inside foreach for {m}");
					NotifyResponse (m, m.Request.CreateErrorResponse ("Connection closed"));
					Log ("After loop body for {m}");
				}
				Log ("After foreach");
				messageWaiters.Clear ();
				messageQueue.Clear ();
			}

			Log ("AbortPendingMessages done");
		}

		void StopPinger ()
		{
			Log ();
			if (pinger != null) {
				// not sure but it seems that if multiple places call StopPinger we should lock here
				lock (pingerLock) {
					if (pinger != null) {
						Log ("Disposing pinger");
						pinger.Dispose ();
						pinger = null;
					}
				}
			}
		}

		void StopRemoteProcess (bool isAsync = false)
		{
			Log ("isAsync=" + isAsync);
			if (process != null && !process.IsCompleted)
				stopping = true;

			if (stopping) {
				// only abort connection if the process is still there
				AbortConnection (isAsync: isAsync);
			}

			StopPinger ();

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

			if (stopping) {
				Log ("process.Cancel()");
				process.Cancel ();
			}
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
					Log ("Creating pinger");
					pinger = new Timer (PingConnection, null, PingPeriod, PingPeriod);
				} catch (Exception ex) {
					LoggingService.LogError ("Connection to layout renderer failed", ex);
					Log (ex.Message);
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
					Log ("connectionStream.ReadAsync");
					int nr = await connectionStream.ReadAsync (buffer, 0, 1, mainCancelSource.Token).ConfigureAwait (false);
					if (nr == 0) {
						// Connection closed. Remote process should die by itself.
						Log ("nr == 0");
						return;
					}
					type = buffer [0];
					msg = BinaryMessage.Read (connectionStream);
				} catch (Exception ex) {
					if (disposed)
						return;
					LoggingService.LogError ("ReadMessage failed", ex);
					Log (ex.Message);
					StopRemoteProcess (isAsync: true);
					PostSetStatus (ConnectionStatus.ConnectionFailed, "Connection to remote process failed.");
					return;
				}

				Log ("Calling HandleMessage id=" + msg.Id);
				HandleMessage (msg, type);
			}
		}

		async void HandleMessage (BinaryMessage msg, byte type)
		{
			var t = Task.Run (() => {
				Log ("Task start");
				msg = LoadMessageData (msg);
				if (type == 0)
					ProcessResponse (msg);
				else
					ProcessRemoteMessage (msg);
				Log ("Task end");
			});

			try {
				lock (pendingMessageTasks)
					pendingMessageTasks.Add (t);

				Log ("await t");
				await t.ConfigureAwait (false);
			} catch (Exception e) {
				LoggingService.LogError ("RemoteProcessConnection.HandleMessage failed", e);
				Log (e.Message);
			} finally {
				lock (pendingMessageTasks)
					pendingMessageTasks.Remove (t);
			}
		}

		List<Task> pendingMessageTasks = new List<Task> ();

		public Task ProcessPendingMessages ()
		{
			lock (pendingMessageTasks) {
				Log ();
				return Task.WhenAll (pendingMessageTasks.ToArray ());
			}
		}

		BinaryMessage LoadMessageData (BinaryMessage msg)
		{
			Log ();
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
			Log ();

			DateTime respTime = DateTime.Now;
			MessageRequest req;

			lock (messageWaiters) {
				Log ("In lock messageWaiters");
				if (messageWaiters.TryGetValue (msg.Id, out req)) {
					Log ("Remove " + msg.Id);
					messageWaiters.Remove (msg.Id);
					try {
						var rt = req.Request.GetResponseType ();
						if (rt != typeof (BinaryMessage)) {
							var resp = (BinaryMessage)Activator.CreateInstance (rt);
							resp.CopyFrom (msg);
							msg = resp;
						}
					} catch (Exception ex) {
						Log (ex);
						msg = msg.CreateErrorResponse (ex.ToString ());
					}

					if (DebugMode) {
						var time = (int)(respTime - req.Request.SentTime).TotalMilliseconds;
						LogMessage (MessageType.Response, msg, time);
					}

					Log ($"{msg}");

				} else if (DebugMode) {
					req = null;
					Log ("req = null " + msg);
					LogMessage (MessageType.Response, msg, -1);
				}
			}

			// Notify the response outside the lock to avoid deadlocks
			if (req != null && !req.Request.OneWay)
				NotifyResponse (req, msg);
		}

		void NotifyResponse (MessageRequest req, BinaryMessage res)
		{
			Log ($"req={req} res={res}");

			if (disposed || res == null) {
				Log ($"disposed={disposed}");
				req.TaskSource.SetException (new Exception ("Connection closed"));
			}
			else if (res.Name == "Error") {
				Log ("res.Name = Error");
				string msg = res.GetArgument<string> ("Message");
				if (res.GetArgument<bool> ("IsInternal") && !string.IsNullOrEmpty (msg)) {
					msg = "The operation failed due to an internal error: " + msg + ".";
				}

				// if someone else had already set the exception, don't crash
				Log ("req.TaskSource.TrySetException: " + msg);
				req.TaskSource.TrySetException (new RemoteProcessException (msg) { ExtendedDetails = res.GetArgument<string> ("Log") });
			} else {
				Log ("req.TaskSource.SetResult");
				req.TaskSource.SetResult (res);
			}
		}

		void ProcessRemoteMessage (BinaryMessage msg)
		{
			Log ("id=" + msg.Id.ToString ());
			if (DebugMode)
				LogMessage (MessageType.Message, msg);

			if (msg.Name == "Connect") {
				Log ("processConnectedEvent.TrySetResult(true)");
				processConnectedEvent.TrySetResult (true);
				return;
			}

			if (MessageReceived != null) {
				Runtime.RunInMainThread (delegate {
					Log ("MessageReceived?.Invoke");
					MessageReceived?.Invoke (null, new MessageEventArgs () { Message = msg });
				});
			}

			try {
				Log ("Before foreach listeners");
				foreach (var li in listeners) {
					li.ProcessMessage (msg);
				}
			} catch (Exception ex) {
				Log (ex);
				LoggingService.LogError ("Exception in message invocation: " + msg, ex);
			}
		}

		enum MessageType { Request, Response, Message }

		long tickBase = Environment.TickCount;

		void LogMessage (MessageType type, BinaryMessage msg, int time = -1)
		{
			Log ("[" + (Environment.TickCount - tickBase) + "] ");

			if (type == MessageType.Request)
				Log ("[CLIENT] XS >> RP " + msg);
			else if (type == MessageType.Response) {
				if (time != -1)
					Log ("[CLIENT] XS << RP " + time + "ms " + msg);
				else
					Log ("[CLIENT] XS << RP " + msg);
			}
			else
				Log ("[CLIENT] XS <- RP " + msg);
		}

		void PingConnection (object state)
		{
			Log ();
			bool lockTaken = false;
			try {
				Monitor.TryEnter (pingerLock, ref lockTaken);
				if (!lockTaken) {
					Log ("lockTaken=false");
					return;
				}

				// only attempt to ping if the process is still there
				if (pinger != null) {
					var msg = new BinaryMessage ("Ping", "Process");
					SendMessage (msg);
				}

			} catch (Exception ex) {
				Log (ex);
				LoggingService.LogError ("Connection ping failed", ex);
			} finally {
				if (lockTaken)
					Monitor.Exit (pingerLock);
			}
		}

		private static object logLock = new object ();
		private static readonly string logFilePath = @"C:\temp\md\" + Process.GetCurrentProcess ().Id.ToString () + ".txt";
		public static void Log (string s = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
		{
			lock (logLock) {
				File.AppendAllText (logFilePath, $"{DateTime.Now.ToString ("HH:mm:ss.fff")} [thread: {Thread.CurrentThread.ManagedThreadId.ToString ().PadLeft (2, ' ')}]: {memberName} (line {lineNumber}): " + s + Environment.NewLine);
			}
		}

		public static void Log (Exception ex, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
		{
			Log ("Exception: " + ex.Message, memberName, lineNumber);
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

