namespace Sharpen
{
	using System;
	using System.IO;

	internal class WrappedSystemStream : Stream
	{
		private InputStream ist;
		private OutputStream ost;

		public WrappedSystemStream (InputStream ist)
		{
			this.ist = ist;
		}

		public WrappedSystemStream (OutputStream ost)
		{
			this.ost = ost;
		}

		public override void Close ()
		{
			if (this.ist != null) {
				this.ist.Close ();
			}
			if (this.ost != null) {
				this.ost.Close ();
			}
		}

		public override void Flush ()
		{
			this.ost.Flush ();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			int res = this.ist.Read (buffer, offset, count);
			return res != -1 ? res : 0;
		}

		public override int ReadByte ()
		{
			return this.ist.Read ();
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotSupportedException ();
		}

		public override void SetLength (long value)
		{
			throw new NotSupportedException ();
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			this.ost.Write (buffer, offset, count);
		}

		public override void WriteByte (byte value)
		{
			this.ost.Write (value);
		}

		public override bool CanRead {
			get { return (this.ist != null); }
		}

		public override bool CanSeek {
			get { return false; }
		}

		public override bool CanWrite {
			get { return (this.ost != null); }
		}

		public override long Length {
			get {
				throw new NotSupportedException ();
			}
		}

		public override long Position {
			get {
				throw new NotSupportedException ();
			}
			set {
				throw new NotSupportedException ();
			}
		}
	}
}
