using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using VsFormat = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrameFormat;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	class VSCodeDebuggerBacktrace : IBacktrace
	{
		readonly int threadId;
		VSCodeDebuggerSession vsCodeDebuggerSession;
		int totalFramesCount;
		Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame [] frames;
		VsFormat frame0Format;

		public VSCodeDebuggerBacktrace (VSCodeDebuggerSession vsCodeDebuggerSession, int threadId)
		{
			this.threadId = threadId;
			this.vsCodeDebuggerSession = vsCodeDebuggerSession;
			frame0Format = VsCodeStackFrame.GetStackFrameFormat (vsCodeDebuggerSession.EvaluationOptions);
			var body = vsCodeDebuggerSession.protocolClient.SendRequestSync (new StackTraceRequest (threadId, 0, 1, frame0Format));
			totalFramesCount = body.TotalFrames ?? 0;
			frames = new Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame [totalFramesCount];
			if (totalFramesCount > 0 && body.StackFrames.Count > 0)
				frames [0] = body.StackFrames [0];
		}

		public int FrameCount {
			get {
				return totalFramesCount;
			}
		}

		public AssemblyLine [] Disassemble (int frameIndex, int firstLine, int count)
		{
			throw new NotImplementedException ();
		}

		public ObjectValue [] GetAllLocals (int frameIndex, EvaluationOptions options)
		{
			List<ObjectValue> results = new List<ObjectValue> ();
			var scopeBody = vsCodeDebuggerSession.protocolClient.SendRequestSync (new ScopesRequest (frames [frameIndex].Id));
			foreach (var variablesGroup in scopeBody.Scopes) {
				using (var timer = vsCodeDebuggerSession.EvaluationStats.StartTimer ()) {
					var varibles = vsCodeDebuggerSession.protocolClient.SendRequestSync (new VariablesRequest (variablesGroup.VariablesReference));
					foreach (var variable in varibles.Variables) {
						results.Add (VsCodeVariableToObjectValue (vsCodeDebuggerSession, variable.Name, variable.EvaluateName, variable.Type, variable.Value, variable.VariablesReference, variablesGroup.VariablesReference, frames [frameIndex].Id));
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

		public ObjectValue [] GetExpressionValues (int frameIndex, string [] expressions, EvaluationOptions options)
		{
			var results = new List<ObjectValue> ();
			foreach (var expr in expressions) {
				using (var timer = vsCodeDebuggerSession.EvaluationStats.StartTimer ()) {
					var responseBody = vsCodeDebuggerSession.protocolClient.SendRequestSync (new EvaluateRequest (
						expr,
						frames [frameIndex].Id));
					results.Add (VsCodeVariableToObjectValue (vsCodeDebuggerSession, expr, expr, responseBody.Type, responseBody.Result, responseBody.VariablesReference, 0, frames [frameIndex].Id));
					timer.Success = true;
				}
			}
			return results.ToArray ();
		}

		internal static ObjectValue VsCodeVariableToObjectValue (VSCodeDebuggerSession vsCodeDebuggerSession, string name, string evalName, string type, string value, int variablesReference, int parentVariablesReference, int frameId)
		{
			return new VSCodeObjectSource (vsCodeDebuggerSession, variablesReference, parentVariablesReference, name, type, evalName, frameId, value).GetValue (default (ObjectPath), null);
		}

		public ObjectValue [] GetLocalVariables (int frameIndex, EvaluationOptions options)
		{
			List<ObjectValue> results = new List<ObjectValue> ();
			var scopeBody = vsCodeDebuggerSession.protocolClient.SendRequestSync (new ScopesRequest (frames [frameIndex].Id));
			foreach (var variablesGroup in scopeBody.Scopes) {
				using (var timer = vsCodeDebuggerSession.EvaluationStats.StartTimer ()) {
					var varibles = vsCodeDebuggerSession.protocolClient.SendRequestSync (new VariablesRequest (variablesGroup.VariablesReference));
					foreach (var variable in varibles.Variables) {
						results.Add (ObjectValue.CreatePrimitive (null, new ObjectPath (variable.Name), variable.Type ?? "<unknown>", new EvaluationResult (variable.Value), ObjectValueFlags.None));
					}
					timer.Success = true;
				}
			}
			return results.ToArray ();
		}

		public ObjectValue [] GetParameters (int frameIndex, EvaluationOptions options)
		{
			return new ObjectValue [0];//TODO: Find out how to seperate Params from other Locals
		}

		public Mono.Debugging.Client.StackFrame [] GetStackFrames (int firstIndex, int lastIndex)
		{
			//Optimisation for getting 1st frame of thread(used for ThreadPad)
			if (firstIndex == 0 && lastIndex == 1 && totalFramesCount > 0) {
				return new Mono.Debugging.Client.StackFrame [] { new VsCodeStackFrame (frame0Format, threadId, 0, frames [0]) };
			}
			var stackFrames = new Mono.Debugging.Client.StackFrame [Math.Min (lastIndex - firstIndex, totalFramesCount - firstIndex)];
			var format = VsCodeStackFrame.GetStackFrameFormat (vsCodeDebuggerSession.EvaluationOptions);
			var body = vsCodeDebuggerSession.protocolClient.SendRequestSync (new StackTraceRequest (threadId, firstIndex, stackFrames.Length, format));
			for (int i = 0; i < stackFrames.Length; i++) {
				frames [i + firstIndex] = body.StackFrames [i];
				stackFrames [i] = new VsCodeStackFrame (format, threadId, i, body.StackFrames [i]);
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
