namespace Sharpen
{
	using ICSharpCode.SharpZipLib.Zip.Compression;
	using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
	using System;

	internal class InflaterInputStream : InputStream
	{
		protected InputStream @in;
		protected Inflater inf;

		public InflaterInputStream (InputStream s)
		{
			this.@in = s;
			this.inf = new Inflater ();
			base.Wrapped = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream (s.GetWrappedStream (), this.inf);
		}

		public InflaterInputStream (InputStream s, Inflater i)
		{
			this.@in = s;
			this.inf = i;
			base.Wrapped = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream (s.GetWrappedStream (), i);
		}

		public InflaterInputStream (InputStream s, Inflater i, int bufferSize)
		{
			this.@in = s;
			this.inf = i;
			base.Wrapped = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream (s.GetWrappedStream (), i, bufferSize);
		}
	}
}
