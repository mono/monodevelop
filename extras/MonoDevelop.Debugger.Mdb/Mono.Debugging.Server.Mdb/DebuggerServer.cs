using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using ST = System.Threading;

using Mono.Debugger;

using MD = Mono.Debugger;
using DL = Mono.Debugging.Client;
using DB = Mono.Debugging.Backend;

using Mono.Debugging.Client;
using Mono.Debugging.Backend.Mdb;

namespace DebuggerServer
{
	class DebuggerServer : MarshalByRefObject, IDebuggerServer, ISponsor
	{
		private IDebuggerController controller;
		private MD.Debugger debugger;
		private MD.DebuggerSession session;
		private MD.Process process;
		private int max_frames;
		bool internalInterruptionRequested;
		List<ST.WaitCallback> stoppedWorkQueue = new List<ST.WaitCallback> ();
		bool initializing;

		public DebuggerServer (IDebuggerController dc)
		{
			this.controller = dc;
			MarshalByRefObject mbr = (MarshalByRefObject)controller;
			ILease lease = mbr.GetLifetimeService() as ILease;
			lease.Register(this);
			max_frames = 50;
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
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
				debugger.ProcessReachedMainEvent += new MD.ProcessEventHandler (OnInitialized);

				IExpressionParser parser = null;

				DebuggerOptions options = DebuggerOptions.ParseCommandLine (new string[] { startInfo.Command } );
				options.StopInMain = false;
				session = new MD.DebuggerSession (config, options, "main", null);
				debugger.Run(session);

			} catch (Exception e) {
				Console.WriteLine ("error: " + e.ToString ());
				throw;
			}
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
			});
		}

		public void Exit ()
		{
			lock (debugger) {
				ResetTaskQueue ();
				debugger.Kill ();
			}
		}

		public void NextLine ()
		{
			//FIXME: doesn't look like the proper way to do this.
			//can this be done for other threads/processes? how?

			lock (debugger) {
				process.MainThread.NextLine ();
			}
		}

		public void StepLine ()
		{
			//FIXME: doesn't look like the proper way to do this.
			//can this be done for other threads/processes? how?

			lock (debugger) {
				process.MainThread.StepLine ();
			}
		}

		public void Finish ()
		{
			//FIXME: param @native ?
			lock (debugger) {
				process.MainThread.Finish (false);
			}
		}

		public int InsertBreakpoint (string filename, int line, bool enable)
		{
			MD.SourceLocation location = FindFile (filename, line);
			if (location == null)
				//FIXME: exception types
				throw new Exception ("invalid location: " + filename + ":" + line);

			MD.Event ev = null;
			
			ev = session.InsertBreakpoint (process.MainThread.ThreadGroup, location);
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
				process.MainThread.Continue ();
			});
		}
		#endregion

		public void Dispose ()
		{
			MarshalByRefObject mbr = (MarshalByRefObject)controller;
			ILease lease = mbr.GetLifetimeService() as ILease;
			lease.Unregister(this);
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

			initializing = true;
			controller.OnMainProcessCreated(process.ID);
			initializing = false;

			//FIXME: conditionally add event handlers
			process.TargetOutputEvent += OnTargetOutput;
			
			debugger.ProcessCreatedEvent += OnProcessCreatedEvent;
			debugger.ProcessExecdEvent += OnProcessExecdEvent;
			debugger.ProcessExitedEvent += OnProcessExitedEvent;
			
			debugger.ThreadCreatedEvent += OnThreadCreatedEvent;
			debugger.ThreadExitedEvent += OnThreadExitedEvent;

			debugger.TargetExitedEvent += OnTargetExitedEvent;
			debugger.TargetEvent += OnTargetEvent;
			Console.WriteLine ("<< OnInitialized");
		}

		void OnTargetOutput (bool is_stderr, string text)
		{
			controller.OnTargetOutput (is_stderr, text);
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
			bool stoppedByMe = false;
			
			lock (debugger)
			{
				if (process.MainThread.IsStopped) {
					cb (null);
					return;
				}
				stoppedWorkQueue.Add (cb);
				
				if (!internalInterruptionRequested) {
					internalInterruptionRequested = true;
					process.MainThread.Stop ();
				}
			}
		}

		private void OnTargetEvent (object sender, MD.TargetEventArgs args)
		{
			try {
				Console.WriteLine ("pp OnTargetEvent: " + args.Type + " " + internalInterruptionRequested + " " + stoppedWorkQueue.Count);
				
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
				

/*					if (args.Frame.Method != null) {
						foreach (MD.Languages.TargetVariable var in args.Frame.Method.GetLocalVariables (process.MainThread)) {
							MD.Languages.TargetObject ob = var.GetObject (args.Frame);
							bool alive = var.IsInScope (args.Frame.TargetAddress);
							Console.WriteLine ("\n--- var1: " + var.Name + " alive:" + alive + " " + var.IsAlive (args.Frame.TargetAddress));
							if (alive)
								Console.WriteLine (" = '" + Util.ObjectToString (args.Frame, var.GetObject (args.Frame)) + "'");
//								Util.PrintObject (args.Frame, var.GetObject (args.Frame));
						}
					}
*/
				}
				
				NotifyTargetEvent (args);

			} catch (Exception e) {
				Console.WriteLine ("*** DS.OnTargetEvent, exception : {0}", e.ToString ());
			}
		}
		
		void NotifyTargetEvent (MD.TargetEventArgs args)
		{
			try {
				DL.TargetEventArgs dl_args = new DL.TargetEventArgs ((DL.TargetEventType)args.Type);

				//FIXME: using Backtrace.Mode.Default right now
				//FIXME: code from BacktraceCommand.DoExecute
				//FIXME: better way than args.Frame.Thread.* ?

				if (args.Type != MD.TargetEventType.TargetExited) {
					MD.Backtrace backtrace = args.Frame.Thread.CurrentBacktrace;
					if (backtrace == null)
						backtrace = args.Frame.Thread.GetBacktrace (MD.Backtrace.Mode.Default, max_frames);

					BacktraceWrapper wrapper = new BacktraceWrapper (backtrace);
					dl_args.Backtrace = new DL.Backtrace (wrapper);
				}

				controller.OnTargetEvent (dl_args);
			} catch (Exception e) {
				Console.WriteLine ("*** DS.OnTargetEvent, exception : {0}", e.ToString ());
			}
		}

		private void OnProcessCreatedEvent (MD.Debugger debugger, MD.Process process)
		{
			Console.WriteLine ("*** DS.OnProcessCreatedEvent");
			controller.OnProcessCreated (process.ID);
		}
		
		private void OnProcessExitedEvent (MD.Debugger debugger, MD.Process process)
		{
			Console.WriteLine ("*** DS.OnProcessExitedEvent");
			controller.OnProcessExited (process.ID);
		}
		
		private void OnProcessExecdEvent (MD.Debugger debugger, MD.Process process)
		{
			Console.WriteLine ("*** DS.OnProcessExecdEvent");
			controller.OnProcessExecd (process.ID);
		}
		
		private void OnThreadCreatedEvent (MD.Debugger debugger, MD.Thread process)
		{
			Console.WriteLine ("*** DS.OnThreadCreatedEvent");
			controller.OnThreadCreated (process.ID);
		}
		
		private void OnThreadExitedEvent (MD.Debugger debugger, MD.Thread process)
		{
			Console.WriteLine ("*** DS.OnThreadExitedEvent");
			controller.OnThreadExited (process.ID);
		}

		private void OnTargetExitedEvent (MD.Debugger debugger)
		{
			Console.WriteLine ("*** DS.OnTargetExitedEvent");
		}

	}
}
