namespace Sharpen
{
	using System;
	using System.Text;

	internal class CharsetEncoder
	{
		private Encoding enc;

		public CharsetEncoder (Encoding enc)
		{
			this.enc = enc;
		}

		public ByteBuffer Encode (CharSequence str)
		{
			return Encode (str.ToString ());
		}

		public ByteBuffer Encode (string str)
		{
			return ByteBuffer.Wrap (enc.GetBytes (str));
		}
	}
}
