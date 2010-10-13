namespace Sharpen
{
	using System;
	using System.IO;

	public class InputStream : IDisposable
	{
		private long mark;
		protected Stream Wrapped;

		public static implicit operator InputStream (Stream s)
		{
			return Wrap (s);
		}

		public static implicit operator Stream (InputStream s)
		{
			return s.GetWrappedStream ();
		}
		
		public virtual int Available ()
		{
			return 0;
		}

		public virtual void Close ()
		{
			if (Wrapped != null) {
				Wrapped.Close ();
			}
		}

		public void Dispose ()
		{
			Close ();
		}

		internal Stream GetWrappedStream ()
		{
			if (Wrapped != null) {
				return Wrapped;
			}
			return new WrappedSystemStream (this);
		}

		public virtual void Mark (int readlimit)
		{
			if (Wrapped != null) {
				this.mark = Wrapped.Position;
			}
		}

		public virtual bool MarkSupported ()
		{
			return ((Wrapped != null) && Wrapped.CanSeek);
		}

		public virtual int Read ()
		{
			if (Wrapped == null) {
				throw new NotImplementedException ();
			}
			return Wrapped.ReadByte ();
		}

		public virtual int Read (byte[] buf)
		{
			return Read (buf, 0, buf.Length);
		}

		public virtual int Read (byte[] b, int off, int len)
		{
			if (Wrapped != null) {
				int num = Wrapped.Read (b, off, len);
				return ((num <= 0) ? -1 : num);
			}
			int totalRead = 0;
			while (totalRead < len) {
				int nr = Read ();
				if (nr == -1)
					return -1;
				b[off + totalRead] = (byte)nr;
				totalRead++;
			}
			return totalRead;
		}

		public virtual void Reset ()
		{
			if (Wrapped == null) {
				throw new IOException ();
			}
			Wrapped.Position = mark;
		}

		public virtual long Skip (long cnt)
		{
			long n = cnt;
			while (n > 0) {
				if (Read () == -1)
					return cnt - n;
				n--;
			}
			return cnt - n;
		}

		static internal InputStream Wrap (Stream s)
		{
			InputStream stream = new InputStream ();
			stream.Wrapped = s;
			return stream;
		}
	}
}
