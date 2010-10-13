namespace Sharpen
{
	using System;
	using System.IO;

	internal class BufferedInputStream : InputStream
	{
		public BufferedInputStream (InputStream s)
		{
			base.Wrapped = new BufferedStream (s.GetWrappedStream ());
		}

		public BufferedInputStream (InputStream s, int bufferSize)
		{
			base.Wrapped = new BufferedStream (s.GetWrappedStream (), bufferSize);
		}
	}
}
