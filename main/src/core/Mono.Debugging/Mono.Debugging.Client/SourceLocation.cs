using System;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class SourceLocation
	{
		public readonly string Method;
		public readonly string Filename;
		public readonly int Line = -1;
		public readonly int Column = -1;

		public SourceLocation (string method, string filename, int line): this (method, filename, line, -1)
		{
		}

		public SourceLocation (string method, string filename, int line, int column)
		{
			this.Method = method;
			this.Filename = filename;
			this.Line = line;
			this.Column = column;
		}
		
		public override string ToString ()
		{
			return string.Format("[SourceLocation Method={0}, Filename={1}, Line={2}, Column={3}]", Method, Filename, Line, Column);
		}

	}
}
