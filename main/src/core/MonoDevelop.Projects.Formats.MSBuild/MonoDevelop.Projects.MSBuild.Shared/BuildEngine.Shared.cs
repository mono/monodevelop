// 
// ProjectBuilder.cs
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
using System.Threading;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

//this is the builder for the deprecated build engine API
using MonoDevelop.Core.Execution;
using System.Net.Configuration;
using System.Diagnostics;
#pragma warning disable 618

namespace MonoDevelop.Projects.MSBuild
{
	partial class BuildEngine
	{
		static readonly AutoResetEvent workDoneEvent = new AutoResetEvent (false);
		static ThreadStart workDelegate;
		static readonly object workLock = new object ();
		static Thread workThread;
		static Exception workError;

		static List<int> cancelledTasks = new List<int> ();
		static int currentTaskId;
		static int projectIdCounter;
		Dictionary<int, ProjectBuilder> projects = new Dictionary<int, ProjectBuilder> ();

		readonly ManualResetEvent doneEvent = new ManualResetEvent (false);

		static RemoteProcessServer server;

		internal WaitHandle WaitHandle {
			get { return doneEvent; }
		}

		public class LogWriter: ILogWriter
		{
			int id;

			public LogWriter (int loggerId)
			{
				this.id = loggerId;
			}

			public void Write (string text)
			{
				server.SendMessage (new LogMessage { LoggerId = id, Text = text });
			}
		}

		public class NullLogWriter: ILogWriter
		{
			public void Write (string text)
			{
			}
		}

		void WatchProcess (int procId)
		{
			var t = new Thread (delegate () {
				while (true) {
					Thread.Sleep (1000);
					try {
						// Throws exception if process is not running.
						// When watching a .NET process from Mono, GetProcessById may
						// return the process with HasExited=true
						Process p = Process.GetProcessById (procId);
						if (p.HasExited)
							break;
					}
					catch {
						break;
					}
				}
				doneEvent.Set ();
			});
			t.IsBackground = true;
			t.Start ();
		}

		public BuildEngine (RemoteProcessServer pserver)
		{
			server = pserver;
		}

		[MessageHandler]
		public BinaryMessage Initialize (InitializeRequest msg)
		{
			WatchProcess (msg.IdeProcessId);
			SetCulture (CultureInfo.GetCultureInfo (msg.CultureName));
			SetGlobalProperties (msg.GlobalProperties);
			return msg.CreateResponse ();
		}

		[MessageHandler]
		public LoadProjectResponse LoadProject (LoadProjectRequest msg)
		{
			var pb = LoadProject (msg.ProjectFile);
			lock (projects) {
				var id = ++projectIdCounter;
				projects [id] = pb;
				return new LoadProjectResponse { ProjectId = id };
			}
		}

		[MessageHandler]
		public BinaryMessage UnloadProject (UnloadProjectRequest msg)
		{
			ProjectBuilder pb = null;
			lock (projects) {
				if (projects.TryGetValue (msg.ProjectId, out pb))
					projects.Remove (msg.ProjectId);
			}
			if (pb != null)
				UnloadProject (pb);
			return msg.CreateResponse ();
		}

		[MessageHandler]
		public BinaryMessage Dispose (DisposeRequest msg)
		{
			doneEvent.Set ();
			return msg.CreateResponse ();
		}

		[MessageHandler]
		public BinaryMessage Ping (PingRequest msg)
		{
			return msg.CreateResponse ();
		}

		[MessageHandler]
		public BinaryMessage CancelTask (CancelTaskRequest msg)
		{
			lock (cancelledTasks) {
				if (currentTaskId == msg.TaskId)
					AbortCurrentTask ();
				else
					cancelledTasks.Add (msg.TaskId);
			}
			return msg.CreateResponse ();
		}

		[MessageHandler]
		public BinaryMessage SetGlobalProperties (SetGlobalPropertiesRequest msg)
		{
			SetGlobalProperties (msg.Properties);
			return msg.CreateResponse ();
		}

		[MessageHandler]
		public BinaryMessage DisposeProject (DisposeProjectRequest msg)
		{
			var pb = GetProject (msg.ProjectId);
			if (pb != null)
				pb.Dispose ();
			return msg.CreateResponse ();
		}

		[MessageHandler]
		public BinaryMessage RefreshProject (RefreshProjectRequest msg)
		{
			var pb = GetProject (msg.ProjectId);
			if (pb != null)
				pb.Refresh ();
			return msg.CreateResponse ();
		}

		[MessageHandler]
		public BinaryMessage RefreshWithContent (RefreshWithContentRequest msg)
		{
			var pb = GetProject (msg.ProjectId);
			if (pb != null)
				pb.RefreshWithContent (msg.Content);
			return msg.CreateResponse ();
		}

		[MessageHandler]
		public BinaryMessage RunProject (RunProjectRequest msg)
		{
			var pb = GetProject (msg.ProjectId);
			if (pb != null) {
				var logger = msg.LogWriterId != -1 ? (ILogWriter) new LogWriter (msg.LogWriterId) : (ILogWriter) new NullLogWriter ();
				var res = pb.Run (msg.Configurations, logger, msg.Verbosity, msg.RunTargets, msg.EvaluateItems, msg.EvaluateProperties, msg.GlobalProperties, msg.TaskId);
				return new RunProjectResponse { Result = res };
			}
			return msg.CreateResponse ();
		}

		ProjectBuilder GetProject (int projectId)
		{
			lock (projects) {
				ProjectBuilder pb;
				if (projects.TryGetValue (projectId, out pb))
					return pb;
				return null;
			}
		}

		static bool IsTaskCancelled (int taskId)
		{
			lock (cancelledTasks) {
				return cancelledTasks.Contains (taskId);
			}
		}

		static bool SetCurrentTask (int taskId)
		{
			lock (cancelledTasks) {
				if (cancelledTasks.Contains (taskId))
					return false;
				currentTaskId = taskId;
				return true;
			}
		}

		static void ResetCurrentTask ()
		{
			lock (cancelledTasks) {
				currentTaskId = -1;
			}
		}

		static void AbortCurrentTask ()
		{
			workThread.Abort ();
			workThread = null;
			workDoneEvent.Set ();
		}

		internal static void RunSTA (ThreadStart ts)
		{
			RunSTA (-1, ts);
		}

		internal static void RunSTA (int taskId, ThreadStart ts)
		{
			lock (workLock) {
				if (IsTaskCancelled (taskId))
					return;
				lock (threadLock) {
					// Last chance to check for canceled task before the thread is started
					if (IsTaskCancelled (taskId))
						return;
					
					workDelegate = ts;
					workError = null;
					if (workThread == null) {
						workThread = new Thread (STARunner);
						workThread.SetApartmentState (ApartmentState.STA);
						workThread.IsBackground = true;
						workThread.CurrentUICulture = uiCulture;
						workThread.Start ();
					}
					else
						// Awaken the existing thread
						Monitor.Pulse (threadLock);
				}
				if (!SetCurrentTask (taskId)) {
					// The task was aborted after all. Since the thread is already running we need to abort it
					AbortCurrentTask ();
					return;
				}

				workDoneEvent.WaitOne ();

				ResetCurrentTask ();
			}
			if (workError != null)
				throw new Exception ("MSBuild operation failed", workError);
		}

		static readonly object threadLock = new object ();
		
		static void STARunner ()
		{
			lock (threadLock) {
				do {
					try {
						workDelegate ();
					}
					catch (Exception ex) {
						workError = ex;
					}
					workDoneEvent.Set ();
				}
				while (Monitor.Wait (threadLock, 60000));
				
				workThread = null;
			}
		}
	}
}