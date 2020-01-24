using System;
using System.Linq;
using System.Collections.Generic;

using Mono.Debugging.Client;
using Mono.Debugging.Backend;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

using MonoDevelop.Core;

using StackFrame = Mono.Debugging.Client.StackFrame;
using VsStackFrame = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame;
using VsFrameFormat = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrameFormat;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	class VSCodeDebuggerBacktrace : IBacktrace
	{
		readonly VSCodeDebuggerSession session;
		readonly VsStackFrame[] frames;
		readonly List<Scope>[] scopes;
		readonly VsFrameFormat format;
		readonly int threadId;

		public VSCodeDebuggerBacktrace (VSCodeDebuggerSession session, int threadId)
		{
			this.session = session;
			this.threadId = threadId;

			format = VsCodeStackFrame.GetStackFrameFormat (session.EvaluationOptions);

			var response = session.protocolClient.SendRequestSync (new StackTraceRequest (threadId) { StartFrame = 0, Levels = 1, Format = format });

			FrameCount = response.TotalFrames ?? 0;
			frames = new VsStackFrame[FrameCount];
			scopes = new List<Scope>[FrameCount];
			if (FrameCount > 0 && response.StackFrames.Count > 0)
				frames[0] = response.StackFrames[0];
		}

		public int FrameCount {
			get; private set;
		}

		public AssemblyLine [] Disassemble (int frameIndex, int firstLine, int count)
		{
			throw new NotImplementedException ();
		}

		List<Scope> GetScopes (int frameIndex)
		{
			if (scopes[frameIndex] == null) {
				var response = session.protocolClient.SendRequestSync (new ScopesRequest (frames[frameIndex].Id));
				scopes[frameIndex] = response.Scopes;
			}

			return scopes[frameIndex];
		}

		public ObjectValue [] GetAllLocals (int frameIndex, EvaluationOptions options)
		{
			var results = new List<ObjectValue> ();
			var frame = frames[frameIndex];

			foreach (var scope in GetScopes (frameIndex)) {
				using (var timer = session.EvaluationStats.StartTimer ()) {
					VariablesResponse response;

					try {
						response = session.protocolClient.SendRequestSync (new VariablesRequest (scope.VariablesReference));
					} catch (Exception ex) {
						LoggingService.LogError ($"[VsCodeDebugger] Failed to get local variables for the scope: {scope.Name}", ex);
						timer.Success = false;
						continue;
					}
					
					foreach (var variable in response.Variables) {
						var source = new VSCodeVariableSource (session, variable, scope.VariablesReference, frame.Id);
						results.Add (source.GetValue (default (ObjectPath), null));
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
			var frame = frames[frameIndex];

			foreach (var expr in expressions) {
				using (var timer = session.EvaluationStats.StartTimer ()) {
					var response = session.protocolClient.SendRequestSync (new EvaluateRequest (expr) { FrameId = frame.Id });
					var source = new VSCodeEvaluationSource (session, expr, response, frame.Id);

					results.Add (source.GetValue (default (ObjectPath), null));
					timer.Success = true;
				}
			}

			return results.ToArray ();
		}

		ObjectValue[] GetVariables (int frameIndex, string scopeName)
		{
			var results = new List<ObjectValue> ();
			var frame = frames[frameIndex];

			foreach (var scope in GetScopes (frameIndex)) {
				if (!scope.Name.Equals (scopeName, StringComparison.Ordinal))
					continue;

				using (var timer = session.EvaluationStats.StartTimer ()) {
					VariablesResponse response;

					try {
						response = session.protocolClient.SendRequestSync (new VariablesRequest (scope.VariablesReference));
					} catch (Exception ex) {
						LoggingService.LogError ($"[VsCodeDebugger] Failed to get local variables for the scope: {scope.Name}", ex);
						timer.Success = false;
						continue;
					}

					foreach (var variable in response.Variables) {
						var source = new VSCodeVariableSource (session, variable, scope.VariablesReference, frame.Id);
						results.Add (source.GetValue (default (ObjectPath), null));
					}

					timer.Success = true;
				}
			}

			return results.ToArray ();
		}

		public ObjectValue [] GetLocalVariables (int frameIndex, EvaluationOptions options)
		{
			return GetVariables (frameIndex, "Locals");
		}

		public ObjectValue [] GetParameters (int frameIndex, EvaluationOptions options)
		{
			return GetVariables (frameIndex, "Arguments");
		}

		public StackFrame [] GetStackFrames (int firstIndex, int lastIndex)
		{
			//Optimisation for getting 1st frame of thread(used for ThreadPad)
			if (firstIndex == 0 && lastIndex == 1 && FrameCount > 0)
				return new StackFrame[] { new VsCodeStackFrame (this.format, threadId, 0, frames[0]) };

			var stackFrames = new StackFrame [Math.Min (lastIndex - firstIndex, FrameCount - firstIndex)];
			var format = VsCodeStackFrame.GetStackFrameFormat (session.EvaluationOptions);
			var body = session.protocolClient.SendRequestSync (new StackTraceRequest (threadId) { StartFrame = firstIndex, Levels = stackFrames.Length, Format = format });
			for (int i = 0; i < stackFrames.Length; i++) {
				frames[i + firstIndex] = body.StackFrames [i];
				stackFrames[i] = new VsCodeStackFrame (format, threadId, i, body.StackFrames [i]);
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
