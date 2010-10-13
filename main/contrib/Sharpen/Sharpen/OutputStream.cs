namespace Sharpen
{
	using System;
	using System.IO;

	public class OutputStream : IDisposable
	{
		protected Stream Wrapped;

		public static implicit operator OutputStream (Stream s)
		{
			return Wrap (s);
		}

		public static implicit operator Stream (OutputStream s)
		{
			return s.GetWrappedStream ();
		}
		
		public virtual void Close ()
		{
			if (this.Wrapped != null) {
				this.Wrapped.Close ();
			}
		}

		public void Dispose ()
		{
			this.Close ();
		}

		public virtual void Flush ()
		{
			if (this.Wrapped != null) {
				this.Wrapped.Flush ();
			}
		}

		internal Stream GetWrappedStream ()
		{
			if (this.Wrapped != null) {
				return this.Wrapped;
			}
			return new WrappedSystemStream (this);
		}

		static internal OutputStream Wrap (Stream s)
		{
			OutputStream stream = new OutputStream ();
			stream.Wrapped = s;
			return stream;
		}

		public virtual void Write (int b)
		{
			if (this.Wrapped == null) {
				throw new NotImplementedException ();
			}
			this.Wrapped.WriteByte ((byte)b);
		}

		public virtual void Write (byte[] b)
		{
			this.Write (b, 0, b.Length);
		}

		public virtual void Write (byte[] b, int offset, int len)
		{
			if (this.Wrapped != null) {
				this.Wrapped.Write (b, offset, len);
			} else {
				for (int i = 0; i < len; i++) {
					this.Write (b[i + offset]);
				}
			}
		}
	}
}
