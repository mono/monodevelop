namespace DebuggerLibrary
{
	public interface IBacktrace
	{
		int FrameCount { get; }
		StackFrame[] GetStackFrames(int firstIndex, int lastIndex);
	}
}
