using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using ST = System.Threading;

using Mono.Debugger;

using MD = Mono.Debugger;
using ML = Mono.Debugger.Languages;
using DL = Mono.Debugging.Client;
using DB = Mono.Debugging.Backend;

using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Backend.Mdb;

namespace DebuggerServer
{
	class DebuggerServer : MarshalByRefObject, IDebuggerServer, ISponsor
	{
		IDebuggerController controller;
		MD.Debugger debugger;
		MD.DebuggerSession session;
		MD.Process process;
		MD.GUIManager guiManager;
		MdbAdaptor mdbAdaptor;
		int max_frames;
		List<ST.WaitCallback> stoppedWorkQueue = new List<ST.WaitCallback> ();
		List<ST.WaitCallback> eventQueue = new List<ST.WaitCallback> ();
		bool initializing;
		bool running;
		bool exited;
		NRefactoryEvaluator evaluator = new NRefactoryEvaluator ();
		MD.Thread activeThread;
		Dictionary<int,BreakEvent> events = new Dictionary<int,BreakEvent> ();
		Dictionary<int,string> lastConditionValue = new Dictionary<int,string> ();
		
		DateTime lastBreakEventUpdate = DateTime.Now;
		Dictionary<int, ST.WaitCallback> breakUpdates = new Dictionary<int,ST.WaitCallback> ();
		bool breakUpdateEventsQueued;
		
		MdbObjectValueAdaptor mdbObjectValueAdaptor;
		
		const int BreakEventUpdateNotifyDelay = 500;
		public DebuggerSessionOptions SessionOptions;
		
		public DebuggerServer (IDebuggerController dc)
		{
			this.controller = dc;
			mdbObjectValueAdaptor = new MdbObjectValueAdaptor ();
			MarshalByRefObject mbr = (MarshalByRefObject)controller;
			ILease lease = mbr.GetLifetimeService() as ILease;
			lease.Register(this);
			max_frames = 100;
				
			ST.Thread t = new ST.Thread ((ST.ThreadStart)EventDispatcher);
			t.IsBackground = true;
			t.Start ();
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}
		
		public MdbObjectValueAdaptor MdbObjectValueAdaptor {
			get { return mdbObjectValueAdaptor; }
		}
		
		public Mono.Debugging.Evaluation.ExpressionEvaluator Evaluator {
			get { return evaluator; }
		}

		public MdbAdaptor MdbAdaptor {
			get { return mdbAdaptor; }
		}

		#region IDebugger Members
		
		public string InitializeMdb (string mdbVersion, int mdPid)
		{
			mdbAdaptor = MdbAdaptorFactory.CreateAdaptor (mdbVersion);
			WatchParentProcess (mdPid);
			return mdbAdaptor.MdbVersion;
		}
	
		void WatchParentProcess (int pid)
		{
			ST.Thread t = new ST.Thread (delegate () {
				while (true) {
					try {
						System.Diagnostics.Process.GetProcessById (pid);
					} catch {
						ForceShutdown ();
					}
					ST.Thread.Sleep (1000);
				}
			});
			t.IsBackground = true;
			t.Start ();
		}
		
		void ForceShutdown ()
		{
			try {
				Exit ();
			} catch {
			}
			Environment.Exit (1);
		}

