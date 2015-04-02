//
// AutoTestClientSession.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Components.AutoTest
{
	public class AutoTestClientSession: MarshalByRefObject, IAutoTestClient
	{
		Process process;
		AutoTestSession session;
		ManualResetEvent waitEvent = new ManualResetEvent (false);
		int defaultEventWaitTimeout = 20000;
		Queue<string> eventQueue = new Queue<string> ();
		IAutoTestService service;


		public void StartApplication (string file = null, string args = null, IDictionary<string, string> environment = null)
		{
			if (file == null) {
				var binDir = Path.GetDirectoryName (typeof(AutoTestClientSession).Assembly.Location);
				file = Path.Combine (binDir, "MonoDevelop.exe");
			}

			MonoDevelop.Core.Execution.RemotingService.RegisterRemotingChannel ();

			BinaryFormatter bf = new BinaryFormatter ();
			ObjRef oref = RemotingServices.Marshal (this);
			MemoryStream ms = new MemoryStream ();
			bf.Serialize (ms, oref);
			string sref = Convert.ToBase64String (ms.ToArray ());

			var pi = new ProcessStartInfo (file, args) { UseShellExecute = false };
			pi.EnvironmentVariables ["MONO_AUTOTEST_CLIENT"] = sref;
			pi.EnvironmentVariables ["GTK_MODULES"] = "gail:atk-bridge";
			if (environment != null)
				foreach (var e in environment)
					pi.EnvironmentVariables [e.Key] = e.Value;

			process = Process.Start (pi);

			if (!waitEvent.WaitOne (15000)) {
				try {
					process.Kill ();
				} catch { }
				throw new Exception ("Could not connect to application");
			}
		}

		public void AttachApplication ()
		{
			MonoDevelop.Core.Execution.RemotingService.RegisterRemotingChannel ();

			string sref = File.ReadAllText (AutoTestService.SessionReferenceFile);
			byte[] data = Convert.FromBase64String (sref);
			MemoryStream ms = new MemoryStream (data);
			BinaryFormatter bf = new BinaryFormatter ();
			service = (IAutoTestService) bf.Deserialize (ms);
			session = service.AttachClient (this);
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}

		public void Stop ()
		{
			if (service != null)
				service.DetachClient (this);
			else
				process.Kill ();
		}

		public void ExecuteCommand (object cmd)
		{
			session.ExecuteCommand (cmd);
		}

		public void SelectObject (string name)
		{
			ClearEventQueue ();
			session.SelectObject (name);
		}

		public void SelectActiveWidget ()
		{
			ClearEventQueue ();
			session.SelectActiveWidget ();
		}

		public bool SelectWidget (string name, bool focus = true)
		{
			ClearEventQueue ();
			return session.SelectWidget (name, focus);
		}

		public object GetPropertyValue (string propertyName)
		{
			ClearEventQueue ();
			return session.GetPropertyValue (propertyName);
		}

		public bool SetPropertyValue (string propertyName, object value, object[] index = null)
		{
			ClearEventQueue ();
			return session.SetPropertyValue (propertyName, value, index);
		}

		public object GlobalInvoke (string name, params object[] args)
		{
			ClearEventQueue ();
			return session.GlobalInvoke (name, args);
		}

		public T GlobalInvoke<T> (string name, params object[] args)
		{
			return (T) GlobalInvoke (name, args);
		}

		public object GetGlobalValue (string name)
		{
			return session.GetGlobalValue (name);
		}

		public T GetGlobalValue<T> (string name)
		{
			return (T) session.GetGlobalValue (name);
		}

		public void SetGlobalValue (string name, object value)
		{
			ClearEventQueue ();
			session.SetGlobalValue (name, value);
		}

		public object Invoke (string name, params object[] args)
		{
			ClearEventQueue ();
			return session.Invoke (name, args);
		}

		public void TypeText (string text)
		{
			ClearEventQueue ();
			session.TypeText (text);
		}

		public bool SelectTreeviewItem (string treeName, string name, string after = null)
		{
			ClearEventQueue ();
			return session.SelectTreeviewItem (treeName, name, after);
		}

		public string[] GetTreeviewCells ()
		{

			ClearEventQueue ();
			return session.GetTreeviewCells ();
		}

		public bool IsBuildSuccessful ()
		{
			return session.IsBuildSuccessful ();
		}

		public void PressKey (Gdk.Key key)
		{
			ClearEventQueue ();
			session.SendKeyPress (key, Gdk.ModifierType.None, null);
		}

		public void PressKey (Gdk.Key key, Gdk.ModifierType state)
		{
			ClearEventQueue ();
			session.SendKeyPress (key, state, null);
		}

		public void PressKey (Gdk.Key key, Gdk.ModifierType state, string subWindow)
		{
			ClearEventQueue ();
			session.SendKeyPress (key, state, subWindow);
		}

		public void WaitForEvent (string name)
		{
			WaitForEvent (name, defaultEventWaitTimeout);
		}

		public void WaitForEvent (string name, int timeout)
		{
			lock (eventQueue) {
				while (eventQueue.Count > 0) {
					if (eventQueue.Dequeue () == name)
						return;
				}
				if (!Monitor.Wait (eventQueue, timeout))
					throw new Exception ("Expected event '" + name + "' not received");
			}
		}

		public void WaitForWindow (string windowName, int timeout = 10000)
		{
			session.WaitForWindow (windowName, timeout);
		}

		void ClearEventQueue ()
		{
			eventQueue.Clear ();
		}

		void IAutoTestClient.Connect (AutoTestSession session)
		{
			this.session = session;
			waitEvent.Set ();
		}

		void IAutoTestClient.NotifyEvent (string eventName)
		{
			lock (eventQueue) {
				eventQueue.Enqueue (eventName);
				Monitor.PulseAll (eventQueue);
			}
		}

		public AppResult[] Query (Func<AppQuery, AppQuery> query)
		{
			AppQuery c = session.CreateNewQuery ();
			c = query (c);

			return session.ExecuteQuery (c);
		}
	}

	public interface IAutoTestClient
	{
		void Connect (AutoTestSession session);
		void NotifyEvent (string eventName);
	}
}

