namespace Sharpen
{
	using System;

	internal class FileReader : InputStreamReader
	{
		public FileReader (FilePath f) : base(f.GetPath ())
		{
		}
	}
}
