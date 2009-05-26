using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Samples.Debugging.CorDebug;
using MonoDevelop.Debugger.Evaluation;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Win32
{
	public class CorValRef
	{
		CorValue val;
		ValueLoader loader;
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
		}

		public CorValue Val {
			get {
				if (version < CorDebuggerSession.EvaluationTimestamp && loader != null) {
					// Obsolete value, get a new one
					version = CorDebuggerSession.EvaluationTimestamp;
					CorValue v = loader ();
					if (v != null)
						val = v;
				}
				return val;
			}
		}
	}
}
