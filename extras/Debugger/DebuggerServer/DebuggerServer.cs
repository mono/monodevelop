using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Lifetime;
using System.Text;

using Mono.Debugger;
using MD = Mono.Debugger;

using DL = DebuggerLibrary;
using DebuggerLibrary;

namespace DebuggerServer
{
	class DebuggerServer : MarshalByRefObject, IDebugger, ISponsor
	{
		private IDebuggerController controller;
		private Debugger debugger;
		private MD.DebuggerSession session;
		private MD.Process process;
		private int max_frames;

		//[DllImport("monodebuggerserver")]
		//static extern int mono_debugger_server_static_init (); 
		//FIXME: see frontend/Main.cs:common_thread_main also

		public DebuggerServer (IDebuggerController dc)
		{
			this.controller = dc;
			MarshalByRefObject mbr = (MarshalByRefObject)controller;
			ILease lease = mbr.GetLifetimeService() as ILease;
			lease.Register(this);
			max_frames = 50;
		}

		#region IDebugger Members

		public void Run(string [] args)
		{
			try {
				if (args == null)
					throw new ArgumentNullException ("args");

				Console.WriteLine("Run ({0})", args.Length);

				Report.Initialize ();
				//mono_debugger_server_static_init ();
				//MD.Backend.Semaphore.Wait ();

				DebuggerConfiguration config = new DebuggerConfiguration ();
				config.LoadConfiguration ();
				debugger = new Debugger (config);
				debugger.MainProcessCreatedEvent += new ProcessEventHandler (OnInitialized);

				IExpressionParser parser = null;
				string name = "main";
				//DebuggerSession session = new DebuggerSession (config, name, parser);
				DebuggerOptions options = DebuggerOptions.ParseCommandLine (args);
				session = new MD.DebuggerSession (config, options, "main", null);
				debugger.Run(session);

			} catch (Exception e) {
				Console.WriteLine ("error: " + e.ToString ());
			}
		}

		public void Stop ()
		{
			debugger.Kill ();
		}

		public void NextLine ()
		{
			//FIXME: doesn't look like the proper way to do this.
			//can this be done for other threads/processes? how?

			//FIXME: if (process != null)
			process.MainThread.NextLine ();
		}

		public void StepLine ()
		{
			//FIXME: doesn't look like the proper way to do this.
			//can this be done for other threads/processes? how?

			//FIXME: if (process != null)
			try {
				process.MainThread.StepLine ();
			} catch (Exception e) {
				Console.WriteLine ("DS.StepLine: {0}", e.ToString ());
				throw;
			}
		}

		public void Finish ()
		{
			//FIXME: param @native ?
			process.MainThread.Finish (false);
		}

		public int InsertBreakpoint (string filename, int line, bool activate)
		{
			MD.SourceLocation location = FindFile (process.MainThread, filename, line);
			if (location == null)
				//FIXME: exception types
				throw new Exception ("invalid location");

			MD.Event ev = session.InsertBreakpoint (process.MainThread.ThreadGroup, location);
			if (activate)
				ev.Activate (process.MainThread);

			return ev.Index;
		}

		public void RemoveBreakpoint (int handle)
		{
			//FIXME: handle errors
			Event ev = session.GetEvent (handle);
			session.DeleteEvent (ev);
		}

		static MD.SourceLocation FindFile (Thread target, string filename, int line)
		{
			SourceFile file = target.Process.FindFile (filename);
			if (file == null)
				return null;
			//throw new ScriptingException ("Cannot find source file `{0}'.", filename);

			MethodSource source = file.FindMethod (line);
			if (source == null)
				return null;
			//throw new ScriptingException ("Cannot find method corresponding to line {0} in `{1}'.", line, file.Name);

			return new MD.SourceLocation (source, file, line);
		}

		public void Continue ()
		{
			process.MainThread.Continue ();
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

		private void OnInitialized (Debugger debugger, Process process)
		{
			this.process = process;
			this.debugger = debugger;
			controller.OnMainProcessCreated(process.ID);

			process.TargetOutputEvent += OnTargetOutput;
			debugger.TargetEvent += OnTargetEvent;
		}

		void OnTargetOutput (bool is_stderr, string line)
		{
			Console.WriteLine ("output: " + line);
		}

		private void OnTargetEvent (object sender, MD.TargetEventArgs args)
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

	}
}