		public void Run (MonoDebuggerStartInfo startInfo, DebuggerSessionOptions sessionOptions)
		{
			try {
				if (startInfo == null)
					throw new ArgumentNullException ("startInfo");
			
				Console.WriteLine ("MDB version: " + mdbAdaptor.MdbVersion);
				
				this.SessionOptions = sessionOptions;
				mdbAdaptor.StartInfo = startInfo;

				Report.Initialize ();

				DebuggerConfiguration config = new DebuggerConfiguration ();
				config.LoadConfiguration ();
				mdbAdaptor.Configuration = config;
				mdbAdaptor.InitializeConfiguration ();
				
				debugger = new MD.Debugger (config);
				
				debugger.ModuleLoadedEvent += OnModuleLoadedEvent;
				debugger.ModuleUnLoadedEvent += OnModuleUnLoadedEvent;
				
				debugger.ProcessReachedMainEvent += delegate (MD.Debugger deb, MD.Process proc) {
					OnInitialized (deb, proc);
				};

				if (startInfo.IsXsp) {
					mdbAdaptor.SetupXsp ();
					config.FollowFork = false;
				}
				config.OpaqueFileNames = false;
				
				DebuggerOptions options = DebuggerOptions.ParseCommandLine (new string[] { startInfo.Command } );
				options.WorkingDirectory = startInfo.WorkingDirectory;
				Environment.CurrentDirectory = startInfo.WorkingDirectory;
				options.StopInMain = false;
				
				if (!string.IsNullOrEmpty (startInfo.Arguments))
					options.InferiorArgs = ToArgsArray (startInfo.Arguments);
				
				if (startInfo.EnvironmentVariables != null) {
					foreach (KeyValuePair<string,string> env in startInfo.EnvironmentVariables)
						options.SetEnvironment (env.Key, env.Value);
				}
				session = new MD.DebuggerSession (config, options, "main", null);
				mdbAdaptor.Session = session;
				mdbAdaptor.InitializeSession ();
				
				ST.ThreadPool.QueueUserWorkItem (delegate {
					// Run in a thread to avoid a deadlock, since NotifyStarted calls back to the client.
					NotifyStarted ();
					debugger.Run(session);
				});

			} catch (Exception e) {
				Console.WriteLine ("error: " + e.ToString ());
				throw;
			}
		}
		
		public void AttachToProcess (long pid, DebuggerSessionOptions sessionOptions)
		{
			Report.Initialize ();

			this.SessionOptions = sessionOptions;
			DebuggerConfiguration config = new DebuggerConfiguration ();
			mdbAdaptor.Configuration = config;
			mdbAdaptor.InitializeConfiguration ();
			config.LoadConfiguration ();
			debugger = new MD.Debugger (config);

			DebuggerOptions options = DebuggerOptions.ParseCommandLine (new string[0]);
			options.StopInMain = false;
			session = new MD.DebuggerSession (config, options, "main", (IExpressionParser) null);
			mdbAdaptor.Session = session;
			
			Process proc = debugger.Attach (session, (int)pid);
			OnInitialized (debugger, proc);
			
			ST.ThreadPool.QueueUserWorkItem (delegate {
				NotifyStarted ();
			});
		}
		
		public void Detach ()
		{
			CancelRuntimeInvokes ();
			try {
				debugger.Detach ();
			} catch (Exception ex) {
				Console.WriteLine (ex);
			} finally {
				running = false;
			}
		}

		public void Stop ()
		{
			CancelRuntimeInvokes ();
			QueueTask (delegate {
				guiManager.Stop (process.MainThread);
			});
		}

		public void Exit ()
		{
			CancelRuntimeInvokes ();
			ResetTaskQueue ();
			debugger.Kill ();
			running = false;
		}

		public void NextLine ()
		{
			if (running)
				throw new InvalidOperationException ("Target already running");
			OnStartRunning ();
			guiManager.StepOver (activeThread);
		}

		public void StepLine ()
		{
			if (running)
				throw new InvalidOperationException ("Target already running");
			OnStartRunning ();
			guiManager.StepInto (activeThread);
		}

		public void StepInstruction ()
		{
			if (running)
				throw new InvalidOperationException ("Target already running");
			OnStartRunning ();
			activeThread.StepInstruction ();
		}

		public void NextInstruction ()
		{
			if (running)
				throw new InvalidOperationException ("Target already running");
			OnStartRunning ();
			activeThread.NextInstruction ();
		}

		public void Finish ()
		{
			if (running)
				throw new InvalidOperationException ("Target already running");
			OnStartRunning ();
			guiManager.StepOut (activeThread);
		}

