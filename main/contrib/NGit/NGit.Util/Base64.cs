/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using NGit;
using Sharpen;

namespace NGit.Util
{
	/// <summary>Encodes and decodes to and from Base64 notation.</summary>
	/// <remarks>
	/// Encodes and decodes to and from Base64 notation.
	/// <p>
	/// I am placing this code in the Public Domain. Do with it as you will. This
	/// software comes with no guarantees or warranties but with plenty of
	/// well-wishing instead! Please visit &lt;a
	/// href="http://iharder.net/base64"&gt;http://iharder.net/base64</a> periodically
	/// to check for updates or to contribute improvements.
	/// </p>
	/// </remarks>
	/// <author>Robert Harder</author>
	/// <author>rob@iharder.net</author>
	/// <version>2.1, stripped to minimum feature set used by JGit.</version>
	public class Base64
	{
		/// <summary>The equals sign (=) as a byte.</summary>
		/// <remarks>The equals sign (=) as a byte.</remarks>
		private const sbyte EQUALS_SIGN = (sbyte)('=');

		/// <summary>Indicates equals sign in encoding.</summary>
		/// <remarks>Indicates equals sign in encoding.</remarks>
		private const sbyte EQUALS_SIGN_DEC = -1;

		/// <summary>Indicates white space in encoding.</summary>
		/// <remarks>Indicates white space in encoding.</remarks>
		private const sbyte WHITE_SPACE_DEC = -2;

		/// <summary>Indicates an invalid byte during decoding.</summary>
		/// <remarks>Indicates an invalid byte during decoding.</remarks>
		private const sbyte INVALID_DEC = -3;

		/// <summary>Preferred encoding.</summary>
		/// <remarks>Preferred encoding.</remarks>
		private static readonly string UTF_8 = "UTF-8";

		/// <summary>The 64 valid Base64 values.</summary>
		/// <remarks>The 64 valid Base64 values.</remarks>
		private static readonly byte[] ENC;

		/// <summary>
		/// Translates a Base64 value to either its 6-bit reconstruction value or a
		/// negative number indicating some other meaning.
		/// </summary>
		/// <remarks>
		/// Translates a Base64 value to either its 6-bit reconstruction value or a
		/// negative number indicating some other meaning. The table is only 7 bits
		/// wide, as the 8th bit is discarded during decoding.
		/// </remarks>
		private static readonly sbyte[] DEC;

		static Base64()
		{
			//
			//  NOTE: The following source code is heavily derived from the
			//  iHarder.net public domain Base64 library.  See the original at
			//  http://iharder.sourceforge.net/current/java/base64/
			//
			try
			{
				ENC = Sharpen.Runtime.GetBytesForString(("ABCDEFGHIJKLMNOPQRSTUVWXYZ" + "abcdefghijklmnopqrstuvwxyz"
					 + "0123456789" + "+/"), UTF_8);
			}
			catch (UnsupportedEncodingException uee)
			{
				//
				//
				//
				//
				throw new RuntimeException(uee.Message, uee);
			}
			DEC = new sbyte[128];
			Arrays.Fill(DEC, INVALID_DEC);
			for (int i = 0; i < 64; i++)
			{
				DEC[ENC[i]] = unchecked((sbyte)i);
			}
			DEC[EQUALS_SIGN] = EQUALS_SIGN_DEC;
			DEC[(sbyte)('\t')] = WHITE_SPACE_DEC;
			DEC[(sbyte)('\n')] = WHITE_SPACE_DEC;
			DEC[(sbyte)('\r')] = WHITE_SPACE_DEC;
			DEC[(sbyte)(' ')] = WHITE_SPACE_DEC;
		}

		/// <summary>Defeats instantiation.</summary>
		/// <remarks>Defeats instantiation.</remarks>
		public Base64()
		{
		}

