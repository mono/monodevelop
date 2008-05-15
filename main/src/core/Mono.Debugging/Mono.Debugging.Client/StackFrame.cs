using System;
using Mono.Debugging.Backend;
using System.Collections.Generic;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class StackFrame
	{
		long address;
		SourceLocation location;
		IBacktrace sourceBacktrace;
		int index;

		public StackFrame (long address, SourceLocation location)
		{
			this.address = address;
			this.location = location;
		}

		public StackFrame(long address, string module, string method, string filename, int line)
		{
			this.location = new SourceLocation(method, filename, line);
			this.address = address;
		}

		public SourceLocation SourceLocation
		{
			get { return location; }
		}

		public long Address
		{
			get { return address; }
		}

		internal IBacktrace SourceBacktrace {
			get { return sourceBacktrace; }
			set { sourceBacktrace = value; }
		}

		internal int Index {
			get { return index; }
			set { index = value; }
		}
		
		public ObjectValue[] GetLocalVariables ()
		{
			return sourceBacktrace.GetLocalVariables (index);
		}
		
		public ObjectValue[] GetParameters ()
		{
			return sourceBacktrace.GetParameters (index);
		}
		
		public ObjectValue GetThisReference ()
		{
			return sourceBacktrace.GetThisReference (index);
		}
		
		public ObjectValue[] GetExpressionValues (string[] expressions)
		{
			return sourceBacktrace.GetExpressionValues (index, expressions);
		}

		public override string ToString()
		{
			return String.Format("0x{0:X} in {1} at {2}:{3}", address, location.Method, location.Filename, location.Line);
		}
	}
}
