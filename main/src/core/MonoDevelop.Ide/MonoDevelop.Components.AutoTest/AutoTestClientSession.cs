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
using System.Runtime.Remoting.Channels;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.IO;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;
using System.Collections.Generic;

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
		
		public AutoTestClientSession ()
		{
		}
		
		public void StartApplication (string file, string args)
		{
			AutoTestService.SetupRemoting ();
			
			if (file.ToLower ().EndsWith (".exe") && Path.DirectorySeparatorChar != '\\') {
				args = "\"" + file + "\" " + args;
				file = "mono";
			}
			
			BinaryFormatter bf = new BinaryFormatter ();
			ObjRef oref = RemotingServices.Marshal (this);
			MemoryStream ms = new MemoryStream ();
			bf.Serialize (ms, oref);
			string sref = Convert.ToBase64String (ms.ToArray ());
			
			ProcessStartInfo pi = new ProcessStartInfo (file, args);
			pi.UseShellExecute = false;
			pi.EnvironmentVariables ["MONO_AUTOTEST_CLIENT"] = sref;
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
			AutoTestService.SetupRemoting ();

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
		
		public object GlobalInvoke (string name, params object[] args)
		{
			ClearEventQueue ();
			return session.GlobalInvoke (name, args);
		}
		
		public object GetGlobalValue (string name)
		{
			return session.GetGlobalValue (name);
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
	}

	public interface IAutoTestClient
	{
		void Connect (AutoTestSession session);
		void NotifyEvent (string eventName);
	}
}

