namespace Sharpen
{
	using System;
	using System.IO;

	internal class PrintWriter : StreamWriter
	{
		public PrintWriter (FilePath path) : base(path.GetPath ())
		{
		}

		public PrintWriter (StreamWriter other) : base(other.BaseStream)
		{
		}
	}
}
