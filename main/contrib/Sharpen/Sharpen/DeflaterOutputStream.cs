namespace Sharpen
{
	using ICSharpCode.SharpZipLib.Zip.Compression;
	using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
	using System;

	internal class DeflaterOutputStream : OutputStream
	{
		public DeflaterOutputStream (OutputStream s)
		{
			base.Wrapped = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.DeflaterOutputStream (s.GetWrappedStream ());
		}

		public DeflaterOutputStream (OutputStream s, Deflater d)
		{
			base.Wrapped = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.DeflaterOutputStream (s.GetWrappedStream (), d);
		}

		public void Finish ()
		{
			((ICSharpCode.SharpZipLib.Zip.Compression.Streams.DeflaterOutputStream)base.Wrapped).Finish ();
		}
	}
}
