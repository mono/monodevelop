using System;
using System.Diagnostics;
using Mono.Debugging.Client;

namespace Mono.Debugging.Backend.Mdb
{
	public class MonoDebuggerSession: DebuggerSession
	{
		DebuggerController controller;
		
		public void StartDebugger ()
		{
			controller = new DebuggerController (Frontend);
			controller.StartDebugger ();
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			controller.StopDebugger ();
		}
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			controller.DebuggerServer.Run (startInfo);
		}

		protected override void OnStop ()
		{
			controller.DebuggerServer.Stop ();
		}
		
		protected override void OnExit ()
		{
			controller.Exit ();
		}

		protected override void OnStepLine ()
		{
			controller.DebuggerServer.StepLine ();
		}

		protected override void OnNextLine ()
		{
			controller.DebuggerServer.NextLine ();
		}

		protected override void OnFinish ()
		{
			controller.DebuggerServer.Finish ();
		}

		//breakpoints etc

		// returns a handle
		protected override int OnInsertBreakpoint (string filename, int line, bool activate)
		{
			return controller.DebuggerServer.InsertBreakpoint (filename, line, activate);
		}

		protected override void OnRemoveBreakpoint (int handle)
		{
			controller.DebuggerServer.RemoveBreakpoint (handle);
		}
		
		protected override void OnEnableBreakpoint (int handle, bool enable)
		{
			controller.DebuggerServer.EnableBreakpoint (handle, enable);
		}

		protected override void OnContinue ()
		{
			controller.DebuggerServer.Continue ();
		}
	}
}
