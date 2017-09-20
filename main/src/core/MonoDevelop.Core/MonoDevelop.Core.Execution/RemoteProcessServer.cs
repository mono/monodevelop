//
// RemoteProcessServer.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Reflection;
using System.Linq;

namespace MonoDevelop.Core.Execution
{
	public class RemoteProcessServer
	{
		TcpClient socket;
		Stream outStream;
		Stream inStream;
		string messages = "";
		bool shuttingDown;

		Dictionary<string, MessageListenerHandler> listeners = new Dictionary<string, MessageListenerHandler> ();
		Dictionary<string, Type> messageTypes = new Dictionary<string, Type> ();

		const int MESSAGE_QUEUE_END = 1;

		public void Connect (string [] processArgs, object processListener)
		{
			Connect (int.Parse (processArgs [0]), processListener, bool.Parse (processArgs [1]));
		}

		public void Connect (int port, object processListener, bool debugMode = false)
		{
			DebugMode = debugMode;
			socket = new TcpClient ("127.0.0.1", port);
			outStream = socket.GetStream ();
			inStream = outStream;

			AddListener (processListener);
			Start ();

			BinaryMessage msg = new BinaryMessage ("Connect");
			WriteMessage (1, msg);
		}

		public bool DebugMode { get; private set; }

		public void LogError (Exception ex)
		{
			Log ("ERROR", ex.ToString ());
		}

		public void LogError (string message)
		{
			Log ("ERROR", message);
		}

		public void LogWarning (string message)
		{
			Log ("WARNING", message);
		}

		public void LogInfo (string message)
		{
			Log ("INFO", message);
		}

		private void Log (string tag, string message)
		{
			if (messages.Length == 0)
				messages += "\n";
			messages += message;
			Console.WriteLine (tag + ": " + message);
		}

		public void ResetLog ()
		{
			messages = "";
		}

		public string GetLog ()
		{
			return messages;
		}

		public void Shutdown ()
		{
			try {
				shuttingDown = true;
				inStream.Close ();
				socket.Close ();
			} catch {
				// Ignore
			}
		}

		void Start ()
		{
			var t = new Thread (Run);
			t.Start ();
		}

		public void AddListener (MessageListener listener)
		{
			AddListener ((object)listener);
		}

		public void AddListener (object listener)
		{
			lock (listeners) {
				var li = new MessageListenerHandler (listener);
				li.InitListener (this);
				listeners [li.TargetId] = li;
				RegisterMessageTypes (li.Listener.GetMessageTypes ());
			}
		}

		public void RemoveListener (MessageListener listener)
		{
			RemoveListener ((object)listener);
		}

		public void RemoveListener (object listener)
		{
			lock (listeners) {
				var li = listeners.Values.FirstOrDefault (l => l.Target == listener);
				if (li != null)
					listeners.Remove (li.TargetId);
			}
		}

		public void SendMessage (BinaryMessage msg)
		{
			WriteMessage (1, msg);
		}

		public void Run ()
		{
			List<BinaryMessage> messages = new List<BinaryMessage> ();

			while (!shuttingDown) {
				BinaryMessage msg;
				int type;
				try {
					type = inStream.ReadByte ();
					if (type == -1)
						break;
					msg = BinaryMessage.Read (inStream);
					msg = LoadMessageData (msg);
					if (DebugMode) {
						String mtype = type == MESSAGE_QUEUE_END ? "[M] " : "[Q] ";
						Console.WriteLine ("[SERVER] XS >> RP " + mtype + msg);
					}
				} catch (Exception e) {
					Console.WriteLine (e);
					break;
				}
				if (msg.Name == "Stop" && msg.Target == "Process") {
					try {
						WriteMessage (0, msg.CreateResponse ());
					} catch {
						// Ignore
					}
					break;
				}
				if (msg.Name == "Ping" && msg.Target == "Process") {
					try {
						WriteMessage (0, msg.CreateResponse ());
					} catch {
						// Ignore
					}
					continue;
				}
				messages.Add (msg);
				if (type == MESSAGE_QUEUE_END) {
					ProcessMessages (messages);
					messages.Clear ();
				}
			}
		}

		void ProcessMessages (List<BinaryMessage> msgs)
		{
			foreach (BinaryMessage msg in msgs) {
				MessageListenerHandler l;
				lock (listeners) {
					listeners.TryGetValue (msg.Target ?? "", out l);
				}

				if (l != null) {
					l.DispatchMessage (msg);
				} else {
					BinaryMessage response = msg.CreateErrorResponse ("No handler found for target: " + msg.Target, true);
					SendResponse (response);
				}
			}
		}

		public void SendResponse (BinaryMessage response)
		{
			WriteMessage (0, response);
		}

		public void WriteMessage (byte type, BinaryMessage msg)
		{
			msg.ReadCustomData ();
			lock (listeners) {
				if (DebugMode)
					Console.WriteLine ("[SERVER] XS << RP " + type + " [" + msg.ProcessingTime + "ms] " + msg);
				outStream.WriteByte (type);
				try {
					msg.Write (outStream);
				} catch (Exception ex) {
					msg.CreateErrorResponse (ex.ToString (), true).Write (outStream);
				}
			}
		}

		public void RegisterMessageTypes (params Type [] types)
		{
			foreach (var t in types) {
				var a = (MessageDataTypeAttribute)Attribute.GetCustomAttribute (t, typeof (MessageDataTypeAttribute));
				if (a != null) {
					var name = a.Name ?? t.FullName;
					messageTypes [name] = t;
				}
			}
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

		class MessageListenerHandler
		{
			RemoteProcessServer server;
			MessageListener listener;
			object target;

			public MessageListenerHandler (object target)
			{
				this.target = target;
				listener = target as MessageListener;
				if (listener == null)
					listener = new MessageListener (target);
			}

			public object Target {
				get {
					return target;
				}
			}

			public MessageListener Listener {
				get {
					return listener;
				}
			}

			internal void InitListener (RemoteProcessServer server)
			{
				this.server = server;
			}

			public void Dispose ()
			{
			}

			public void DispatchMessage (BinaryMessage msg)
			{
				ThreadPool.QueueUserWorkItem ((state) => { ExecuteMessage (msg); });
			}

			void ExecuteMessage (BinaryMessage msg)
			{
				BinaryMessage response = null;
				var sw = System.Diagnostics.Stopwatch.StartNew ();
				try {
					if (msg.Name == "FlushMessages") {
						response = msg.CreateResponse ();
					} else
						response = listener.ProcessMessage (msg);
				} catch (Exception ex) {
					if (ex is TargetInvocationException)
						ex = ((TargetInvocationException)ex).InnerException;
					server.LogError (ex);
					response = msg.CreateErrorResponse (ex.Message, !(ex is RemoteProcessException));
					Console.WriteLine (ex);
				}
				if (response != null) {
					response.Id = msg.Id;
					response.ProcessingTime = sw.ElapsedMilliseconds;
					server.SendResponse (response);
				} else if (!msg.OneWay)
					server.SendResponse (msg.CreateErrorResponse ("Got no response from server for message: " + msg, true));
			}

			public string TargetId {
				get { return listener.TargetId ?? ""; }
			}
		}
	}
}

