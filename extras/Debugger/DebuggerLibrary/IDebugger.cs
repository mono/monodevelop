using System;
using System.Collections.Generic;
using System.Text;

namespace DebuggerLibrary
{
	public interface IDebugger
	{
		void Run(string [] args);

		void Stop ();

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

		//FIXME: add method for adding/removing multiple breakpoints
		//FIXME: Enable/Disable

		void Continue ();
	}
}
