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
		
		void OnProcessCreated (int process_id);
		void OnProcessExited (int process_id);
		void OnProcessExecd (int process_id);
		
		void OnThreadCreated (int thread_id);
		void OnThreadExited (int thread_id);
		
		void OnTargetEvent (TargetEventArgs args);
		
		void OnTargetOutput (bool isStderr, string text);
	}
}