		public void Continue ()
		{
			if (running)
				throw new InvalidOperationException ("Target already running");
			OnStartRunning ();
			QueueTask (delegate {
				guiManager.Continue (activeThread);
			});
		}
		
		public bool AllowBreakpointChanges {
			get {
				return mdbAdaptor == null || mdbAdaptor.AllowBreakEventChanges;
			}
		}

		public int InsertBreakEvent (DL.BreakEvent be, bool enable)
		{
			CancelRuntimeInvokes ();
			DL.Breakpoint bp = be as DL.Breakpoint;
			MD.Event ev = null;
			
			if (bp != null) {
				MD.SourceLocation location = new MD.SourceLocation (bp.FileName, bp.Line);
				MD.SourceBreakpoint sbp = new MD.SourceBreakpoint (session, ThreadGroup.Global, location);
				mdbAdaptor.InitializeBreakpoint (sbp);
				session.AddEvent (sbp);
				ev = sbp;
			}
			else if (be is Catchpoint) {
				Catchpoint cp = (Catchpoint) be;
				ML.TargetType exc = null;
				foreach (Module mod in process.Modules) {
					exc = mod.Language.LookupType (cp.ExceptionName);
					if (exc != null)
						break;
				}
				if (exc == null)
					throw new Exception ("Unknown exception type.");
				ev = session.InsertExceptionCatchPoint (process.MainThread, ThreadGroup.Global, exc);
			}
			
			ev.IsEnabled = enable;
			
			if (!initializing) {
				lock (debugger) {
					mdbAdaptor.ActivateEvent (ev);
				}
			}
						                    
			if (bp != null && !running && !initializing && activeThread.CurrentFrame != null && !string.IsNullOrEmpty (bp.ConditionExpression) && bp.BreakIfConditionChanges) {
				// Initial expression evaluation
				MdbEvaluationContext ctx = new MdbEvaluationContext (activeThread, activeThread.CurrentFrame, SessionOptions.EvaluationOptions);
				ML.TargetObject ob = EvaluateExp (ctx, bp.ConditionExpression);
				if (ob != null)
					lastConditionValue [ev.Index] = evaluator.TargetObjectToExpression (ctx, ob).Value;
			}
			
			events [ev.Index] = be;
			return ev.Index;
		}
		
		public void RemoveBreakEvent (int handle)
		{
			if (exited)
				return;
			CancelRuntimeInvokes ();
			Event ev = session.GetEvent (handle);
			mdbAdaptor.RemoveEvent (ev);
		}
		
		public void EnableBreakEvent (int handle, bool enable)
		{
			CancelRuntimeInvokes ();
			Event ev = session.GetEvent (handle);
			if (enable)
				ev.Activate (process.MainThread);
			else
				ev.Deactivate (process.MainThread);
		}
		
		public object UpdateBreakEvent (object handle, DL.BreakEvent bp)
		{
			events [(int)handle] = bp;
			return handle;
		}

