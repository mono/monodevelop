using System;
using System.Collections.Generic;
using System.Text;
using Mono.Debugging.Client;

namespace Mono.Debugging.Backend
{
	public interface IDebuggerSessionBackend
	{
		void Run (DebuggerStartInfo startInfo);

		void Stop ();
		
		void Exit ();

		// Step one source line
		void StepLine ();

		// Step one source line, but step over method calls
		void NextLine ();

		// Continue until leaving the current method
		void Finish ();

		//breakpoints etc

		// returns a handle
		int InsertBreakpoint (string filename, int line, bool activate);

		void RemoveBreakpoint (int handle);
		
		void EnableBreakpoint (int handle, bool enable);

		void Continue ();
	}
}
