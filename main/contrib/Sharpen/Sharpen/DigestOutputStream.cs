namespace Sharpen
{
	using System;

	public class DigestOutputStream : OutputStream
	{
		private MessageDigest digest;
		private bool @on = true;
		private OutputStream os;

		public DigestOutputStream (OutputStream os, MessageDigest md)
		{
			this.os = os;
			this.digest = md;
		}

		public override void Close ()
		{
			os.Close ();
		}

		public override void Flush ()
		{
			os.Flush ();
		}

		public MessageDigest GetMessageDigest ()
		{
			return digest;
		}

		public void On (bool b)
		{
			@on = b;
		}

		public override void Write (int b)
		{
			if (@on) {
				digest.Update ((byte)b);
			}
			os.Write (b);
		}

		public override void Write (byte[] b, int offset, int len)
		{
			if (@on) {
				digest.Update (b, offset, len);
			}
			os.Write (b, offset, len);
		}
	}
}
