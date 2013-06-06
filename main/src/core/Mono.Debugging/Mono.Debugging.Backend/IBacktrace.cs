
using Mono.Debugging.Client;

namespace Mono.Debugging.Backend
{
	public interface IBacktrace: IDebuggerBackendObject
	{
		int FrameCount { get; }
		StackFrame[] GetStackFrames (int firstIndex, int lastIndex);
		ObjectValue[] GetLocalVariables (int frameIndex, EvaluationOptions options);
		ObjectValue[] GetParameters (int frameIndex, EvaluationOptions options);
		ObjectValue GetThisReference (int frameIndex, EvaluationOptions options);
		ExceptionInfo GetException (int frameIndex, EvaluationOptions options);
		ObjectValue[] GetAllLocals (int frameIndex, EvaluationOptions options);
		ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, EvaluationOptions options);
		CompletionData GetExpressionCompletionData (int frameIndex, string exp);
		AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count);
		ValidationResult ValidateExpression (int frameIndex, string expression, EvaluationOptions options);
	}
}