		bool BreakEventCheck (MD.TargetEventArgs args)
		{
			MD.StackFrame frame = args.Frame;
			if (!(args.Data is int))
				return true;
			
			int eventHandle = (int) args.Data;
			
			DL.BreakEvent be;
			if (!events.TryGetValue (eventHandle, out be))
				return true;
			
			// Check hit count
			if (be.HitCount > 0) {
				be.HitCount--;
				DispatchEvent (delegate {
					NotifyBreakEventUpdate (eventHandle, be.HitCount, null);
				});
				return false;
			}

			MdbEvaluationContext ctx = new MdbEvaluationContext (frame.Thread, frame, SessionOptions.EvaluationOptions);
			DL.Breakpoint bp = be as DL.Breakpoint;
			if (bp != null && !string.IsNullOrEmpty (bp.ConditionExpression)) {
				ML.TargetObject val = EvaluateExp (ctx, bp.ConditionExpression);
				if (val == null)
					return false;
				if (bp.BreakIfConditionChanges) {
					string current = evaluator.TargetObjectToExpression (ctx, val).Value;
					string last;
					bool found = lastConditionValue.TryGetValue (eventHandle, out last);
					lastConditionValue [eventHandle] = current;
					if (!found || last == current)
						return false;
				} else {
					ML.TargetFundamentalObject fob = val as ML.TargetFundamentalObject;
					if (fob == null)
						return false;
					object ob = fob.GetObject (frame.Thread);
					if (!(ob is bool) || !(bool)ob)
						return false;
				}
			}

			switch (be.HitAction) {
				case HitAction.Break:
					return true;
				case HitAction.CustomAction:
					return controller.OnCustomBreakpointAction (be.CustomActionId, eventHandle);
				case HitAction.PrintExpression:
					if (string.IsNullOrEmpty (be.TraceExpression) || frame == null)
						return false;
					ML.TargetObject val = EvaluateExp (ctx, be.TraceExpression);
					if (val != null) {
						string str = evaluator.TargetObjectToString (ctx, val);
						DispatchEvent (delegate {
							controller.OnTargetOutput (false, str + "\n");
							NotifyBreakEventUpdate (eventHandle, -1, str);
						});
					}
					return false;
			}
			return false;
		}
		
		ML.TargetObject EvaluateExp (EvaluationContext ctx, string exp)
		{
			ValueReference var;
			try {
				EvaluationContext cctx = ctx.Clone ();
				EvaluationOptions ops = cctx.Options;
				ops.AllowMethodEvaluation = true;
				cctx.Options = ops;
				var = (ValueReference) Server.Instance.Evaluator.Evaluate (cctx, exp);
				return (ML.TargetObject) var.Value;
			} catch {
				return null;
			}
		}
		
		void NotifyBreakEventUpdate (int eventHandle, int hitCount, string lastTrace)
		{
			bool notify = false;
			
			lock (breakUpdates)
			{
				int span = (int) (DateTime.Now - lastBreakEventUpdate).TotalMilliseconds;
				if (span >= BreakEventUpdateNotifyDelay && !breakUpdateEventsQueued) {
					// Last update was more than 0.5s ago. The update can be sent.
					lastBreakEventUpdate = DateTime.Now;
					notify = true;
				} else {
					// Queue the event notifications to avoid wasting too much time
					breakUpdates [eventHandle] = delegate {
						controller.UpdateBreakpoint (eventHandle, hitCount, lastTrace);
					};
					if (!breakUpdateEventsQueued) {
						breakUpdateEventsQueued = true;
						
						ST.ThreadPool.QueueUserWorkItem (delegate {
							ST.Thread.Sleep (BreakEventUpdateNotifyDelay - span);
							List<ST.WaitCallback> copy;
							lock (breakUpdates) {
								copy = new List<ST.WaitCallback> (breakUpdates.Values);
								breakUpdates.Clear ();
								breakUpdateEventsQueued = false;
								lastBreakEventUpdate = DateTime.Now;
							}
							foreach (ST.WaitCallback wc in copy)
								wc (null);
						});
					}
				}
			}
			if (notify)
				controller.UpdateBreakpoint (eventHandle, hitCount, lastTrace);
		}

		public ThreadInfo[] GetThreads (long processId)
		{
			MD.Process p = GetProcess (processId);
			if (p == null)
				return new ThreadInfo [0];
			List<DL.ThreadInfo> list = new List<DL.ThreadInfo> ();
			foreach (MD.Thread t in p.GetThreads ()) {
				DL.ThreadInfo ct = CreateThreadInfo (t);
				list.Add (ct);
			}
			return list.ToArray ();
		}
		
