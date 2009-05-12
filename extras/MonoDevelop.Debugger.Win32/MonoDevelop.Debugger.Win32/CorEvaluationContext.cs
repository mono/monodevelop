using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Debugger.Evaluation;
using Microsoft.Samples.Debugging.CorDebug;

namespace MonoDevelop.Debugger.Win32
{
	class CorEvaluationContext: EvaluationContext<CorValue, CorType>
	{
		CorEval corEval;

		public CorFrame Frame { get; set; }
		public CorThread Thread { get; set; }
		public CorDebuggerSession Session { get; set; }

		public CorEvaluationContext (CorDebuggerSession session)
		{
			Session = session;
			Evaluator = session.Evaluator;
			Adapter = session.ObjectAdapter;
			AsyncEvaluationTracker = session.AsyncEvaluationTracker;
		}

		public CorEval Eval {
			get {
				if (corEval == null)
					corEval = Thread.CreateEval ();
				return corEval;
			}
		}

		public override void CopyFrom (EvaluationContext<CorValue, CorType> ctx)
		{
			base.CopyFrom (ctx);
			Frame = ((CorEvaluationContext) ctx).Frame;
		}

		public override void WriteDebuggerError (Exception ex)
		{
			Session.Frontend.NotifyDebuggerOutput (true, ex.Message);
		}

		public override void WriteDebuggerOutput (string message, params object[] values)
		{
			Session.Frontend.NotifyDebuggerOutput (false, string.Format (message, values));
		}

		public CorValue RuntimeInvoke (CorFunction function, CorValue thisObj, CorValue[] arguments)
		{
			return null;
		}
	}
}