		// Suppress empty block warning.
		/// <summary>
		/// Encodes up to three bytes of the array <var>source</var> and writes the
		/// resulting four Base64 bytes to <var>destination</var>.
		/// </summary>
		/// <remarks>
		/// Encodes up to three bytes of the array <var>source</var> and writes the
		/// resulting four Base64 bytes to <var>destination</var>. The source and
		/// destination arrays can be manipulated anywhere along their length by
		/// specifying <var>srcOffset</var> and <var>destOffset</var>. This method
		/// does not check to make sure your arrays are large enough to accommodate
		/// <var>srcOffset</var> + 3 for the <var>source</var> array or
		/// <var>destOffset</var> + 4 for the <var>destination</var> array. The
		/// actual number of significant bytes in your array is given by
		/// <var>numSigBytes</var>.
		/// </remarks>
		/// <param name="source">the array to convert</param>
		/// <param name="srcOffset">the index where conversion begins</param>
		/// <param name="numSigBytes">the number of significant bytes in your array</param>
		/// <param name="destination">the array to hold the conversion</param>
		/// <param name="destOffset">the index where output will be put</param>
		private static void Encode3to4(byte[] source, int srcOffset, int numSigBytes, byte
			[] destination, int destOffset)
		{
			// We have to shift left 24 in order to flush out the 1's that appear
			// when Java treats a value as negative that is cast from a byte.
			int inBuff = 0;
			switch (numSigBytes)
			{
				case 3:
				{
					inBuff |= (int)(((uint)(source[srcOffset + 2] << 24)) >> 24);
					goto case 2;
				}

				case 2:
				{
					//$FALL-THROUGH$
					inBuff |= (int)(((uint)(source[srcOffset + 1] << 24)) >> 16);
					goto case 1;
				}

				case 1:
				{
					//$FALL-THROUGH$
					inBuff |= (int)(((uint)(source[srcOffset] << 24)) >> 8);
					break;
				}
			}
			switch (numSigBytes)
			{
				case 3:
				{
					destination[destOffset] = ENC[((int)(((uint)inBuff) >> 18))];
					destination[destOffset + 1] = ENC[((int)(((uint)inBuff) >> 12)) & unchecked((int)
						(0x3f))];
					destination[destOffset + 2] = ENC[((int)(((uint)inBuff) >> 6)) & unchecked((int)(
						0x3f))];
					destination[destOffset + 3] = ENC[(inBuff) & unchecked((int)(0x3f))];
					break;
				}

				case 2:
				{
					destination[destOffset] = ENC[((int)(((uint)inBuff) >> 18))];
					destination[destOffset + 1] = ENC[((int)(((uint)inBuff) >> 12)) & unchecked((int)
						(0x3f))];
					destination[destOffset + 2] = ENC[((int)(((uint)inBuff) >> 6)) & unchecked((int)(
						0x3f))];
					destination[destOffset + 3] = (byte)EQUALS_SIGN;
					break;
				}

				case 1:
				{
					destination[destOffset] = ENC[((int)(((uint)inBuff) >> 18))];
					destination[destOffset + 1] = ENC[((int)(((uint)inBuff) >> 12)) & unchecked((int)
						(0x3f))];
					destination[destOffset + 2] = (byte)EQUALS_SIGN;
					destination[destOffset + 3] = (byte)EQUALS_SIGN;
					break;
				}
			}
		}

		/// <summary>Encodes a byte array into Base64 notation.</summary>
		/// <remarks>Encodes a byte array into Base64 notation.</remarks>
		/// <param name="source">The data to convert</param>
		/// <returns>encoded base64 representation of source.</returns>
		public static string EncodeBytes(byte[] source)
		{
			return EncodeBytes(source, 0, source.Length);
		}

		/// <summary>Encodes a byte array into Base64 notation.</summary>
		/// <remarks>Encodes a byte array into Base64 notation.</remarks>
		/// <param name="source">The data to convert</param>
		/// <param name="off">Offset in array where conversion should begin</param>
		/// <param name="len">Length of data to convert</param>
		/// <returns>encoded base64 representation of source.</returns>
		public static string EncodeBytes(byte[] source, int off, int len)
		{
			int len43 = len * 4 / 3;
			byte[] outBuff = new byte[len43 + ((len % 3) > 0 ? 4 : 0)];
			int d = 0;
			int e = 0;
			int len2 = len - 2;
			for (; d < len2; d += 3, e += 4)
			{
				Encode3to4(source, d + off, 3, outBuff, e);
			}
			if (d < len)
			{
				Encode3to4(source, d + off, len - d, outBuff, e);
				e += 4;
			}
			try
			{
				return Sharpen.Runtime.GetStringForBytes(outBuff, 0, e, UTF_8);
			}
			catch (UnsupportedEncodingException)
			{
				return Sharpen.Runtime.GetStringForBytes(outBuff, 0, e);
			}
		}

