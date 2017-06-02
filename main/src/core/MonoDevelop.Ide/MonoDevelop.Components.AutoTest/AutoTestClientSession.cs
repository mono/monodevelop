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
using System.Xml;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Tasks;
using System.Text;

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

		IAutoTestSessionDebug<MarshalByRefObject> debugObject;
		public IAutoTestSessionDebug<MarshalByRefObject> DebugObject {  
			get {
				return debugObject;
			}
			set {
				debugObject = value;
				if (session != null) {
					session.DebugObject = debugObject; 
				}
			}
		}
				
		public int StartApplication (string file = null, string args = null, IDictionary<string, string> environment = null)
		{
			if (file == null) {
				var binDir = Path.GetDirectoryName (typeof(AutoTestClientSession).Assembly.Location);
				file = Path.Combine (binDir, "MonoDevelop.exe");
				if (!File.Exists (file)) {
					file = Path.Combine (binDir, "XamarinStudio.exe");
				}
			} else if (!File.Exists (file)) {
				file = file.Replace ("MonoDevelop.exe", "XamarinStudio.exe");
			}

			if (!File.Exists (file)) {
				throw new FileNotFoundException (file);
			}

			MonoDevelop.Core.Execution.RemotingService.RegisterRemotingChannel ();

			BinaryFormatter bf = new BinaryFormatter ();
			ObjRef oref = RemotingServices.Marshal (this);
			MemoryStream ms = new MemoryStream ();
			bf.Serialize (ms, oref);
			string sref = Convert.ToBase64String (ms.ToArray ());

			var errors = new StringBuilder ();
			var pi = new ProcessStartInfo (file, args) { UseShellExecute = false };
			
			pi.EnvironmentVariables ["MONO_AUTOTEST_CLIENT"] = sref;
			if (environment != null)
				foreach (var e in environment)
					pi.EnvironmentVariables [e.Key] = e.Value;

			process = new Process ();
			process.StartInfo = pi;
			process.ErrorDataReceived += (sender, e) => {
				lock (errors) {
					if (e != null && e.Data != null)
						errors.Append (e.Data);
				}
			};
			if (!process.Start ())
				throw new Exception ("Process start failed");
			process.BeginErrorReadLine ();

			if (!waitEvent.WaitOne (120000)) {
				try {
					process.Kill ();
				} catch { }
				throw new Exception ("Could not connect to application\n"+errors.ToString ());
			}

			return process.Id;
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
			if (DebugObject != null) {
				session.DebugObject = DebugObject;
			}
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
				try {
					process.Kill ();
				} catch (InvalidOperationException) {
					Console.WriteLine ("Process has already exited");
				}
		}

		public AutoTestSession.MemoryStats MemoryStats {
			get {
				return session.GetMemoryStats ();
			}
		}

		public string[] CounterStats {
			get {
				return session.GetCounterStats ();
			}
		}

		public void ExitApp ()
		{
			ClearEventQueue ();
			session.ExitApp ();
		}

		public void ExecuteCommand (object cmd, object dataItem = null, CommandSource source = CommandSource.Unknown)
		{
			session.ExecuteCommand (cmd, dataItem, source);
		}

		/*
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
		*/
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

		public void TakeScreenshot (string screenshotPath)
		{
			session.TakeScreenshot (screenshotPath);
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

		/*
		public object Invoke (string name, params object[] args)
		{
			ClearEventQueue ();
			return session.Invoke (name, args);
		}
		*/

		public int ErrorCount (TaskSeverity severity)
		{
			return session.ErrorCount (severity);
		}

		public List<TaskListEntryDTO> GetErrors (TaskSeverity severity)
		{
			return session.GetErrors (severity);
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
			lock (eventQueue) {
				eventQueue.Clear ();
			}
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

			ClearEventQueue ();
			return session.ExecuteQuery (c);
		}

		public AppResult[] WaitForElement (Func<AppQuery, AppQuery> query, int timeout = 5000)
		{
			AppQuery c = session.CreateNewQuery ();
			c = query (c);

			ClearEventQueue ();
			return session.WaitForElement (c, timeout);
		}

		public void WaitForNoElement (Func<AppQuery, AppQuery> query, int timeout = 5000)
		{
			AppQuery c = session.CreateNewQuery ();
			c = query (c);

			ClearEventQueue ();
			session.WaitForNoElement (c, timeout);
		}

		public bool SelectElement (Func<AppQuery, AppQuery> query)
		{
			AppResult[] results = Query (query);

			if (results.Length > 0) {
				return session.Select (results [0]);
			}

			return false;
		}

		public bool ClickElement (Func<AppQuery, AppQuery> query, bool wait = true)
		{
			AppResult[] results = Query (query);
			if (results.Length > 0) {
				return session.Click (results [0], wait);
			}

			return false;
		}

		public bool ClickElement (Func<AppQuery, AppQuery> query, double x, double y, bool wait = true)
		{
			AppResult [] results = Query (query);
			if (results.Length > 0) {
				return session.Click (results [0], x, y, wait);
			}

			return false;
		}

		public bool EnterText (Func<AppQuery, AppQuery> query, string text)
		{
			AppResult[] results = Query (query);
			if (results.Length > 0) {
				bool result = session.Select (results [0]);
				if (!result) {
					return false;
				}

				return session.EnterText (results [0], text);
			}

			return false;
		}

		public bool TypeKey (Func<AppQuery, AppQuery> query, char key, string modifiers)
		{
			AppResult[] results = Query (query);
			if (results.Length > 0) {
				bool result = session.Select (results [0]);
				if (!result) {
					return false;
				}

				return session.TypeKey (results [0], key, modifiers);
			}

			return false;
		}

		public bool TypeKey (Func<AppQuery, AppQuery> query, string keyString, string modifiers)
		{
			AppResult[] results = Query (query);
			if (results.Length > 0) {
				bool result = session.Select (results [0]);
				if (!result) {
					return false;
				}

				return session.TypeKey (results [0], keyString, modifiers);
			}

			return false;
		}

		// FIXME: Not convinced this is the best name
		public bool ToggleElement (Func<AppQuery, AppQuery> query, bool active)
		{
			AppResult[] results = Query (query);
			if (results.Length == 0) {
				return false;
			}

			return session.Toggle (results [0], active);
		}

		public void Flash (Func<AppQuery, AppQuery> query)
		{
			AppResult[] results = Query (query);
			foreach (var result in results) {
				session.Flash (result);
			}
		}

		public void SetProperty (Func<AppQuery, AppQuery> query, string propertyName, object value)
		{
			AppResult[] results = Query (query);
			foreach (var result in results) {
				session.SetProperty (result, propertyName, value);
			}
		}

		public bool SetActiveConfiguration (Func<AppQuery, AppQuery> query, string configuration)
		{
			AppResult[] results = Query (query);
			if (results.Length == 0) {
				return false;
			}

			return session.SetActiveConfiguration (results [0], configuration);
		}

		public bool SetActiveRuntime (Func<AppQuery, AppQuery> query, string runtime)
		{
			AppResult[] results = Query (query);
			if (results.Length == 0) {
				return false;
			}

			return session.SetActiveRuntime (results [0], runtime);
		}

		public void RunAndWaitForTimer (Action action, string counterName, int timeout = 20000)
		{
			AutoTestSession.TimerCounterContext context = session.CreateNewTimerContext (counterName);
			action ();
			session.WaitForTimerContext (context, timeout);
		}

		public XmlDocument ResultsAsXml (AppResult[] results)
		{
			string xmlResults = session.ResultsAsXml (results);
			XmlDocument document = new XmlDocument ();
			document.LoadXml (xmlResults);

			return document;
		}
	}

	public interface IAutoTestClient
	{
		void Connect (AutoTestSession session);
		void NotifyEvent (string eventName);
	}
}

