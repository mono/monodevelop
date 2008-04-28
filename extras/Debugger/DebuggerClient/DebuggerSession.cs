using System;
using DebuggerLibrary;
using System.Diagnostics;

namespace DebuggerClient
{
	public class DebuggerSession
	{
		private DebuggerController controller;
		private Backtrace current_backtrace;

		private int main_process_id;

		public DebuggerSession (IDebuggerController controller)
		{
			this.controller = (DebuggerController) controller;
			this.controller.MainProcessCreatedEvent += OnMainProcessCreated;
		}

		public void NextLine ()
		{
			controller.DebuggerServer.NextLine ();
		}

		public void StepLine ()
		{
			controller.DebuggerServer.StepLine ();
		}

		public void Finish ()
		{
			controller.DebuggerServer.Finish ();
		}

		public int InsertBreakpoint (string filename, int line, bool activate)
		{
			return controller.DebuggerServer.InsertBreakpoint (filename, line, activate);
		}

		public void RemoveBreakpoint (int index)
		{
			controller.DebuggerServer.RemoveBreakpoint (index);
		}

		public void Continue ()
		{
			controller.DebuggerServer.Continue ();
		}

		public void Stop ()
		{
			controller.DebuggerServer.Stop ();
		}

		//breakpoints
		//stop
		//query methods
		//events.. 

		public event TargetEventHandler TargetEvent;
		public event ProcessEventHandler MainProcessCreatedEvent;

		void OnMainProcessCreated (int process_id)
		{
			main_process_id = process_id;
			controller.TargetEvent += OnTargetEvent;
		}

		void OnTargetEvent (TargetEventArgs args)
		{
			current_backtrace = args.Backtrace;
			if (TargetEvent != null)
				TargetEvent (args);
		}
	}
}