		/// <summary>
		/// Decodes four bytes from array <var>source</var> and writes the resulting
		/// bytes (up to three of them) to <var>destination</var>.
		/// </summary>
		/// <remarks>
		/// Decodes four bytes from array <var>source</var> and writes the resulting
		/// bytes (up to three of them) to <var>destination</var>. The source and
		/// destination arrays can be manipulated anywhere along their length by
		/// specifying <var>srcOffset</var> and <var>destOffset</var>. This method
		/// does not check to make sure your arrays are large enough to accommodate
		/// <var>srcOffset</var> + 4 for the <var>source</var> array or
		/// <var>destOffset</var> + 3 for the <var>destination</var> array. This
		/// method returns the actual number of bytes that were converted from the
		/// Base64 encoding.
		/// </remarks>
		/// <param name="source">the array to convert</param>
		/// <param name="srcOffset">the index where conversion begins</param>
		/// <param name="destination">the array to hold the conversion</param>
		/// <param name="destOffset">the index where output will be put</param>
		/// <returns>the number of decoded bytes converted</returns>
		private static int Decode4to3(byte[] source, int srcOffset, byte[] destination, int
			 destOffset)
		{
			// Example: Dk==
			if (source[srcOffset + 2] == EQUALS_SIGN)
			{
				int outBuff = ((DEC[source[srcOffset]] & unchecked((int)(0xFF))) << 18) | ((DEC[source
					[srcOffset + 1]] & unchecked((int)(0xFF))) << 12);
				destination[destOffset] = unchecked((byte)((int)(((uint)outBuff) >> 16)));
				return 1;
			}
			else
			{
				// Example: DkL=
				if (source[srcOffset + 3] == EQUALS_SIGN)
				{
					int outBuff = ((DEC[source[srcOffset]] & unchecked((int)(0xFF))) << 18) | ((DEC[source
						[srcOffset + 1]] & unchecked((int)(0xFF))) << 12) | ((DEC[source[srcOffset + 2]]
						 & unchecked((int)(0xFF))) << 6);
					destination[destOffset] = unchecked((byte)((int)(((uint)outBuff) >> 16)));
					destination[destOffset + 1] = unchecked((byte)((int)(((uint)outBuff) >> 8)));
					return 2;
				}
				else
				{
					// Example: DkLE
					int outBuff = ((DEC[source[srcOffset]] & unchecked((int)(0xFF))) << 18) | ((DEC[source
						[srcOffset + 1]] & unchecked((int)(0xFF))) << 12) | ((DEC[source[srcOffset + 2]]
						 & unchecked((int)(0xFF))) << 6) | ((DEC[source[srcOffset + 3]] & unchecked((int
						)(0xFF))));
					destination[destOffset] = unchecked((byte)(outBuff >> 16));
					destination[destOffset + 1] = unchecked((byte)(outBuff >> 8));
					destination[destOffset + 2] = unchecked((byte)(outBuff));
					return 3;
				}
			}
		}

		/// <summary>Low-level decoding ASCII characters from a byte array.</summary>
		/// <remarks>Low-level decoding ASCII characters from a byte array.</remarks>
		/// <param name="source">The Base64 encoded data</param>
		/// <param name="off">The offset of where to begin decoding</param>
		/// <param name="len">The length of characters to decode</param>
		/// <returns>decoded data</returns>
		/// <exception cref="System.ArgumentException">the input is not a valid Base64 sequence.
		/// 	</exception>
		public static byte[] DecodeBytes(byte[] source, int off, int len)
		{
			byte[] outBuff = new byte[len * 3 / 4];
			// Upper limit on size of output
			int outBuffPosn = 0;
			byte[] b4 = new byte[4];
			int b4Posn = 0;
			for (int i = off; i < off + len; i++)
			{
				byte sbiCrop = unchecked((byte)(source[i] & unchecked((int)(0x7f))));
				sbyte sbiDecode = DEC[sbiCrop];
				if (unchecked((sbyte)EQUALS_SIGN_DEC) <= sbiDecode)
				{
					b4[b4Posn++] = sbiCrop;
					if (b4Posn > 3)
					{
						outBuffPosn += Decode4to3(b4, 0, outBuff, outBuffPosn);
						b4Posn = 0;
						// If that was the equals sign, break out of 'for' loop
						if (sbiCrop == EQUALS_SIGN)
						{
							break;
						}
					}
				}
				else
				{
					if (sbiDecode != WHITE_SPACE_DEC)
					{
						throw new ArgumentException(MessageFormat.Format(JGitText.Get().badBase64InputCharacterAt
							, i, source[i] & unchecked((int)(0xff))));
					}
				}
			}
			if (outBuff.Length == outBuffPosn)
			{
				return outBuff;
			}
			byte[] @out = new byte[outBuffPosn];
			System.Array.Copy(outBuff, 0, @out, 0, outBuffPosn);
			return @out;
		}

		/// <summary>Decodes data from Base64 notation.</summary>
		/// <remarks>Decodes data from Base64 notation.</remarks>
		/// <param name="s">the string to decode</param>
		/// <returns>the decoded data</returns>
		public static byte[] DecodeBytes(string s)
		{
			byte[] bytes;
			try
			{
				bytes = Sharpen.Runtime.GetBytesForString(s, UTF_8);
			}
			catch (UnsupportedEncodingException)
			{
				bytes = Sharpen.Runtime.GetBytesForString(s);
			}
			return DecodeBytes(bytes, 0, bytes.Length);
		}
	}
}