		public ProcessInfo[] GetProcesses ()
		{
			List<DL.ProcessInfo> list = new List<DL.ProcessInfo> ();
			foreach (MD.Process p in debugger.Processes)
				list.Add (new DL.ProcessInfo (p.ID, p.TargetApplication + " " + string.Join (" ", p.CommandLineArguments)));
			return list.ToArray ();
		}
		
		ThreadInfo CreateThreadInfo (MD.Thread t)
		{
			string loc;
			if (t.CurrentFrame != null && t.CurrentFrame.SourceLocation != null) {
				loc = t.CurrentFrame.ToString ();
			} else
				loc = "<Unknown>";
			
			return new ThreadInfo (t.Process.ID, t.ID, t.Name, loc);
		}
		
		public DL.Backtrace GetThreadBacktrace (long processId, long threadId)
		{
			MD.Thread t = GetThread (processId, threadId);
			if (t != null && t.IsStopped)
				return CreateBacktrace (t);
			else
				return null;
		}
		
		public void SetActiveThread (long processId, long threadId)
		{
			activeThread = GetThread (processId, threadId);
		}

		MD.Thread GetThread (long procId, long threadId)
		{
			MD.Process proc = GetProcess (procId);
			if (proc != null) {
				foreach (MD.Thread t in proc.GetThreads ()) {
					if (t.ID == (int)threadId)
						return t;
				}
			}
			return null;
		}
		
		MD.Process GetProcess (long id)
		{
			foreach (MD.Process p in debugger.Processes) {
				if (p.ID == (int)id)
					return p;
			}
			return null;
		}

		public AssemblyLine[] DisassembleFile (string file)
		{
			CancelRuntimeInvokes ();
			
			// Not working yet
			return null;
			
/*			SourceFile sourceFile = session.FindFile (file);
			List<AssemblyLine> lines = new List<AssemblyLine> ();
			foreach (MethodSource met in sourceFile.Methods) {
				TargetAddress addr = met.NativeMethod.StartAddress;
				TargetAddress endAddr = met.NativeMethod.EndAddress;
				while (addr < endAddr) {
					SourceAddress line = met.NativeMethod.LineNumberTable.Lookup (addr);
					AssemblerLine aline = process.MainThread.DisassembleInstruction (met.NativeMethod, addr);
					if (aline != null) {
						if (line != null)
							lines.Add (new DL.AssemblyLine (addr.Address, aline.Text, line.Row));
						else
							lines.Add (new DL.AssemblyLine (addr.Address, aline.Text));
						addr += aline.InstructionSize;
					} else
						addr++;
				}
			}
			lines.Sort (delegate (DL.AssemblyLine l1, DL.AssemblyLine l2) {
				return l1.SourceLine.CompareTo (l2.SourceLine);
			});
			return lines.ToArray ();
						*/
		}
		
		#endregion

		public void Dispose ()
		{
			MarshalByRefObject mbr = (MarshalByRefObject)controller;
			ILease lease = mbr.GetLifetimeService() as ILease;
			lease.Unregister(this);
		}
		
		public void WriteDebuggerOutput (string msg, params object[] args)
		{
			DispatchEvent (delegate {
				controller.OnDebuggerOutput (false, string.Format (msg, args));
			});
		}
		
		public void WriteDebuggerError (Exception ex)
		{
			if (ex is EvaluatorException)
				Console.WriteLine (ex.Message);
			else
				Console.WriteLine (ex);
		}
		
		public ML.TargetObject RuntimeInvoke (MdbEvaluationContext ctx, ML.TargetFunctionType function,
							  ML.TargetStructObject object_argument,
							  params ML.TargetObject[] param_objects)
		{
			MethodCall mc = new MethodCall (ctx, function, object_argument, param_objects);
			ctx.Adapter.AsyncExecute (mc, ctx.Options.EvaluationTimeout);
			return mc.ReturnValue;
		}

