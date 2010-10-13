namespace Sharpen
{
	using System;
	using System.IO;

	internal class FileInputStream : InputStream
	{
		public FileInputStream (FilePath file) : this(file.GetPath ())
		{
		}

		public FileInputStream (string file)
		{
			if (!File.Exists (file)) {
				throw new FileNotFoundException ("File not found", file);
			}
			base.Wrapped = File.OpenRead (file);
		}

		public FileChannel GetChannel ()
		{
			return new FileChannel ((FileStream)base.Wrapped);
		}
	}
}
