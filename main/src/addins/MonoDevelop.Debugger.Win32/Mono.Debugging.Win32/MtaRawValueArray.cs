using System;
using Mono.Debugging.Backend;

namespace MonoDevelop.Debugger.Win32
{
	class MtaRawValueArray : IRawValueArray
	{
		readonly IRawValueArray source;

		public MtaRawValueArray (IRawValueArray s)
		{
			source = s;
		}

		public int[] Dimensions
		{
			get { return MtaThread.Run (() => source.Dimensions); }
		}

		public Array GetValues (int[] index, int count)
		{
			return MtaThread.Run (() => source.GetValues (index, count));
		}

		public object GetValue (int[] index)
		{
			return MtaThread.Run (() => source.GetValue (index));
		}

		public void SetValue (int[] index, object value)
		{
			MtaThread.Run (() => source.SetValue (index, value));
		}

		public Array ToArray ()
		{
			return MtaThread.Run (() => source.ToArray ());
		}
	}
}
