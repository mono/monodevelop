namespace Sharpen
{
	using System;
	using System.IO;

	internal class ByteArrayOutputStream : OutputStream
	{
		public ByteArrayOutputStream ()
		{
			base.Wrapped = new MemoryStream ();
		}

		public ByteArrayOutputStream (int bufferSize)
		{
			base.Wrapped = new MemoryStream (bufferSize);
		}

		public long Size ()
		{
			return ((MemoryStream)base.Wrapped).Length;
		}

		public byte[] ToByteArray ()
		{
			return ((MemoryStream)base.Wrapped).ToArray ();
		}
		
		public override void Close ()
		{
			// Closing a ByteArrayOutputStream has no effect.
		}
		
		public override string ToString ()
		{
			return System.Text.Encoding.UTF8.GetString (ToByteArray ());
		}
	}
}