		DL.Backtrace CreateBacktrace (MD.Thread thread)
		{
			List<MD.StackFrame> frames = new List<MD.StackFrame> ();
			DateTime t = DateTime.Now;
			if (!thread.CurrentFrame.Language.IsManaged) {
				MD.Backtrace bt = thread.GetBacktrace (MD.Backtrace.Mode.Native, max_frames);
				if (bt != null) {
					Console.WriteLine ("GetBacktrace native time: {0} ms n:{1}", (DateTime.Now - t).TotalMilliseconds, bt.Count);
					frames.AddRange (bt.Frames);
				}
			} else {
				t = DateTime.Now;
				MD.Backtrace backtrace = thread.GetBacktrace (MD.Backtrace.Mode.Managed, max_frames);
				if (backtrace != null) {
					Console.WriteLine ("GetBacktrace managed time: {0} ms n:{1}", (DateTime.Now - t).TotalMilliseconds, backtrace.Count);
					frames.AddRange (backtrace.Frames);
				}
			}
			if (frames.Count > 0) {
				BacktraceWrapper wrapper = new BacktraceWrapper (frames.ToArray ());
				return new DL.Backtrace (wrapper);
			} else if (thread.CurrentBacktrace != null) {
				BacktraceWrapper wrapper = new BacktraceWrapper (thread.CurrentBacktrace.Frames);
				return new DL.Backtrace (wrapper);
			}
			return null;
		}

		#region ISponsor Members

		public TimeSpan Renewal(ILease lease)
		{
			return TimeSpan.FromSeconds(7);
		}

		#endregion

		private void OnInitialized (MD.Debugger debugger, Process process)
		{
			Console.WriteLine (">> OnInitialized");
			
			this.process = process;
			this.debugger = debugger;
			
			mdbAdaptor.Process = process;
			
			guiManager = process.StartGUIManager ();

			//FIXME: conditionally add event handlers
			process.TargetOutputEvent += OnTargetOutput;
			
			debugger.ProcessCreatedEvent += OnProcessCreatedEvent;
			debugger.ProcessExecdEvent += OnProcessExecdEvent;
			debugger.ProcessExitedEvent += OnProcessExitedEvent;
			
			debugger.ThreadCreatedEvent += OnThreadCreatedEvent;
			debugger.ThreadExitedEvent += OnThreadExitedEvent;
			
			debugger.TargetExitedEvent += OnTargetExitedEvent;
			guiManager.TargetEvent += OnTargetEvent;

			// Not supported
			//guiManager.BreakpointHitHandler = BreakEventCheck;
			
			activeThread = process.MainThread;
			running = true;
			
			Console.WriteLine ("<< OnInitialized");
		}
		
