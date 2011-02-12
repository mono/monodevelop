namespace Sharpen
{
	using System;
	using System.Text;

	internal class CharsetDecoder
	{
		private Encoding enc;
		Decoder decoder;

		public CharsetDecoder (Encoding enc)
		{
			this.enc = enc;
			this.decoder = enc.GetDecoder ();
		}

		public string Decode (ByteBuffer b)
		{
			string res = enc.Decode (b);
			if (res.IndexOf ('\uFFFD') != -1 && decoder.Fallback == DecoderFallback.ExceptionFallback)
				throw new CharacterCodingException ();
			return res;
		}

		public void OnMalformedInput (CodingErrorAction action)
		{
			if (action == CodingErrorAction.REPORT)
				decoder.Fallback = DecoderFallback.ExceptionFallback;
			else
				decoder.Fallback = DecoderFallback.ReplacementFallback;
		}

		public void OnUnmappableCharacter (CodingErrorAction action)
		{
			if (action == CodingErrorAction.REPORT)
				decoder.Fallback = DecoderFallback.ExceptionFallback;
			else
				decoder.Fallback = DecoderFallback.ReplacementFallback;
		}
	}
}
