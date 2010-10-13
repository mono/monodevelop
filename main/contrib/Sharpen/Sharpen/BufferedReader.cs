namespace Sharpen
{
	using System;
	using System.IO;

	internal class BufferedReader : StreamReader
	{
		public BufferedReader (InputStreamReader r) : base(r.BaseStream)
		{
		}
	}
}
