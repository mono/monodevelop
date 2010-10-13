namespace Sharpen
{
	using System;
	using System.IO;

	internal class BufferedOutputStream : OutputStream
	{
		public BufferedOutputStream (OutputStream outs)
		{
			base.Wrapped = new BufferedStream (outs.GetWrappedStream ());
		}

		public BufferedOutputStream (OutputStream outs, int bufferSize)
		{
			base.Wrapped = new BufferedStream (outs.GetWrappedStream (), bufferSize);
		}
	}
}
