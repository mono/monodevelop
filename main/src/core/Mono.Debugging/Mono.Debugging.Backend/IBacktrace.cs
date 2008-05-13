
using Mono.Debugging.Client;

namespace Mono.Debugging.Backend
{
	public interface IBacktrace
	{
		int FrameCount { get; }
		StackFrame[] GetStackFrames (int firstIndex, int lastIndex);
		ObjectValue[] GetLocalVariables (int frameIndex);
	}
}
