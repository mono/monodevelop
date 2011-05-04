using System;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class SourceLocation
	{
		public string MethodName { get; private set; }
		public string FileName { get; private set; }
		public int Line { get; private set; }
		public int Column { get; private set; }

		public SourceLocation (string methodName, string fileName, int line)
			: this (methodName, fileName, line, -1)
		{
		}

		public SourceLocation (string methodName, string fileName, int line, int column)
		{
			this.MethodName = methodName;
			this.FileName = fileName;
			this.Line = line;
			this.Column = column;
		}
		
		public override string ToString ()
		{
			return string.Format("[SourceLocation Method={0}, Filename={1}, Line={2}, Column={3}]", MethodName, FileName, Line, Column);
		}

	}
}
