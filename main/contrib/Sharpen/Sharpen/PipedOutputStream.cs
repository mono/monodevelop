namespace Sharpen
{
	using System;

	internal class PipedOutputStream : OutputStream
	{
		PipedInputStream ips;

		public PipedOutputStream ()
		{
		}

		public PipedOutputStream (PipedInputStream iss) : this()
		{
			Attach (iss);
		}

		public override void Close ()
		{
			ips.Close ();
			base.Close ();
		}

		internal void Attach (PipedInputStream iss)
		{
			ips = iss;
		}

		public override void Write (int b)
		{
			ips.Write (b);
		}

		public override void Write (byte[] b, int offset, int len)
		{
			ips.Write (b, offset, len);
		}
	}
}
