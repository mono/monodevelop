using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Win32
{
	class MtaRawValueArray : IRawValueArray
	{
		IRawValueArray source;

		public MtaRawValueArray (IRawValueArray s)
		{
			source = s;
		}

		public int[] Dimensions
		{
			get { return MtaThread.Run (() => source.Dimensions); }
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
