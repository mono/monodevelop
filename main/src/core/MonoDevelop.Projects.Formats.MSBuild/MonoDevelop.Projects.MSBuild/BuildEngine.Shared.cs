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
using System.Reflection;
using System.Linq;
#pragma warning disable 618

namespace MonoDevelop.Projects.MSBuild
{
	partial class BuildEngine
	{
		static AutoResetEvent workDoneEvent;
		static ThreadStart workDelegate;
		static readonly object workLock = new object ();
		static Thread workThread;
		static Exception workError;

		static List<int> cancelledTasks = new List<int> ();
		static int currentTaskId;
		static int fatalErrorRetries = 4;
		static int projectIdCounter;
		static string msbuildBinDir;
		Dictionary<int, ProjectBuilder> projects = new Dictionary<int, ProjectBuilder> ();

		static RemoteProcessServer server;

		public class LogWriter: IEngineLogWriter
		{
			int id;

			public LogWriter (int loggerId, MSBuildEvent eventFilter)
			{
				this.id = loggerId;
				RequiredEvents = eventFilter;
			}

			public void Write (string text, LogEvent [] events)
			{
				server.SendMessage (new LogMessage { LoggerId = id, LogText = text, Events = events });
			}

			public MSBuildEvent RequiredEvents { get; private set; }
		}

		public class NullLogWriter: IEngineLogWriter
		{
			public void Write (string text, LogEvent [] events)
			{
			}

			public MSBuildEvent RequiredEvents {
				get { return default (MSBuildEvent); }
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
				server.Shutdown ();
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
			msbuildBinDir = msg.BinDir;
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
		public BinaryMessage Ping (PingRequest msg)
		{
			if (fatalErrorRetries <= 0)
				throw new Exception ("Too many fatal exceptions");
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
				var logger = msg.LogWriterId != -1 ? (IEngineLogWriter) new LogWriter (msg.LogWriterId, msg.EnabledLogEvents) : (IEngineLogWriter) new NullLogWriter ();
				var res = pb.Run (msg.Configurations, logger, msg.Verbosity, msg.BinLogFilePath, msg.RunTargets, msg.EvaluateItems, msg.EvaluateProperties, msg.GlobalProperties, msg.TaskId);
				return new RunProjectResponse { Result = res };
			}
			return msg.CreateResponse ();
		}

		[MessageHandler]
		public BinaryMessage BeginBuild (BeginBuildRequest msg)
		{
			var logger = msg.LogWriterId != -1 ? (IEngineLogWriter)new LogWriter (msg.LogWriterId, msg.EnabledLogEvents) : (IEngineLogWriter)new NullLogWriter ();
			BeginBuildOperation (logger, msg.BinLogFilePath, msg.Verbosity, msg.Configurations);
			return msg.CreateResponse ();
		}

		[MessageHandler]
		public BinaryMessage EndBuild (EndBuildRequest msg)
		{
			EndBuildOperation ();
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

				AutoResetEvent doneEvent;

				lock (threadLock) {
					// Last chance to check for canceled task before the thread is started
					if (IsTaskCancelled (taskId))
						return;

					doneEvent = workDoneEvent = new AutoResetEvent (false);
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

				doneEvent.WaitOne ();

				ResetCurrentTask ();
			}
			if (workError != null) {
				if (workError is OutOfMemoryException)
					fatalErrorRetries = 0;
				else
					fatalErrorRetries--;
				throw new Exception ("MSBuild operation failed", workError);
			}
		}

		static readonly object threadLock = new object ();
		
		static void STARunner ()
		{
			try {
				lock (threadLock) {
					do {
						var doneEvent = workDoneEvent;
						try {
							workDelegate ();
						} catch (ThreadAbortException) {
							// Gracefully stop the thread
							Thread.ResetAbort ();
							return;
						} catch (Exception ex) {
							workError = ex;
						}
						doneEvent.Set ();
					}
					while (Monitor.Wait (threadLock, 60000));

					workThread = null;
				}
			} catch (ThreadAbortException) {
				// Gracefully stop the thread
				Thread.ResetAbort ();
			}
		}
	}

	interface IEngineLogWriter
	{
		void Write (string text, LogEvent[] events);
		MSBuildEvent RequiredEvents { get; }
	}
}