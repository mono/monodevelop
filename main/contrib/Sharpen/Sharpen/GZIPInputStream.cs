namespace Sharpen
{
	using System;
	using System.IO.Compression;

	internal class GZIPInputStream : InputStream
	{
		public const int GZIP_MAGIC = 0;

		public GZIPInputStream (InputStream s)
		{
			Wrapped = new GZipStream (s, CompressionMode.Decompress);
		}
	}
}
