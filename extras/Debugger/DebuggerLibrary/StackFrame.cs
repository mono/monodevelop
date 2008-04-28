using System;

namespace DebuggerLibrary
{
	[Serializable]
	public class StackFrame
	{
		private long address;
		private SourceLocation location;

		public StackFrame(long address, SourceLocation location)
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

		public override string ToString()
		{
			return String.Format("0x{0:X} in {1} at {2}:{3}", address, location.Method, location.Filename, location.Line);
		}

	}
}
