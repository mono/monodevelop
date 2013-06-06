using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Win32
{
	class MtaRawValueString : IRawValueString
	{
		IRawValueString source;

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
