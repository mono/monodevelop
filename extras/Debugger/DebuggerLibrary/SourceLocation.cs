using System;

namespace DebuggerLibrary
{
	[Serializable]
	public class SourceLocation
	{
		public readonly string Method;
		public readonly string Filename;
		public readonly int Line = -1;

		public SourceLocation(string method, string filename, int line)
		{
			this.Method = method;
			this.Filename = filename;
			this.Line = line;
		}
	}
}
