using Mono.Debugging.Backend;

namespace MonoDevelop.Debugger.Win32
{
	class MtaRawValueString : IRawValueString
	{
		readonly IRawValueString source;

		public MtaRawValueString (IRawValueString s)
		{
			source = s;
		}

		public int Length
		{
			get { return MtaThread.Run (() => source.Length); }
		}

		public string Substring (int index, int length)
		{
			return MtaThread.Run (() => source.Substring (index, length));
		}

		public string Value
		{
			get { return MtaThread.Run (() => source.Value); }
		}
	}
}
