using Mono.Debugging.Backend;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Win32
{
	class MtaBacktrace: IBacktrace
	{
		readonly IBacktrace backtrace;

		public MtaBacktrace (IBacktrace bt)
		{
			backtrace = bt;
		}

		public AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
			return MtaThread.Run (() => backtrace.Disassemble (frameIndex, firstLine, count));
		}

		public int FrameCount
		{
			get {
				return MtaThread.Run (() => backtrace.FrameCount);
			}
		}

		public ObjectValue[] GetAllLocals (int frameIndex, EvaluationOptions options)
		{
			return MtaThread.Run (() => backtrace.GetAllLocals (frameIndex, options));
		}

		public ExceptionInfo GetException (int frameIndex, EvaluationOptions options)
		{
			return MtaThread.Run (() => backtrace.GetException (frameIndex, options));
		}

		public CompletionData GetExpressionCompletionData (int frameIndex, string exp)
		{
			return MtaThread.Run (() => backtrace.GetExpressionCompletionData (frameIndex, exp));
		}

		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, EvaluationOptions options)
		{
			return MtaThread.Run (() => backtrace.GetExpressionValues (frameIndex, expressions, options));
		}

		public ObjectValue[] GetLocalVariables (int frameIndex, EvaluationOptions options)
		{
			return MtaThread.Run (() => backtrace.GetLocalVariables (frameIndex, options));
		}

		public ObjectValue[] GetParameters (int frameIndex, EvaluationOptions options)
		{
			return MtaThread.Run (() => backtrace.GetParameters (frameIndex, options));
		}

		public StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
			return MtaThread.Run (() => backtrace.GetStackFrames (firstIndex, lastIndex));
		}

		public ObjectValue GetThisReference (int frameIndex, EvaluationOptions options)
		{
			return MtaThread.Run (() => backtrace.GetThisReference (frameIndex, options));
		}

		public ValidationResult ValidateExpression (int frameIndex, string expression, EvaluationOptions options)
		{
			return MtaThread.Run (() => backtrace.ValidateExpression (frameIndex, expression, options));
		}
	}
}
