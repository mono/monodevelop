
using Mono.Debugging.Client;

namespace Mono.Debugging.Backend
{
	public interface IBacktrace
	{
		int FrameCount { get; }
		StackFrame[] GetStackFrames (int firstIndex, int lastIndex);
		ObjectValue[] GetLocalVariables (int frameIndex);
		ObjectValue[] GetParameters (int frameIndex);
		ObjectValue GetThisReference (int frameIndex);
		ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, bool evaluateMethods);
		AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count);
	}
}
