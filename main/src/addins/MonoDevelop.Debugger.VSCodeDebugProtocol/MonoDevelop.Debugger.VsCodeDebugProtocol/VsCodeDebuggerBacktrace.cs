using System;
using System.Linq;
using System.Collections.Generic;

using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

using VsStackFrameFormat = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrameFormat;
using VsStackFrame = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame;
using StackFrame = Mono.Debugging.Client.StackFrame;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	class VSCodeDebuggerBacktrace : IBacktrace
	{
		readonly VsCodeDebuggerSession session;
		readonly VsStackFrameFormat frame0Format;
		readonly VsStackFrame[] frames;
		readonly int totalFramesCount;
		readonly int threadId;

		public VSCodeDebuggerBacktrace (VsCodeDebuggerSession session, int threadId)
		{
			this.threadId = threadId;
			this.session = session;

			frame0Format = VsCodeDebuggerStackFrame.GetStackFrameFormat (session.EvaluationOptions);
			var body = session.protocolClient.SendRequestSync (new StackTraceRequest (threadId) { StartFrame = 0, Levels = 1, Format = frame0Format });
			totalFramesCount = body.TotalFrames ?? 0;
			frames = new VsStackFrame [totalFramesCount];
			if (totalFramesCount > 0 && body.StackFrames.Count > 0)
				frames[0] = body.StackFrames[0];
		}

		public int FrameCount {
			get {
				return totalFramesCount;
			}
		}

		public AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
			throw new NotImplementedException ();
		}

		public ObjectValue[] GetAllLocals (int frameIndex, EvaluationOptions options)
		{
			var results = new List<ObjectValue> ();
			var scopeBody = session.protocolClient.SendRequestSync (new ScopesRequest (frames[frameIndex].Id));
			foreach (var variablesGroup in scopeBody.Scopes) {
				using (var timer = session.EvaluationStats.StartTimer ()) {
					var varibles = session.protocolClient.SendRequestSync (new VariablesRequest (variablesGroup.VariablesReference));
					foreach (var variable in varibles.Variables) {
						results.Add (VsCodeVariableToObjectValue (session, variable.Name, variable.EvaluateName, variable.Type, variable.Value, variable.VariablesReference, variablesGroup.VariablesReference, frames[frameIndex].Id));
					}
					timer.Success = true;
				}
			}
			return results.ToArray ();
		}

		public ExceptionInfo GetException (int frameIndex, EvaluationOptions options)
		{
			return new ExceptionInfo (GetAllLocals (frameIndex, options).FirstOrDefault (o => o.Name == "$exception"));
		}

		public CompletionData GetExpressionCompletionData (int frameIndex, string exp)
		{
			return new CompletionData ();
		}

		public EvaluationContext GetEvaluationContext (int frameIndex, EvaluationOptions options)
		{
			return new VsCodeDebuggerEvaluationContext (session, frames[frameIndex], threadId, options);
		}

		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, EvaluationOptions options)
		{
			var results = new List<ObjectValue> ();

			foreach (var expr in expressions) {
				using (var timer = session.EvaluationStats.StartTimer ()) {
					var responseBody = session.protocolClient.SendRequestSync (new EvaluateRequest (expr) { FrameId = frames[frameIndex].Id });
					results.Add (VsCodeVariableToObjectValue (session, expr, expr, responseBody.Type, responseBody.Result, responseBody.VariablesReference, 0, frames[frameIndex].Id));
					timer.Success = true;
				}
			}

			return results.ToArray ();
		}

		internal static ObjectValue VsCodeVariableToObjectValue (VsCodeDebuggerSession session, string name, string evalName, string type, string value, int variablesReference, int parentVariablesReference, int frameId)
		{
			return new VSCodeObjectSource (session, variablesReference, parentVariablesReference, name, type, evalName, frameId, value).GetValue (default (ObjectPath), null);
		}

		public ObjectValue[] GetLocalVariables (int frameIndex, EvaluationOptions options)
		{
			var results = new List<ObjectValue> ();
			var scopeBody = session.protocolClient.SendRequestSync (new ScopesRequest (frames[frameIndex].Id));
			foreach (var variablesGroup in scopeBody.Scopes) {
				using (var timer = session.EvaluationStats.StartTimer ()) {
					var variables = session.protocolClient.SendRequestSync (new VariablesRequest (variablesGroup.VariablesReference));
					foreach (var variable in variables.Variables) {
						results.Add (ObjectValue.CreatePrimitive (null, new ObjectPath (variable.Name), variable.Type ?? "<unknown>", new EvaluationResult (variable.Value), ObjectValueFlags.None));
					}
					timer.Success = true;
				}
			}
			return results.ToArray ();
		}

		public ObjectValue[] GetParameters (int frameIndex, EvaluationOptions options)
		{
			return new ObjectValue [0];//TODO: Find out how to seperate Params from other Locals
		}

		public StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
			//Optimisation for getting 1st frame of thread(used for ThreadPad)
			if (firstIndex == 0 && lastIndex == 1 && totalFramesCount > 0) {
				return new StackFrame[] { new VsCodeDebuggerStackFrame (frame0Format, threadId, 0, frames[0]) };
			}
			var stackFrames = new StackFrame [Math.Min (lastIndex - firstIndex, totalFramesCount - firstIndex)];
			var format = VsCodeDebuggerStackFrame.GetStackFrameFormat (session.EvaluationOptions);
			var body = session.protocolClient.SendRequestSync (new StackTraceRequest (threadId) { StartFrame = firstIndex, Levels = stackFrames.Length, Format = format });
			for (int i = 0; i < stackFrames.Length; i++) {
				frames[i + firstIndex] = body.StackFrames[i];
				stackFrames[i] = new VsCodeDebuggerStackFrame (format, threadId, i, body.StackFrames[i]);
			}
			return stackFrames;
		}

		public ObjectValue GetThisReference (int frameIndex, EvaluationOptions options)
		{
			return GetAllLocals (frameIndex, options).FirstOrDefault (l => l.Name == "this");
		}

		public ValidationResult ValidateExpression (int frameIndex, string expression, EvaluationOptions options)
		{
			return new ValidationResult (true, null);
		}
	}
}
