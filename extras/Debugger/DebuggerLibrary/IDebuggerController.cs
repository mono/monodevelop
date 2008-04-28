namespace DebuggerLibrary
{
	public interface IDebuggerController
	{
		void RegisterDebugger(IDebugger debugger);
		void WaitForExit();
		//callbacks

		void OnMainProcessCreated(int process_id);

		//void OnTargetOutput (bool is_stderr, string line);

		//FIXME: better naming for event callbacks
		void OnTargetEvent (TargetEventArgs args);
	}
}
