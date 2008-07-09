using System;
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
		int max_frames;
		bool internalInterruptionRequested;
		List<ST.WaitCallback> stoppedWorkQueue = new List<ST.WaitCallback> ();
		List<ST.WaitCallback> eventQueue = new List<ST.WaitCallback> ();
		bool initializing;
		bool running;
		ExpressionEvaluator evaluator = new NRefactoryEvaluator ();
		MD.Thread activeThread;

		public DebuggerServer (IDebuggerController dc)
		{
			this.controller = dc;
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
		
		public ExpressionEvaluator Evaluator {
			get { return evaluator; }
		}

		#region IDebugger Members

		public void Run (DL.DebuggerStartInfo startInfo)
		{
			try {
				if (startInfo == null)
					throw new ArgumentNullException ("startInfo");

				Report.Initialize ();

				DebuggerConfiguration config = new DebuggerConfiguration ();
				config.LoadConfiguration ();
				debugger = new MD.Debugger (config);
				
				debugger.ProcessReachedMainEvent += delegate (MD.Debugger deb, MD.Process proc) {
					OnInitialized (deb, proc);
					NotifyStarted ();
				};

				DebuggerOptions options = DebuggerOptions.ParseCommandLine (new string[] { startInfo.Command } );
				options.StopInMain = false;
				session = new MD.DebuggerSession (config, options, "main", null);
				
				debugger.Run(session);

			} catch (Exception e) {
				Console.WriteLine ("error: " + e.ToString ());
				throw;
			}
		}
		
		public void AttachToProcess (int pid)
		{
			Report.Initialize ();

			DebuggerConfiguration config = new DebuggerConfiguration ();
			config.LoadConfiguration ();
			debugger = new MD.Debugger (config);

			DebuggerOptions options = DebuggerOptions.ParseCommandLine (new string[0]);
			options.StopInMain = false;
			session = new MD.DebuggerSession (config, options, "main", (IExpressionParser) null);
			
			Process proc = debugger.Attach (session, pid);
			OnInitialized (debugger, proc);
			
			ST.ThreadPool.QueueUserWorkItem (delegate {
				NotifyStarted ();
			});
		}
		
		public void Detach ()
		{
			RunWhenStopped (delegate {
				try {
					debugger.Detach ();
				} catch (Exception ex) {
					Console.WriteLine (ex);
				} finally {
					running = false;
				}
			});
		}

		public void Stop ()
		{
			QueueTask (delegate {
				if (internalInterruptionRequested) {
					// Stop already internally requested. By resetting the flag, the interruption
					// won't be considered internal anymore and the session won't be automatically restarted.
					internalInterruptionRequested = false;
				}
				else
					process.MainThread.Stop ();
				running = false;
			});
		}

		public void Exit ()
		{
			ResetTaskQueue ();
			debugger.Kill ();
			running = false;
		}

		public void NextLine ()
		{
			running = true;
			guiManager.StepOver (activeThread);
		}

		public void StepLine ()
		{
			running = true;
			guiManager.StepInto (activeThread);
		}

		public void StepInstruction ()
		{
			running = true;
			activeThread.StepInstruction ();
		}

		public void NextInstruction ()
		{
			running = true;
			activeThread.NextInstruction ();
		}

		public void Finish ()
		{
			running = true;
			guiManager.StepOut (activeThread);
		}

		public int InsertBreakpoint (string filename, int line, bool enable)
		{
			MD.SourceLocation location = FindFile (filename, line);
			if (location == null)
				//FIXME: exception types
				throw new Exception ("invalid location: " + filename + ":" + line);

			MD.Event ev = null;
			
			ev = session.InsertBreakpoint (ThreadGroup.Global, location);
			ev.IsEnabled = enable;
			
			if (!initializing) {
				Exception error = null;
				RunWhenStopped (delegate {
					try {
						ev.Activate (process.MainThread);
					} catch (Exception ex) {
						error = ex;
						Console.WriteLine (ex);
					}
				});
				if (error != null)
					throw new Exception ("Breakpoint could not be set: " + error.Message);
			}
			
			return ev.Index;
		}

		public void RemoveBreakpoint (int handle)
		{
			//FIXME: handle errors
			QueueTask (delegate {
				Event ev = session.GetEvent (handle);
				session.DeleteEvent (ev);
			});
		}
		
		public void EnableBreakpoint (int handle, bool enable)
		{
			RunWhenStopped (delegate {
				Event ev = session.GetEvent (handle);
				if (enable)
					ev.Activate (process.MainThread);
				else
					ev.Deactivate (process.MainThread);
			});
		}

		MD.SourceLocation FindFile (string filename, int line)
		{
			SourceFile file = session.FindFile (filename);
			if (file == null)
				return null;

			MethodSource source = file.FindMethod (line);
			if (source == null)
				return null;

			return new MD.SourceLocation (source, file, line);
		}

		public void Continue ()
		{
			QueueTask (delegate {
				running = true;
				guiManager.Continue (activeThread);
			});
		}
		
		public ThreadInfo[] GetThreads (int processId)
		{
			MD.Process p = GetProcess (processId);
			if (p == null)
				return new ThreadInfo [0];
			List<DL.ThreadInfo> list = new List<DL.ThreadInfo> ();
			foreach (MD.Thread t in p.GetThreads ()) {
				DL.ThreadInfo ct = new DL.ThreadInfo (processId, t.ID, t.Name);
				list.Add (ct);
			}
			return list.ToArray ();
		}
		
		public ProcessInfo[] GetPocesses ()
		{
			List<DL.ProcessInfo> list = new List<DL.ProcessInfo> ();
			foreach (MD.Process p in debugger.Processes)
				list.Add (new DL.ProcessInfo (p.ID, p.TargetApplication + " " + string.Join (" ", p.CommandLineArguments)));
			return list.ToArray ();
		}
		
		public DL.Backtrace GetThreadBacktrace (int processId, int threadId)
		{
			MD.Thread t = GetThread (processId, threadId);
			if (t != null && t.IsStopped)
				return CreateBacktrace (t);
			else
				return null;
		}
		
		public void SetActiveThread (int processId, int threadId)
		{
			activeThread = GetThread (processId, threadId);
		}

		MD.Thread GetThread (int procId, int threadId)
		{
			MD.Process proc = GetProcess (threadId);
			if (proc != null) {
				foreach (MD.Thread t in proc.GetThreads ()) {
					if (t.ID == threadId)
						return t;
				}
			}
			return null;
		}
		
		MD.Process GetProcess (int id)
		{
			foreach (MD.Process p in debugger.Processes) {
				if (p.ID == id)
					return p;
			}
			return null;
		}

		public AssemblyLine[] DisassembleFile (string file)
		{
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
		
		public void WriteDebuggerOutput (string msg)
		{
			DispatchEvent (delegate {
				controller.OnDebuggerOutput (false, msg);
			});
		}
		
		public ML.TargetObject RuntimeInvoke (MD.Thread thread, ML.TargetFunctionType function,
							  ML.TargetStructObject object_argument,
							  ML.TargetObject[] param_objects)
		{
			MD.RuntimeInvokeResult res = thread.RuntimeInvoke (function, object_argument, param_objects, true, false);
			res.Wait ();
			if (res.ExceptionMessage != null)
				throw new Exception (res.ExceptionMessage);
			return res.ReturnObject;
		}
		
		DL.Backtrace CreateBacktrace (MD.Thread thread)
		{
			List<MD.StackFrame> frames = new List<MD.StackFrame> ();
			MD.Backtrace backtrace = thread.GetBacktrace (MD.Backtrace.Mode.Native, max_frames);
			if (backtrace != null)
				frames.AddRange (backtrace.Frames);
			backtrace = thread.GetBacktrace (MD.Backtrace.Mode.Managed, max_frames);
			if (backtrace != null)
				frames.AddRange (backtrace.Frames);
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
			activeThread = process.MainThread;
			running = true;
			
			Console.WriteLine ("<< OnInitialized");
		}
		
		void NotifyStarted ()
		{
			initializing = true;
			controller.OnMainProcessCreated(process.ID);
			initializing = false;
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
				internalInterruptionRequested = false;
				stoppedWorkQueue.Clear ();
			}
		}
		
		void RunWhenStopped (ST.WaitCallback cb)
		{
			lock (debugger)
			{
				if (process.MainThread.IsStopped) {
					cb (null);
					return;
				}
				stoppedWorkQueue.Add (cb);
				
				if (!internalInterruptionRequested) {
					internalInterruptionRequested = true;
					Console.WriteLine ("pp internal stop: ");
					process.MainThread.Stop ();
				}
			}
		}

		private void OnTargetEvent (object sender, MD.TargetEventArgs args)
		{
			try {
				Console.WriteLine ("Server OnTargetEvent: " + args.Type + " " + internalInterruptionRequested + " " + stoppedWorkQueue.Count + " iss:" + args.IsStopped);

				if (!running || (!args.IsStopped && args.Type != MD.TargetEventType.UnhandledException))
					return;
				
				if (args.Frame != null) {
					activeThread = args.Frame.Thread;
				}
				
				bool isStop = args.Type != MD.TargetEventType.FrameChanged &&
					args.Type != MD.TargetEventType.TargetExited &&
					args.Type != MD.TargetEventType.TargetRunning;
				
				if (isStop) {
					lock (debugger) {
						// The process was stopped, but not as a result of the internal stop request.
						// Reset the internal request flag, in order to avoid the process to be
						// automatically restarted
						if (args.Type != MD.TargetEventType.TargetInterrupted && args.Type != MD.TargetEventType.TargetStopped)
							internalInterruptionRequested = false;
						
						bool notifyThisEvent = !internalInterruptionRequested;
						
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
									if (internalInterruptionRequested) {
										internalInterruptionRequested = false;
										resume = true;
									}
								}
								if (resume)
									process.MainThread.Continue ();
								else
									NotifyTargetEvent (args);
							});
							return;
						}
						
						if (!notifyThisEvent) {
							// It's internal, don't notify the client
							return;
						}
					}
				}
				
				NotifyTargetEvent (args);

			} catch (Exception e) {
				Console.WriteLine ("*** DS.OnTargetEvent, exception : {0}", e.ToString ());
			}
		}
		
		void NotifyTargetEvent (MD.TargetEventArgs args)
		{
			try {
				if (args.Type == MD.TargetEventType.TargetStopped && ((int)args.Data) != 0) {
					DispatchEvent (delegate {
						controller.OnDebuggerOutput (false, string.Format ("Thread {0:x} received signal {1}.", args.Frame.Thread.ID, args.Data));
					});
				}
				
				DL.TargetEventType type;
				
				switch (args.Type) {
					case MD.TargetEventType.Exception: type = DL.TargetEventType.Exception; break;
					case MD.TargetEventType.TargetHitBreakpoint: type = DL.TargetEventType.TargetHitBreakpoint; break;
					case MD.TargetEventType.TargetInterrupted: type = DL.TargetEventType.TargetInterrupted; break;
					case MD.TargetEventType.TargetSignaled: type = DL.TargetEventType.TargetSignaled; break;
					case MD.TargetEventType.TargetStopped: type = DL.TargetEventType.TargetStopped; break;
					case MD.TargetEventType.UnhandledException: type = DL.TargetEventType.UnhandledException; break;
					default:
						return;
				}
				
				running = false;
				
				// Dispose all previous remote objects
				RemoteFrameObject.DisconnectAll ();
				
				DL.TargetEventArgs dl_args = new DL.TargetEventArgs (type);

				//FIXME: using Backtrace.Mode.Default right now
				//FIXME: code from BacktraceCommand.DoExecute
				//FIXME: better way than args.Frame.Thread.* ?
				
				if (args.Type != MD.TargetEventType.TargetExited)
					dl_args.Backtrace = CreateBacktrace (args.Frame.Thread);

				DispatchEvent (delegate {
					controller.OnTargetEvent (dl_args);
				});
			} catch (Exception e) {
				Console.WriteLine ("*** DS.OnTargetEvent, exception : {0}", e.ToString ());
			}
		}

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
		
		private void OnThreadCreatedEvent (MD.Debugger debugger, MD.Thread process)
		{
			WriteDebuggerOutput (string.Format ("Thread {0} created.\n", process.ID));
		}
		
		private void OnThreadExitedEvent (MD.Debugger debugger, MD.Thread process)
		{
			WriteDebuggerOutput (string.Format ("Thread {0} exited.\n", process.ID));
		}

		private void OnTargetExitedEvent (MD.Debugger debugger)
		{
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
	}
}
