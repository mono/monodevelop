namespace Sharpen
{
	using System;
	using System.IO;
	using System.Text;

	internal class PrintWriter : TextWriter
	{
		TextWriter writer;
		
		public PrintWriter (FilePath path)
		{
			writer = new StreamWriter (path);
		}

		public PrintWriter (TextWriter other)
		{
			writer = other;
		}

		public override Encoding Encoding {
			get { return writer.Encoding; }
		}
		
		public override void Close ()
		{
			writer.Close ();
		}
	
		public override void Flush ()
		{
			writer.Flush ();
		}
	
		public override System.IFormatProvider FormatProvider {
			get {
				return writer.FormatProvider;
			}
		}
	
		public override string NewLine {
			get {
				return writer.NewLine;
			}
			set {
				writer.NewLine = value;
			}
		}
	
		public override void Write (char[] buffer, int index, int count)
		{
			writer.Write (buffer, index, count);
		}
	
		public override void Write (char[] buffer)
		{
			writer.Write (buffer);
		}
	
		public override void Write (string format, object arg0, object arg1, object arg2)
		{
			writer.Write (format, arg0, arg1, arg2);
		}
	
		public override void Write (string format, object arg0, object arg1)
		{
			writer.Write (format, arg0, arg1);
		}
	
		public override void Write (string format, object arg0)
		{
			writer.Write (format, arg0);
		}
	
		public override void Write (string format, params object[] arg)
		{
			writer.Write (format, arg);
		}
	
		public override void WriteLine (char[] buffer, int index, int count)
		{
			writer.WriteLine (buffer, index, count);
		}
	
		public override void WriteLine (char[] buffer)
		{
			writer.WriteLine (buffer);
		}
	
		public override void WriteLine (string format, object arg0, object arg1, object arg2)
		{
			writer.WriteLine (format, arg0, arg1, arg2);
		}
	
		public override void WriteLine (string format, object arg0, object arg1)
		{
			writer.WriteLine (format, arg0, arg1);
		}
	
		public override void WriteLine (string format, object arg0)
		{
			writer.WriteLine (format, arg0);
		}
	
		public override void WriteLine (string format, params object[] arg)
		{
			writer.WriteLine (format, arg);
		}
	
		public override void WriteLine (ulong value)
		{
			writer.WriteLine (value);
		}
	
		public override void WriteLine (uint value)
		{
			writer.WriteLine (value);
		}
	
		public override void WriteLine (string value)
		{
			writer.WriteLine (value);
		}
	
		public override void WriteLine (float value)
		{
			writer.WriteLine (value);
		}
	
		public override void WriteLine (object value)
		{
			writer.WriteLine (value);
		}
	
		public override void WriteLine (long value)
		{
			writer.WriteLine (value);
		}
	
		public override void WriteLine (int value)
		{
			writer.WriteLine (value);
		}
	
		public override void WriteLine (double value)
		{
			writer.WriteLine (value);
		}
	
		public override void WriteLine (decimal value)
		{
			writer.WriteLine (value);
		}
	
		public override void WriteLine (char value)
		{
			writer.WriteLine (value);
		}
	
		public override void WriteLine (bool value)
		{
			writer.WriteLine (value);
		}
	
		public override void WriteLine ()
		{
			writer.WriteLine ();
		}
	
		public override void Write (bool value)
		{
			writer.Write (value);
		}
	
		public override void Write (char value)
		{
			writer.Write (value);
		}
	
		public override void Write (decimal value)
		{
			writer.Write (value);
		}
	
		public override void Write (double value)
		{
			writer.Write (value);
		}
	
		public override void Write (int value)
		{
			writer.Write (value);
		}
	
		public override void Write (long value)
		{
			writer.Write (value);
		}
	
		public override void Write (object value)
		{
			writer.Write (value);
		}
	
		public override void Write (float value)
		{
			writer.Write (value);
		}
	
		public override void Write (string value)
		{
			writer.Write (value);
		}
	
		public override void Write (uint value)
		{
			writer.Write (value);
		}
	
		public override void Write (ulong value)
		{
			writer.Write (value);
		}
	}
}
