namespace Sharpen
{
	using System;
	using System.IO.Compression;

	internal class GZIPOutputStream : OutputStream
	{
		public GZIPOutputStream (OutputStream os)
		{
			Wrapped = new GZipStream (os, CompressionMode.Compress);
		}
	}
}
