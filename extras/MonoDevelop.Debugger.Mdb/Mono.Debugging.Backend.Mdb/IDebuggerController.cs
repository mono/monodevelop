using Mono.Debugging.Client;

namespace Mono.Debugging.Backend.Mdb
{
	public interface IDebuggerController
	{
		void RegisterDebugger (IDebuggerServer debugger);
		void WaitForExit();

		//callbacks
		//FIXME: better naming for event callbacks
		void OnMainProcessCreated(int process_id);
		
		void OnTargetEvent (TargetEventArgs args);
		
		void OnTargetOutput (bool isStderr, string text);
		
		void OnDebuggerOutput (bool isStderr, string text);
		
		bool OnCustomBreakpointAction (string actionId, object handle);
		
		void UpdateBreakpoint (object handle, int count, string lastTrace);
		
		void NotifySourceFileLoaded (string[] fullFilePaths);
		
		void NotifySourceFileUnloaded (string[] fullFilePaths);
	}
}
