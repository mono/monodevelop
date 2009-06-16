using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Debugging.Evaluation;
using Microsoft.Samples.Debugging.CorDebug;

namespace MonoDevelop.Debugger.Win32
{
	public class CorEvaluationContext: EvaluationContext<CorValRef, CorType>
	{
		CorEval corEval;
		CorFrame frame;
		int frameIndex;
		int evalTimestamp;
		CorBacktrace backtrace;
		CorThread thread;
		int threadId;

		public CorDebuggerSession Session { get; set; }

		internal CorEvaluationContext (CorDebuggerSession session, CorBacktrace backtrace, CorFrame frame, int index)
		{
			Session = session;
			Evaluator = session.Evaluator;
			Adapter = session.ObjectAdapter;
			this.frame = frame;
			frameIndex = index;
			this.backtrace = backtrace;
			evalTimestamp = CorDebuggerSession.EvaluationTimestamp;
		}

		void CheckTimestamp ( )
		{
			if (evalTimestamp != CorDebuggerSession.EvaluationTimestamp) {
				thread = null;
				frame = null;
				corEval = null;
			}
		}

		public CorThread Thread {
			get {
				CheckTimestamp ();
				if (thread == null)
					thread = Session.GetThread (threadId);
				return thread;
			}
			set {
				thread = value;
				threadId = thread.Id;
			}
		}

		public CorFrame Frame {
			get {
				CheckTimestamp ();
				if (frame == null) {
					frame = null;
					int n = 0;
					foreach (CorFrame f in backtrace.FrameList) {
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
				CheckTimestamp ();
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
