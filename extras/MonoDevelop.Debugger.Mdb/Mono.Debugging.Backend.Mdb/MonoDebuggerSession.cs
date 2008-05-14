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
		
		protected override IDebuggerSessionBackend CreateBackend ()
		{
			return controller.DebuggerServer;
		}
	}
}
