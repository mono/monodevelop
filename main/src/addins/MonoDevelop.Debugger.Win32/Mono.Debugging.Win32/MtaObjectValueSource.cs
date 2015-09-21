using Mono.Debugging.Backend;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Win32
{
	class MtaObjectValueSource: IObjectValueSource
	{
		readonly IObjectValueSource source;

		public MtaObjectValueSource (IObjectValueSource s)
		{
			source = s;
		}

		public ObjectValue[] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
		{
			return MtaThread.Run (() => source.GetChildren (path, index, count, options));
		}

		public object GetRawValue (ObjectPath path, EvaluationOptions options)
		{
			return MtaThread.Run (() => source.GetRawValue (path, options));
		}

		public ObjectValue GetValue (ObjectPath path, EvaluationOptions options)
		{
			return MtaThread.Run (() => source.GetValue (path, options));
		}

		public void SetRawValue (ObjectPath path, object value, EvaluationOptions options)
		{
			MtaThread.Run (() => source.SetRawValue (path, value, options));
		}

		public EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			return MtaThread.Run (() => source.SetValue (path, value, options));
		}
	}
}
