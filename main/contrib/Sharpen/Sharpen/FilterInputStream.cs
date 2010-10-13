namespace Sharpen
{
	using System;

	public class FilterInputStream : InputStream
	{
		protected InputStream @in;

		public FilterInputStream (InputStream s)
		{
			this.@in = s;
		}

		public override int Available ()
		{
			return this.@in.Available ();
		}

		public override void Close ()
		{
			this.@in.Close ();
		}

		public override void Mark (int readlimit)
		{
			this.@in.Mark (readlimit);
		}

		public override bool MarkSupported ()
		{
			return this.@in.MarkSupported ();
		}

		public override int Read ()
		{
			return this.@in.Read ();
		}

		public override int Read (byte[] buf)
		{
			return this.@in.Read (buf);
		}

		public override int Read (byte[] b, int off, int len)
		{
			return this.@in.Read (b, off, len);
		}

		public override void Reset ()
		{
			this.@in.Reset ();
		}

		public override long Skip (long cnt)
		{
			return this.@in.Skip (cnt);
		}
	}
}
