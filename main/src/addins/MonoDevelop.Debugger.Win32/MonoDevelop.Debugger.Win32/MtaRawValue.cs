using Mono.Debugging.Backend;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Win32
{
	class MtaRawValue : IRawValue
	{
		readonly IRawValue source;

		public MtaRawValue (IRawValue s)
		{
			source = s;
		}

		public object CallMethod (string name, object[] parameters, EvaluationOptions options)
		{
			return MtaThread.Run (() => source.CallMethod (name, parameters, options));
		}

		public object GetMemberValue (string name, EvaluationOptions options)
		{
			return MtaThread.Run (() => source.GetMemberValue (name, options));
		}

		public void SetMemberValue (string name, object value, EvaluationOptions options)
		{
			MtaThread.Run (() => source.SetMemberValue (name, value, options));
		}
	}
}