		void NotifyStarted ()
		{
			try {
				initializing = true;
				controller.NotifyStarted();
				initializing = false;
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		void OnTargetOutput (bool is_stderr, string text)
		{
			DispatchEvent (delegate {
				controller.OnTargetOutput (is_stderr, text);
			});
		}
		
		void QueueTask (ST.WaitCallback cb)
		{
			lock (debugger) {
				if (stoppedWorkQueue.Count > 0)
					stoppedWorkQueue.Add (cb);
				else
					cb (null);
			}
		}
		
		void ResetTaskQueue ()
		{
			lock (debugger) {
				stoppedWorkQueue.Clear ();
			}
		}
		
		void LogEvent (MD.TargetEventArgs args)
		{
			Console.WriteLine ("Server OnTargetEvent: {0} stopped:{1} data:{2} queue:{3} thread:{4} running:{5}", args.Type, args.IsStopped, args.Data, stoppedWorkQueue.Count, args.Frame != null ? args.Frame.Thread : null, running);
		}

		private void OnTargetEvent (MD.Thread thread, MD.TargetEventArgs args)
		{
			try {
				if (!running) {
					LogEvent (args);
					return;
				}
				
				bool notifyToClient = args.IsStopped || args.Type == MD.TargetEventType.UnhandledException || args.Type == MD.TargetEventType.Exception || args.Type == MD.TargetEventType.TargetInterrupted;
				
				LogEvent (args);
				
				bool isStop = args.Type != MD.TargetEventType.FrameChanged &&
					args.Type != MD.TargetEventType.TargetExited &&
					args.Type != MD.TargetEventType.TargetRunning;
				
				if (isStop) {
					
					lock (debugger) {
						
						if (stoppedWorkQueue.Count > 0) {
							// Execute queued work in another thread with a small delay
							// since it is not safe to execute it here
							System.Threading.ThreadPool.QueueUserWorkItem (delegate {
								System.Threading.Thread.Sleep (50);
								bool resume = false;
								lock (debugger) {
									foreach (ST.WaitCallback cb in stoppedWorkQueue) {
										cb (null);
									}
									stoppedWorkQueue.Clear ();
								}
								if (resume)
									guiManager.Continue (process.MainThread);
								else if (notifyToClient)
									NotifyTargetEvent (thread, args);
							});
							return;
						}
					}
				}
				
				if (notifyToClient)
					NotifyTargetEvent (thread, args);

			} catch (Exception e) {
				Console.WriteLine ("*** DS.OnTargetEvent1, exception : {0}", e.ToString ());
			}
		}
		
		void NotifyTargetEvent (MD.Thread thread, MD.TargetEventArgs args)
		{
			if (args.Frame != null)
				activeThread = args.Frame.Thread;
	
			try {
				if (args.Type == MD.TargetEventType.TargetStopped && ((int)args.Data) != 0) {
					DispatchEvent (delegate {
						controller.OnDebuggerOutput (false, string.Format ("Thread {0:x} received signal {1}.\n", args.Frame.Thread.ID, args.Data));
					});
				}

				DL.TargetEventType type;
				
				switch (args.Type) {
					case MD.TargetEventType.Exception: type = DL.TargetEventType.ExceptionThrown; break;
					case MD.TargetEventType.TargetHitBreakpoint: type = DL.TargetEventType.TargetHitBreakpoint; break;
					case MD.TargetEventType.TargetInterrupted: type = DL.TargetEventType.TargetInterrupted; break;
					case MD.TargetEventType.TargetSignaled: type = DL.TargetEventType.TargetSignaled; break;
					case MD.TargetEventType.TargetStopped: type = DL.TargetEventType.TargetStopped; break;
					case MD.TargetEventType.UnhandledException: type = DL.TargetEventType.UnhandledException; break;
					default:
						return;
				}

				OnCleanFrameData ();
				
				DL.TargetEventArgs targetArgs = new DL.TargetEventArgs (type);

				if (args.Type != MD.TargetEventType.TargetExited) {
					targetArgs.Backtrace = CreateBacktrace (thread);
					targetArgs.Thread = CreateThreadInfo (activeThread);
				}

				if ((args.Type == MD.TargetEventType.UnhandledException || args.Type == MD.TargetEventType.Exception) && (args.Data is TargetAddress)) {
					MdbEvaluationContext ctx = new MdbEvaluationContext (args.Frame.Thread, args.Frame, SessionOptions.EvaluationOptions);
					targetArgs.Exception = LiteralValueReference.CreateTargetObjectLiteral (ctx, "Exception", args.Frame.ExceptionObject).CreateObjectValue ();
				}

				running = false;

				DispatchEvent (delegate {
					controller.OnTargetEvent (targetArgs);
				});
			} catch (Exception e) {
				Console.WriteLine ("*** DS.OnTargetEvent2, exception : {0}", e.ToString ());
			}
		}

		void OnStartRunning ()
		{
			OnCleanFrameData ();
			running = true;
		}

		void OnCleanFrameData ()
		{
			// Dispose all previous remote objects
			RemoteFrameObject.DisconnectAll ();
			CancelRuntimeInvokes ();
		}

		public void CancelRuntimeInvokes ()
		{
			mdbObjectValueAdaptor.CancelAsyncOperations ();
		}
		
/*		public void WaitRuntimeInvokes ()
		{
			invokeManager.WaitForAll ();
		}
*/		
		private void OnProcessCreatedEvent (MD.Debugger debugger, MD.Process process)
		{
			WriteDebuggerOutput (string.Format ("Process {0} created.\n", process.ID));
		}
		
		private void OnProcessExitedEvent (MD.Debugger debugger, MD.Process process)
		{
			WriteDebuggerOutput (string.Format ("Process {0} exited.\n", process.ID));
		}
		
		private void OnProcessExecdEvent (MD.Debugger debugger, MD.Process process)
		{
			WriteDebuggerOutput (string.Format ("Process {0} execd.\n", process.ID));
		}
		
		private void OnThreadCreatedEvent (MD.Debugger debugger, MD.Thread thread)
		{
			WriteDebuggerOutput (string.Format ("Thread {0} created.\n", thread.ID));
		}
		
		private void OnThreadExitedEvent (MD.Debugger debugger, MD.Thread thread)
		{
			WriteDebuggerOutput (string.Format ("Thread {0} exited.\n", thread.ID));
		}
		
		private void OnModuleLoadedEvent (Module module)
		{
			SourceFile[] sfiles = module.Sources;
			string[] files = new string [sfiles.Length];
			for (int n=0; n<files.Length; n++)
				files [n] = sfiles [n].FileName;
			controller.NotifySourceFileLoaded (files);
			WriteDebuggerOutput (string.Format ("Module loaded: {0}.\n", module.Name));
		}

		private void OnModuleUnLoadedEvent (Module module)
		{
			SourceFile[] sfiles = module.Sources;
			string[] files = new string [sfiles.Length];
			for (int n=0; n<files.Length; n++)
				files [n] = sfiles [n].FileName;
			controller.NotifySourceFileUnloaded (files);
			WriteDebuggerOutput (string.Format ("Module unloaded: {0}.\n", module.Name));
		}

		private void OnTargetExitedEvent (MD.Debugger debugger)
		{
			exited = true;
			DispatchEvent (delegate {
				controller.OnDebuggerOutput (false, "Target exited.\n");
				DL.TargetEventArgs args = new DL.TargetEventArgs (DL.TargetEventType.TargetExited);
				controller.OnTargetEvent (args);
			});
		}

		void DispatchEvent (ST.WaitCallback eventCallback)
		{
			lock (eventQueue) {
				eventQueue.Add (eventCallback);
				ST.Monitor.PulseAll (eventQueue);
			}
		}
		
		void EventDispatcher ()
		{
			while (true) {
				ST.WaitCallback[] cbs;
				lock (eventQueue) {
					if (eventQueue.Count == 0)
						ST.Monitor.Wait (eventQueue);
					cbs = new ST.WaitCallback [eventQueue.Count];
					eventQueue.CopyTo (cbs, 0);
					eventQueue.Clear ();
				}
					
				foreach (ST.WaitCallback wc in cbs) {
					try {
						wc (null);
					} catch (Exception ex) {
						Console.WriteLine (ex);
					}
				}
			}
		}
		
		string[] ToArgsArray (string str)
		{
			List<string> args = new List<string> ();
			bool escaping = false;
			StringBuilder currentArg = new StringBuilder ();
			for (int n=0; n<str.Length; n++) {
				char c = str [n];
				if (c == '\\' && n + 1 < str.Length) {
					if (!escaping || (escaping && (str[n+1] == '"' || str[n+1] == '\\')))
						c = str [++n];
				} else if (c == '"') {
					escaping = !escaping;
					continue;
				} else if (c == ' ' && !escaping) {
					args.Add (currentArg.ToString ());
					currentArg = new StringBuilder ();
					continue;
				}
				currentArg.Append (c);
			}
			if (currentArg.Length > 0)
				args.Add (currentArg.ToString ());
			return args.ToArray ();
		}
	}
}
