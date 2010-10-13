namespace Sharpen
{
	using System;
	using System.IO;

	internal class ByteArrayInputStream : InputStream
	{
		public ByteArrayInputStream (byte[] data)
		{
			base.Wrapped = new MemoryStream (data);
		}

		public ByteArrayInputStream (byte[] data, int off, int len)
		{
			base.Wrapped = new MemoryStream (data, off, len);
		}
		
		public override int Available ()
		{
			MemoryStream ms = (MemoryStream) Wrapped;
			return (int)(ms.Length - ms.Position);
		}
	}
}
