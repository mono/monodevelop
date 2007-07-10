
using System;
using System.IO;

namespace MonoDevelop.Projects.Text
{
	public class TextFileReader: TextReader
	{
		StringReader reader;
		string sourceEncoding;
		
		public TextFileReader (string fileName)
		{
			TextFile file = TextFile.ReadFile (fileName);
			reader = new StringReader (file.Text);
			sourceEncoding = file.SourceEncoding;
		}
		
		public override void Close ()
		{
			reader.Close ();
		}
		
		public override int Peek ()
		{
			return reader.Peek ();
		}
		
		public override int Read ()
		{
			return reader.Read ();
		}
		
		public override int Read (char[] buffer, int index, int len)
		{
			return reader.Read (buffer, index, len);
		}
		
		public override string ReadLine ()
		{
			return reader.ReadLine ();
		}
		
		public override string ReadToEnd ()
		{
			return reader.ReadToEnd ();
		}
		
		public string SourceEncoding {
			get { return sourceEncoding; }
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && reader != null)
				reader.Close ();

			reader = null;
			base.Dispose (disposing);
		}
	}
}
