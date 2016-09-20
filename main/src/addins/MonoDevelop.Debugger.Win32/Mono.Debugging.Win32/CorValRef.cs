using Microsoft.Samples.Debugging.CorDebug;

namespace MonoDevelop.Debugger.Win32
{
	public class CorValRef
	{
		CorValue val;
		readonly ValueLoader loader;
		int version;

		public delegate CorValue ValueLoader ( );

		public CorValRef (CorValue val)
		{
			this.val = val;
			this.version = CorDebuggerSession.EvaluationTimestamp;
		}

		public CorValRef (CorValue val, ValueLoader loader)
		{
			this.val = val;
			this.loader = loader;
			this.version = CorDebuggerSession.EvaluationTimestamp;
		}

		public CorValRef (ValueLoader loader)
		{
			this.val = loader ();
			this.loader = loader;
			this.version = CorDebuggerSession.EvaluationTimestamp;
		}

		public bool IsValid {
			get { return version == CorDebuggerSession.EvaluationTimestamp; }
			set {
				if (value)
					version = CorDebuggerSession.EvaluationTimestamp;
				else
					version = -1;
			}
		}

		public void Reload ()
		{
			if (loader != null) {
				// Obsolete value, get a new one
				CorValue v = loader ();
				version = CorDebuggerSession.EvaluationTimestamp;
				if (v != null)
					val = v;
			}
		}

		public CorValue Val {
			get {
				if (version < CorDebuggerSession.EvaluationTimestamp) {
					Reload ();
				}
				return val;
			}
		}
	}
}
