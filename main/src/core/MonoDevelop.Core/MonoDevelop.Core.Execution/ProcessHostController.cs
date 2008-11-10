//
// ProcessHostController.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Timer = System.Timers.Timer;

namespace MonoDevelop.Core.Execution
{
	internal class ProcessHostController: MarshalByRefObject, IProcessHostController
	{
		int references;
		uint stopDelay;
		DateTime lastReleaseTime;
		bool starting;
		bool stopping;
		Process process;
		Timer timer;
		string id;

		IProcessHost processHost;
		ManualResetEvent runningEvent = new ManualResetEvent (false);
		ManualResetEvent exitRequestEvent = new ManualResetEvent (false);
		ManualResetEvent exitedEvent = new ManualResetEvent (false);
		
		public ProcessHostController (string id, uint stopDelay)
		{
			this.id = id;
			this.stopDelay = stopDelay;
			timer = new Timer ();
			timer.AutoReset = false;
			timer.Elapsed += new System.Timers.ElapsedEventHandler (WaitTimeout);
		}
		
		public void Start ()
		{
			lock (this)
			{
				if (starting) return;
				starting = true;
				exitRequestEvent.Reset ();
				
				string chId = Runtime.ProcessService.RegisterRemotingChannel ();
				
				BinaryFormatter bf = new BinaryFormatter ();
				ObjRef oref = RemotingServices.Marshal (this);
				MemoryStream ms = new MemoryStream ();
				bf.Serialize (ms, oref);
				string sref = Convert.ToBase64String (ms.ToArray ());

				try {
					process = new Process ();
					process.Exited += new EventHandler (ProcessExited);
					string location = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);
					string args = string.Empty;
					if (isDebugMode) args += " --debug";
					args += " \"" + Path.Combine (location, "mdhost.exe") + "\" " + id;
					process.StartInfo = new ProcessStartInfo ("mono", args);
					process.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.RedirectStandardInput = true;
					process.EnableRaisingEvents = true;
					process.Start ();
					process.StandardInput.WriteLine (chId);
					process.StandardInput.WriteLine (sref);
					process.StandardInput.Flush ();
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
					throw;
				}
			}
		}
		
		bool isDebugMode
		{
			get {
				return System.Environment.StackTrace.IndexOf ("ProcessHostController.cs:") > -1;
			}
		}

		void ProcessExited (object sender, EventArgs args)
		{
			lock (this) {
				exitedEvent.Set ();
				Process p = (Process) sender;
				if (p != process) return;

				// The process suddently died
				runningEvent.Reset ();
				processHost = null;
				process = null;
				references = 0;
			}
		}
		
		public RemoteProcessObject CreateInstance (Type type, string[] addins)
		{
			lock (this) {
				references++;
				if (processHost == null)
					Start ();
			}
			
			if (!runningEvent.WaitOne (15000, false)) {
				references--;
				throw new ApplicationException ("Couldn't create a remote process.");
			}
			
			try {
				// Before creating the instance, load the add-ins on which it depends
				if (addins != null && addins.Length > 0)
					processHost.LoadAddins (addins);
				return processHost.CreateInstance (type);
			} catch {
				ReleaseInstance (null);
				throw;
			}
		}
		
		public RemoteProcessObject CreateInstance (string assemblyPath, string typeName, string[] addins)
		{
			lock (this) {
				references++;
				if (processHost == null)
					Start ();
			}
			
			if (!runningEvent.WaitOne (15000, false)) {
				references--;
				throw new ApplicationException ("Couldn't create a remote process.");
			}
			
			try {
				// Before creating the instance, load the add-ins on which it depends
				if (addins != null && addins.Length > 0)
					processHost.LoadAddins (addins);
				return processHost.CreateInstance (assemblyPath, typeName);
			} catch {
				ReleaseInstance (null);
				throw;
			}
		}
		
		public void ReleaseInstance (RemoteProcessObject proc)
		{
			if (processHost == null) return;
			
			lock (this) {
				references--;
				if (references == 0) {
					lastReleaseTime = DateTime.Now;
					if (!stopping) {
						stopping = true;
						if (stopDelay == 0) {
							// Always stop asyncrhonously, so the remote object
							// has time to end the dispose call.
							timer.Interval = 1000;
							timer.Enabled = true;
						} else {
							timer.Interval = stopDelay;
							timer.Enabled = true;
						}
					}
				}
			}
		}
		
		void WaitTimeout (object sender, System.Timers.ElapsedEventArgs args)
		{
			try {
				Process oldProcess;
				
				lock (this) {
					if (references > 0) {
						stopping = false;
						return;
					}
	
					uint waited = (uint) (DateTime.Now - lastReleaseTime).TotalMilliseconds;
					if (waited < stopDelay) {
						timer.Interval = stopDelay - waited;
						timer.Enabled = true;
						return;
					}
				
					runningEvent.Reset ();
					exitedEvent.Reset ();
					exitRequestEvent.Set ();
					oldProcess = process;
					processHost = null;
					process = null;
					stopping = false;
				}
	
				if (!exitedEvent.WaitOne (2000, false)) {
					try {
						oldProcess.Kill ();
					} catch {
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}
		
		public void RegisterHost (IProcessHost processHost)
		{
			lock (this)
			{
				this.processHost = processHost;
				runningEvent.Set ();
				starting = false;
			}
		}
		
		public void WaitForExit ()
		{
			exitRequestEvent.WaitOne ();
		}
	}
}
