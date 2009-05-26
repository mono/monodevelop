using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Debugger.Evaluation;
using Microsoft.Samples.Debugging.CorDebug;

namespace MonoDevelop.Debugger.Win32
{
	public class CorEvaluationContext: EvaluationContext<CorValRef, CorType>
	{
		CorEval corEval;
		CorFrame frame;
		int frameIndex;
		int evalTimestamp;

		public CorThread Thread { get; set; }
		public CorDebuggerSession Session { get; set; }

		public CorEvaluationContext (CorDebuggerSession session, CorFrame frame, int index)
		{
			Session = session;
			Evaluator = session.Evaluator;
			Adapter = session.ObjectAdapter;
			this.frame = frame;
			frameIndex = index;
			evalTimestamp = CorDebuggerSession.EvaluationTimestamp;
		}

		public CorFrame Frame {
			get {
				if (frame == null || evalTimestamp != CorDebuggerSession.EvaluationTimestamp) {
					frame = null;
					int n = 0;
					foreach (CorFrame f in CorBacktrace.GetFrames (Thread)) {
						if (n++ == frameIndex) {
							frame = f;
							break;
						}
					}
				}
				return frame;
			}
		}

		public CorEval Eval {
			get {
				if (corEval == null)
					corEval = Thread.CreateEval ();
				return corEval;
			}
		}

		public override void CopyFrom (EvaluationContext<CorValRef, CorType> ctx)
		{
			base.CopyFrom (ctx);
			frame = ((CorEvaluationContext) ctx).frame;
			frameIndex = ((CorEvaluationContext) ctx).frameIndex;
			evalTimestamp = ((CorEvaluationContext) ctx).evalTimestamp;
			Thread = ((CorEvaluationContext) ctx).Thread;
			Session = ((CorEvaluationContext) ctx).Session;
		}

		public override void WriteDebuggerError (Exception ex)
		{
			Session.Frontend.NotifyDebuggerOutput (true, ex.Message);
		}

		public override void WriteDebuggerOutput (string message, params object[] values)
		{
			Session.Frontend.NotifyDebuggerOutput (false, string.Format (message, values));
		}

		public CorValue RuntimeInvoke (CorFunction function, CorType[] typeArgs, CorValue thisObj, CorValue[] arguments)
		{
			return Session.RuntimeInvoke (this, function, typeArgs, thisObj, arguments);
		}
	}
}
