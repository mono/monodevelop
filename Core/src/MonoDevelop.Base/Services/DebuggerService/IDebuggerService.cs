// IDebuggingService - Interface for the debugger to remove the depend on the
//                     debugger.
//
// Author: Todd Berman <tberman@sevenl.net>
//
// (C) 2004 Todd Berman

using System;

namespace MonoDevelop.Services
{

	public interface IDebuggableEditor {
		void ExecutingAt (int lineNumber);
		void ClearExecutingAt (int lineNumber);
	}

	public interface IDebuggingService {
		bool IsRunning { get; }
		bool IsDebugging { get; }
		bool AddBreakpoint (string filename, int linenum);
		void RemoveBreakpoint (string filename, int linenum);
		bool ToggleBreakpoint (string filename, int linenum);
		
		event EventHandler PausedEvent;
		event EventHandler ResumedEvent;
		event EventHandler StartedEvent;
		event EventHandler StoppedEvent;
		
		event BreakpointEventHandler BreakpointAdded;
		event BreakpointEventHandler BreakpointRemoved;
		event BreakpointEventHandler BreakpointChanged;
		event EventHandler ExecutionLocationChanged;

		void Pause ();
		void Resume ();
		void Run (IConsole console, string[] args);
		void Stop ();

		void StepInto ();
		void StepOver ();
		void StepOut ();

		string[] Backtrace { get; }

		string CurrentFilename { get; }
		int CurrentLineNumber { get; }

		string LookupValue (string expr);
		
		IBreakpoint[] Breakpoints { get; }
		IBreakpoint[] GetBreakpointsAtFile (string sourceFile);
		
		void ClearAllBreakpoints ();
		
		IExecutionHandlerFactory GetExecutionHandlerFactory ();
	}
}
