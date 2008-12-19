
using Mono.Debugging.Client;

namespace Mono.Debugging.Backend
{
	public interface IBacktrace
	{
		int FrameCount { get; }
		StackFrame[] GetStackFrames (int firstIndex, int lastIndex);
		ObjectValue[] GetLocalVariables (int frameIndex, int timeout);
		ObjectValue[] GetParameters (int frameIndex, int timeout);
		ObjectValue GetThisReference (int frameIndex, int timeout);
		ObjectValue[] GetAllLocals (int frameIndex, int timeout);
		ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, bool evaluateMethods, int timeout);
		CompletionData GetExpressionCompletionData (int frameIndex, string exp);
		AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count);
	}
}
