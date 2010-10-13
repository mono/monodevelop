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
using System.IO;
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	/// <summary>Encodes and decodes to and from Base64 notation.</summary>
	/// <remarks>
	/// Encodes and decodes to and from Base64 notation.
	/// <p>
	/// Change Log:
	/// </p>
	/// <ul>
	/// <li>v2.1 - Cleaned up javadoc comments and unused variables and methods. Added
	/// some convenience methods for reading and writing to and from files.</li>
	/// <li>v2.0.2 - Now specifies UTF-8 encoding in places where the code fails on systems
	/// with other encodings (like EBCDIC).</li>
	/// <li>v2.0.1 - Fixed an error when decoding a single byte, that is, when the
	/// encoded data was a single byte.</li>
	/// <li>v2.0 - I got rid of methods that used booleans to set options.
	/// Now everything is more consolidated and cleaner. The code now detects
	/// when data that's being decoded is gzip-compressed and will decompress it
	/// automatically. Generally things are cleaner. You'll probably have to
	/// change some method calls that you were making to support the new
	/// options format (<tt>int</tt>s that you "OR" together).</li>
	/// <li>v1.5.1 - Fixed bug when decompressing and decoding to a
	/// byte[] using <tt>decode( String s, boolean gzipCompressed )</tt>.
	/// Added the ability to "suspend" encoding in the Output Stream so
	/// you can turn on and off the encoding if you need to embed base64
	/// data in an otherwise "normal" stream (like an XML file).</li>
	/// <li>v1.5 - Output stream passes on flush() command but doesn't do anything itself.
	/// This helps when using GZIP streams.
	/// Added the ability to GZip-compress objects before encoding them.</li>
	/// <li>v1.4 - Added helper methods to read/write files.</li>
	/// <li>v1.3.6 - Fixed OutputStream.flush() so that 'position' is reset.</li>
	/// <li>v1.3.5 - Added flag to turn on and off line breaks. Fixed bug in input stream
	/// where last buffer being read, if not completely full, was not returned.</li>
	/// <li>v1.3.4 - Fixed when "improperly padded stream" error was thrown at the wrong time.</li>
	/// <li>v1.3.3 - Fixed I/O streams which were totally messed up.</li>
	/// </ul>
	/// <p>
	/// I am placing this code in the Public Domain. Do with it as you will.
	/// This software comes with no guarantees or warranties but with
	/// plenty of well-wishing instead!
	/// Please visit <a href="http://iharder.net/base64">http://iharder.net/base64</a>
	/// periodically to check for updates or to contribute improvements.
	/// </p>
	/// </remarks>
	/// <author>Robert Harder</author>
	/// <author>rob@iharder.net</author>
	/// <version>2.1</version>
	public class Base64
	{
		/// <summary>No options specified.</summary>
		/// <remarks>No options specified. Value is zero.</remarks>
		public const int NO_OPTIONS = 0;

		/// <summary>Specify encoding.</summary>
		/// <remarks>Specify encoding.</remarks>
		public const int ENCODE = 1;

		/// <summary>Specify decoding.</summary>
		/// <remarks>Specify decoding.</remarks>
		public const int DECODE = 0;

		/// <summary>Specify that data should be gzip-compressed.</summary>
		/// <remarks>Specify that data should be gzip-compressed.</remarks>
		public const int GZIP = 2;

		/// <summary>Don't break lines when encoding (violates strict Base64 specification)</summary>
		public const int DONT_BREAK_LINES = 8;

		/// <summary>Maximum line length (76) of Base64 output.</summary>
		/// <remarks>Maximum line length (76) of Base64 output.</remarks>
		private const int MAX_LINE_LENGTH = 76;

		/// <summary>The equals sign (=) as a byte.</summary>
		/// <remarks>The equals sign (=) as a byte.</remarks>
		private const byte EQUALS_SIGN = unchecked((byte)(byte)('='));

		/// <summary>The new line character (\n) as a byte.</summary>
		/// <remarks>The new line character (\n) as a byte.</remarks>
		private const byte NEW_LINE = unchecked((byte)(byte)('\n'));

		/// <summary>Preferred encoding.</summary>
		/// <remarks>Preferred encoding.</remarks>
		private static readonly string PREFERRED_ENCODING = "UTF-8";

		/// <summary>The 64 valid Base64 values.</summary>
		/// <remarks>The 64 valid Base64 values.</remarks>
		private static readonly byte[] ALPHABET;

		private static readonly byte[] _NATIVE_ALPHABET = new byte[] { unchecked((byte)(byte
			)('A')), unchecked((byte)(byte)('B')), unchecked((byte)(byte)('C')), unchecked((
			byte)(byte)('D')), unchecked((byte)(byte)('E')), unchecked((byte)(byte)('F')), unchecked(
			(byte)(byte)('G')), unchecked((byte)(byte)('H')), unchecked((byte)(byte)('I')), 
			unchecked((byte)(byte)('J')), unchecked((byte)(byte)('K')), unchecked((byte)(byte
			)('L')), unchecked((byte)(byte)('M')), unchecked((byte)(byte)('N')), unchecked((
			byte)(byte)('O')), unchecked((byte)(byte)('P')), unchecked((byte)(byte)('Q')), unchecked(
			(byte)(byte)('R')), unchecked((byte)(byte)('S')), unchecked((byte)(byte)('T')), 
			unchecked((byte)(byte)('U')), unchecked((byte)(byte)('V')), unchecked((byte)(byte
			)('W')), unchecked((byte)(byte)('X')), unchecked((byte)(byte)('Y')), unchecked((
			byte)(byte)('Z')), unchecked((byte)(byte)('a')), unchecked((byte)(byte)('b')), unchecked(
			(byte)(byte)('c')), unchecked((byte)(byte)('d')), unchecked((byte)(byte)('e')), 
			unchecked((byte)(byte)('f')), unchecked((byte)(byte)('g')), unchecked((byte)(byte
			)('h')), unchecked((byte)(byte)('i')), unchecked((byte)(byte)('j')), unchecked((
			byte)(byte)('k')), unchecked((byte)(byte)('l')), unchecked((byte)(byte)('m')), unchecked(
			(byte)(byte)('n')), unchecked((byte)(byte)('o')), unchecked((byte)(byte)('p')), 
			unchecked((byte)(byte)('q')), unchecked((byte)(byte)('r')), unchecked((byte)(byte
			)('s')), unchecked((byte)(byte)('t')), unchecked((byte)(byte)('u')), unchecked((
			byte)(byte)('v')), unchecked((byte)(byte)('w')), unchecked((byte)(byte)('x')), unchecked(
			(byte)(byte)('y')), unchecked((byte)(byte)('z')), unchecked((byte)(byte)('0')), 
			unchecked((byte)(byte)('1')), unchecked((byte)(byte)('2')), unchecked((byte)(byte
			)('3')), unchecked((byte)(byte)('4')), unchecked((byte)(byte)('5')), unchecked((
			byte)(byte)('6')), unchecked((byte)(byte)('7')), unchecked((byte)(byte)('8')), unchecked(
			(byte)(byte)('9')), unchecked((byte)(byte)('+')), unchecked((byte)(byte)('/')) };

		static Base64()
		{
			//
			//  NOTE: The following source code is the iHarder.net public domain
			//  Base64 library and is provided here as a convenience.  For updates,
			//  problems, questions, etc. regarding this code, please visit:
			//  http://iharder.sourceforge.net/current/java/base64/
			//
			byte[] __bytes;
			try
			{
				__bytes = Sharpen.Runtime.GetBytesForString("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"
					, PREFERRED_ENCODING);
			}
			catch (UnsupportedEncodingException)
			{
				// end try
				__bytes = _NATIVE_ALPHABET;
			}
			// Fall back to native encoding
			// end catch
			ALPHABET = __bytes;
		}

		/// <summary>
		/// Translates a Base64 value to either its 6-bit reconstruction value
		/// or a negative number indicating some other meaning.
		/// </summary>
		/// <remarks>
		/// Translates a Base64 value to either its 6-bit reconstruction value
		/// or a negative number indicating some other meaning.
		/// </remarks>
		private static readonly byte[] DECODABET = new byte[] { unchecked((byte)(-9)), unchecked(
			(byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9))
			, unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked(
			(byte)(-9)), unchecked((byte)(-5)), unchecked((byte)(-5)), unchecked((byte)(-9))
			, unchecked((byte)(-9)), unchecked((byte)(-5)), unchecked((byte)(-9)), unchecked(
			(byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9))
			, unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked(
			(byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9))
			, unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked(
			(byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-5))
			, unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked(
			(byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9))
			, unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), 62, unchecked(
			(byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), 63, 52, 53, 54, 55, 56
			, 57, 58, 59, 60, 61, unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte
			)(-9)), unchecked((byte)(-1)), unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked(
			(byte)(-9)), 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19
			, 20, 21, 22, 23, 24, 25, unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked(
			(byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked((byte)(-9))
			, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45
			, 46, 47, 48, 49, 50, 51, unchecked((byte)(-9)), unchecked((byte)(-9)), unchecked(
			(byte)(-9)), unchecked((byte)(-9)) };

		private const byte WHITE_SPACE_ENC = unchecked((byte)(-5));

		private const byte EQUALS_SIGN_ENC = unchecked((byte)(-1));

		// end static
		// Decimal  0 -  8
		// Whitespace: Tab and Linefeed
		// Decimal 11 - 12
		// Whitespace: Carriage Return
		// Decimal 14 - 26
		// Decimal 27 - 31
		// Whitespace: Space
		// Decimal 33 - 42
		// Plus sign at decimal 43
		// Decimal 44 - 46
		// Slash at decimal 47
		// Numbers zero through nine
		// Decimal 58 - 60
		// Equals sign at decimal 61
		// Decimal 62 - 64
		// Letters 'A' through 'N'
		// Letters 'O' through 'Z'
		// Decimal 91 - 96
		// Letters 'a' through 'm'
		// Letters 'n' through 'z'
		// Decimal 123 - 126
		// I think I end up not using the BAD_ENCODING indicator.
		//private final static byte BAD_ENCODING    = -9; // Indicates error in encoding
		// Indicates white space in encoding
		// Indicates equals sign in encoding
		private static void CloseStream(IDisposable stream)
		{
			if (stream != null)
			{
				try
				{
					stream.Dispose();
				}
				catch (IOException e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
				}
			}
		}

		/// <summary>Defeats instantiation.</summary>
		/// <remarks>Defeats instantiation.</remarks>
		public Base64()
		{
		}

		//suppress empty block warning
		/// <summary>
		/// Encodes up to the first three bytes of array <var>threeBytes</var>
		/// and returns a four-byte array in Base64 notation.
		/// </summary>
		/// <remarks>
		/// Encodes up to the first three bytes of array <var>threeBytes</var>
		/// and returns a four-byte array in Base64 notation.
		/// The actual number of significant bytes in your array is
		/// given by <var>numSigBytes</var>.
		/// The array <var>threeBytes</var> needs only be as big as
		/// <var>numSigBytes</var>.
		/// Code can reuse a byte array by passing a four-byte array as <var>b4</var>.
		/// </remarks>
		/// <param name="b4">A reusable byte array to reduce array instantiation</param>
		/// <param name="threeBytes">the array to convert</param>
		/// <param name="numSigBytes">the number of significant bytes in your array</param>
		/// <returns>four byte array in Base64 notation.</returns>
		/// <since>1.5.1</since>
		private static byte[] Encode3to4(byte[] b4, byte[] threeBytes, int numSigBytes)
		{
			Encode3to4(threeBytes, 0, numSigBytes, b4, 0);
			return b4;
		}

		// end encode3to4
		/// <summary>
		/// Encodes up to three bytes of the array <var>source</var>
		/// and writes the resulting four Base64 bytes to <var>destination</var>.
		/// </summary>
		/// <remarks>
		/// Encodes up to three bytes of the array <var>source</var>
		/// and writes the resulting four Base64 bytes to <var>destination</var>.
		/// The source and destination arrays can be manipulated
		/// anywhere along their length by specifying
		/// <var>srcOffset</var> and <var>destOffset</var>.
		/// This method does not check to make sure your arrays
		/// are large enough to accommodate <var>srcOffset</var> + 3 for
		/// the <var>source</var> array or <var>destOffset</var> + 4 for
		/// the <var>destination</var> array.
		/// The actual number of significant bytes in your array is
		/// given by <var>numSigBytes</var>.
		/// </remarks>
		/// <param name="source">the array to convert</param>
		/// <param name="srcOffset">the index where conversion begins</param>
		/// <param name="numSigBytes">the number of significant bytes in your array</param>
		/// <param name="destination">the array to hold the conversion</param>
		/// <param name="destOffset">the index where output will be put</param>
		/// <returns>the <var>destination</var> array</returns>
		/// <since>1.3</since>
		private static byte[] Encode3to4(byte[] source, int srcOffset, int numSigBytes, byte
			[] destination, int destOffset)
		{
			//           1         2         3
			// 01234567890123456789012345678901 Bit position
			// --------000000001111111122222222 Array position from threeBytes
			// --------|    ||    ||    ||    | Six bit groups to index ALPHABET
			//          >>18  >>12  >> 6  >> 0  Right shift necessary
			//                0x3f  0x3f  0x3f  Additional AND
			// Create buffer with zero-padding if there are only one or two
			// significant bytes passed in the array.
			// We have to shift left 24 in order to flush out the 1's that appear
			// when Java treats a value as negative that is cast from a byte to an int.
			int inBuff = (numSigBytes > 0 ? ((int)(((uint)(source[srcOffset] << 24)) >> 8)) : 
				0) | (numSigBytes > 1 ? ((int)(((uint)(source[srcOffset + 1] << 24)) >> 16)) : 0
				) | (numSigBytes > 2 ? ((int)(((uint)(source[srcOffset + 2] << 24)) >> 24)) : 0);
			switch (numSigBytes)
			{
				case 3:
				{
					destination[destOffset] = ALPHABET[((int)(((uint)inBuff) >> 18))];
					destination[destOffset + 1] = ALPHABET[((int)(((uint)inBuff) >> 12)) & unchecked(
						(int)(0x3f))];
					destination[destOffset + 2] = ALPHABET[((int)(((uint)inBuff) >> 6)) & unchecked((
						int)(0x3f))];
					destination[destOffset + 3] = ALPHABET[(inBuff) & unchecked((int)(0x3f))];
					return destination;
				}

				case 2:
				{
					destination[destOffset] = ALPHABET[((int)(((uint)inBuff) >> 18))];
					destination[destOffset + 1] = ALPHABET[((int)(((uint)inBuff) >> 12)) & unchecked(
						(int)(0x3f))];
					destination[destOffset + 2] = ALPHABET[((int)(((uint)inBuff) >> 6)) & unchecked((
						int)(0x3f))];
					destination[destOffset + 3] = EQUALS_SIGN;
					return destination;
				}

				case 1:
				{
					destination[destOffset] = ALPHABET[((int)(((uint)inBuff) >> 18))];
					destination[destOffset + 1] = ALPHABET[((int)(((uint)inBuff) >> 12)) & unchecked(
						(int)(0x3f))];
					destination[destOffset + 2] = EQUALS_SIGN;
					destination[destOffset + 3] = EQUALS_SIGN;
					return destination;
				}

				default:
				{
					return destination;
					break;
				}
			}
		}

		// end switch
		// end encode3to4
		/// <summary>
		/// Serializes an object and returns the Base64-encoded
		/// version of that serialized object.
		/// </summary>
		/// <remarks>
		/// Serializes an object and returns the Base64-encoded
		/// version of that serialized object. If the object
		/// cannot be serialized or there is another error,
		/// the method will return <tt>null</tt>.
		/// The object is not GZip-compressed before being encoded.
		/// </remarks>
		/// <param name="serializableObject">The object to encode</param>
		/// <returns>The Base64-encoded object</returns>
		/// <since>1.4</since>
/*		public static string EncodeObject(Serializable serializableObject)
		{
			return EncodeObject(serializableObject, NO_OPTIONS);
		}

		// end encodeObject
		/// <summary>
		/// Serializes an object and returns the Base64-encoded
		/// version of that serialized object.
		/// </summary>
		/// <remarks>
		/// Serializes an object and returns the Base64-encoded
		/// version of that serialized object. If the object
		/// cannot be serialized or there is another error,
		/// the method will return <tt>null</tt>.
		/// <p>
		/// Valid options:<pre>
		/// GZIP: gzip-compresses object before encoding it.
		/// DONT_BREAK_LINES: don't break lines at 76 characters
		/// <i>Note: Technically, this makes your encoding non-compliant.</i>
		/// </pre>
		/// <p>
		/// Example: <code>encodeObject( myObj, Base64.GZIP )</code> or
		/// <p>
		/// Example: <code>encodeObject( myObj, Base64.GZIP | Base64.DONT_BREAK_LINES )</code>
		/// </remarks>
		/// <param name="serializableObject">The object to encode</param>
		/// <param name="options">Specified options</param>
		/// <returns>The Base64-encoded object</returns>
		/// <seealso cref="GZIP">GZIP</seealso>
		/// <seealso cref="DONT_BREAK_LINES">DONT_BREAK_LINES</seealso>
		/// <since>2.0</since>
		public static string EncodeObject(Serializable serializableObject, int options)
		{
			// Streams
			ByteArrayOutputStream baos = null;
			OutputStream b64os = null;
			ObjectOutputStream oos = null;
			GZIPOutputStream gzos = null;
			// Isolate options
			int gzip = (options & GZIP);
			int dontBreakLines = (options & DONT_BREAK_LINES);
			try
			{
				// ObjectOutputStream -> (GZIP) -> Base64 -> ByteArrayOutputStream
				baos = new ByteArrayOutputStream();
				b64os = new Base64.OutputStream(baos, ENCODE | dontBreakLines);
				// GZip?
				if (gzip == GZIP)
				{
					gzos = new GZIPOutputStream(b64os);
					oos = new ObjectOutputStream(gzos);
				}
				else
				{
					// end if: gzip
					oos = new ObjectOutputStream(b64os);
				}
				oos.WriteObject(serializableObject);
			}
			catch (IOException e)
			{
				// end try
				Sharpen.Runtime.PrintStackTrace(e);
				return null;
			}
			finally
			{
				// end catch
				CloseStream(oos);
				CloseStream(gzos);
				CloseStream(b64os);
				CloseStream(baos);
			}
			// end finally
			// Return value according to relevant encoding.
			try
			{
				return Sharpen.Extensions.CreateString(baos.ToByteArray(), PREFERRED_ENCODING);
			}
			catch (UnsupportedEncodingException)
			{
				// end try
				return Sharpen.Extensions.CreateString(baos.ToByteArray());
			}
		}
		 */
		// end catch
		// end encode
		/// <summary>Encodes a byte array into Base64 notation.</summary>
		/// <remarks>
		/// Encodes a byte array into Base64 notation.
		/// Does not GZip-compress data.
		/// </remarks>
		/// <param name="source">The data to convert</param>
		/// <returns>encoded base64 representation of source.</returns>
		/// <since>1.4</since>
		public static string EncodeBytes(byte[] source)
		{
			return EncodeBytes(source, 0, source.Length, NO_OPTIONS);
		}

		// end encodeBytes
		/// <summary>Encodes a byte array into Base64 notation.</summary>
		/// <remarks>
		/// Encodes a byte array into Base64 notation.
		/// <p>
		/// Valid options:<pre>
		/// GZIP: gzip-compresses object before encoding it.
		/// DONT_BREAK_LINES: don't break lines at 76 characters
		/// <i>Note: Technically, this makes your encoding non-compliant.</i>
		/// </pre>
		/// <p>
		/// Example: <code>encodeBytes( myData, Base64.GZIP )</code> or
		/// <p>
		/// Example: <code>encodeBytes( myData, Base64.GZIP | Base64.DONT_BREAK_LINES )</code>
		/// </remarks>
		/// <param name="source">The data to convert</param>
		/// <param name="options">Specified options</param>
		/// <returns>encoded base64 representation of source.</returns>
		/// <seealso cref="GZIP">GZIP</seealso>
		/// <seealso cref="DONT_BREAK_LINES">DONT_BREAK_LINES</seealso>
		/// <since>2.0</since>
		public static string EncodeBytes(byte[] source, int options)
		{
			return EncodeBytes(source, 0, source.Length, options);
		}

		// end encodeBytes
		/// <summary>Encodes a byte array into Base64 notation.</summary>
		/// <remarks>
		/// Encodes a byte array into Base64 notation.
		/// Does not GZip-compress data.
		/// </remarks>
		/// <param name="source">The data to convert</param>
		/// <param name="off">Offset in array where conversion should begin</param>
		/// <param name="len">Length of data to convert</param>
		/// <returns>encoded base64 representation of source.</returns>
		/// <since>1.4</since>
		public static string EncodeBytes(byte[] source, int off, int len)
		{
			return EncodeBytes(source, off, len, NO_OPTIONS);
		}

		// end encodeBytes
		/// <summary>Encodes a byte array into Base64 notation.</summary>
		/// <remarks>
		/// Encodes a byte array into Base64 notation.
		/// <p>
		/// Valid options:<pre>
		/// GZIP: gzip-compresses object before encoding it.
		/// DONT_BREAK_LINES: don't break lines at 76 characters
		/// <i>Note: Technically, this makes your encoding non-compliant.</i>
		/// </pre>
		/// <p>
		/// Example: <code>encodeBytes( myData, Base64.GZIP )</code> or
		/// <p>
		/// Example: <code>encodeBytes( myData, Base64.GZIP | Base64.DONT_BREAK_LINES )</code>
		/// </remarks>
		/// <param name="source">The data to convert</param>
		/// <param name="off">Offset in array where conversion should begin</param>
		/// <param name="len">Length of data to convert</param>
		/// <param name="options">Specified options</param>
		/// <returns>encoded base64 representation of source.</returns>
		/// <seealso cref="GZIP">GZIP</seealso>
		/// <seealso cref="DONT_BREAK_LINES">DONT_BREAK_LINES</seealso>
		/// <since>2.0</since>
		public static string EncodeBytes(byte[] source, int off, int len, int options)
		{
			// Isolate options
			int dontBreakLines = (options & DONT_BREAK_LINES);
			int gzip = (options & GZIP);
			// Compress?
			if (gzip == GZIP)
			{
				ByteArrayOutputStream baos = null;
				GZIPOutputStream gzos = null;
				Base64.OutputStream b64os = null;
				try
				{
					// GZip -> Base64 -> ByteArray
					baos = new ByteArrayOutputStream();
					b64os = new Base64.OutputStream(baos, ENCODE | dontBreakLines);
					gzos = new GZIPOutputStream(b64os);
					gzos.Write(source, off, len);
					gzos.Close();
				}
				catch (IOException e)
				{
					// end try
					Sharpen.Runtime.PrintStackTrace(e);
					return null;
				}
				finally
				{
					// end catch
					CloseStream(gzos);
					CloseStream(b64os);
					CloseStream(baos);
				}
				// end finally
				// Return value according to relevant encoding.
				try
				{
					return Sharpen.Extensions.CreateString(baos.ToByteArray(), PREFERRED_ENCODING);
				}
				catch (UnsupportedEncodingException)
				{
					// end try
					return Sharpen.Extensions.CreateString(baos.ToByteArray());
				}
			}
			else
			{
				// end catch
				// end if: compress
				// Else, don't compress. Better not to use streams at all then.
				// Convert option to boolean in way that code likes it.
				bool breakLines = dontBreakLines == 0;
				int len43 = len * 4 / 3;
				byte[] outBuff = new byte[(len43) + ((len % 3) > 0 ? 4 : 0) + (breakLines ? (len43
					 / MAX_LINE_LENGTH) : 0)];
				// Main 4:3
				// Account for padding
				// New lines
				int d = 0;
				int e = 0;
				int len2 = len - 2;
				int lineLength = 0;
				for (; d < len2; d += 3, e += 4)
				{
					Encode3to4(source, d + off, 3, outBuff, e);
					lineLength += 4;
					if (breakLines && lineLength == MAX_LINE_LENGTH)
					{
						outBuff[e + 4] = NEW_LINE;
						e++;
						lineLength = 0;
					}
				}
				// end if: end of line
				// end for: each piece of array
				if (d < len)
				{
					Encode3to4(source, d + off, len - d, outBuff, e);
					e += 4;
				}
				// end if: some padding needed
				// Return value according to relevant encoding.
				try
				{
					return Sharpen.Extensions.CreateString(outBuff, 0, e, PREFERRED_ENCODING);
				}
				catch (UnsupportedEncodingException)
				{
					// end try
					return Sharpen.Extensions.CreateString(outBuff, 0, e);
				}
			}
		}

		// end catch
		// end else: don't compress
		// end encodeBytes
		/// <summary>
		/// Decodes four bytes from array <var>source</var>
		/// and writes the resulting bytes (up to three of them)
		/// to <var>destination</var>.
		/// </summary>
		/// <remarks>
		/// Decodes four bytes from array <var>source</var>
		/// and writes the resulting bytes (up to three of them)
		/// to <var>destination</var>.
		/// The source and destination arrays can be manipulated
		/// anywhere along their length by specifying
		/// <var>srcOffset</var> and <var>destOffset</var>.
		/// This method does not check to make sure your arrays
		/// are large enough to accommodate <var>srcOffset</var> + 4 for
		/// the <var>source</var> array or <var>destOffset</var> + 3 for
		/// the <var>destination</var> array.
		/// This method returns the actual number of bytes that
		/// were converted from the Base64 encoding.
		/// </remarks>
		/// <param name="source">the array to convert</param>
		/// <param name="srcOffset">the index where conversion begins</param>
		/// <param name="destination">the array to hold the conversion</param>
		/// <param name="destOffset">the index where output will be put</param>
		/// <returns>the number of decoded bytes converted</returns>
		/// <since>1.3</since>
		private static int Decode4to3(byte[] source, int srcOffset, byte[] destination, int
			 destOffset)
		{
			// Example: Dk==
			if (source[srcOffset + 2] == EQUALS_SIGN)
			{
				// Two ways to do the same thing. Don't know which way I like best.
				//int outBuff =   ( ( DECODABET[ source[ srcOffset    ] ] << 24 ) >>>  6 )
				//              | ( ( DECODABET[ source[ srcOffset + 1] ] << 24 ) >>> 12 );
				int outBuff = ((DECODABET[source[srcOffset]] & unchecked((int)(0xFF))) << 18) | (
					(DECODABET[source[srcOffset + 1]] & unchecked((int)(0xFF))) << 12);
				destination[destOffset] = unchecked((byte)((int)(((uint)outBuff) >> 16)));
				return 1;
			}
			else
			{
				// Example: DkL=
				if (source[srcOffset + 3] == EQUALS_SIGN)
				{
					// Two ways to do the same thing. Don't know which way I like best.
					//int outBuff =   ( ( DECODABET[ source[ srcOffset     ] ] << 24 ) >>>  6 )
					//              | ( ( DECODABET[ source[ srcOffset + 1 ] ] << 24 ) >>> 12 )
					//              | ( ( DECODABET[ source[ srcOffset + 2 ] ] << 24 ) >>> 18 );
					int outBuff = ((DECODABET[source[srcOffset]] & unchecked((int)(0xFF))) << 18) | (
						(DECODABET[source[srcOffset + 1]] & unchecked((int)(0xFF))) << 12) | ((DECODABET
						[source[srcOffset + 2]] & unchecked((int)(0xFF))) << 6);
					destination[destOffset] = unchecked((byte)((int)(((uint)outBuff) >> 16)));
					destination[destOffset + 1] = unchecked((byte)((int)(((uint)outBuff) >> 8)));
					return 2;
				}
				else
				{
					// Example: DkLE
					try
					{
						// Two ways to do the same thing. Don't know which way I like best.
						//int outBuff =   ( ( DECODABET[ source[ srcOffset     ] ] << 24 ) >>>  6 )
						//              | ( ( DECODABET[ source[ srcOffset + 1 ] ] << 24 ) >>> 12 )
						//              | ( ( DECODABET[ source[ srcOffset + 2 ] ] << 24 ) >>> 18 )
						//              | ( ( DECODABET[ source[ srcOffset + 3 ] ] << 24 ) >>> 24 );
						int outBuff = ((DECODABET[source[srcOffset]] & unchecked((int)(0xFF))) << 18) | (
							(DECODABET[source[srcOffset + 1]] & unchecked((int)(0xFF))) << 12) | ((DECODABET
							[source[srcOffset + 2]] & unchecked((int)(0xFF))) << 6) | ((DECODABET[source[srcOffset
							 + 3]] & unchecked((int)(0xFF))));
						destination[destOffset] = unchecked((byte)(outBuff >> 16));
						destination[destOffset + 1] = unchecked((byte)(outBuff >> 8));
						destination[destOffset + 2] = unchecked((byte)(outBuff));
						return 3;
					}
					catch (Exception)
					{
						System.Console.Out.WriteLine(string.Empty + source[srcOffset] + ": " + (DECODABET
							[source[srcOffset]]));
						System.Console.Out.WriteLine(string.Empty + source[srcOffset + 1] + ": " + (DECODABET
							[source[srcOffset + 1]]));
						System.Console.Out.WriteLine(string.Empty + source[srcOffset + 2] + ": " + (DECODABET
							[source[srcOffset + 2]]));
						System.Console.Out.WriteLine(string.Empty + source[srcOffset + 3] + ": " + (DECODABET
							[source[srcOffset + 3]]));
						return -1;
					}
				}
			}
		}

		//e nd catch
		// end decodeToBytes
		/// <summary>
		/// Very low-level access to decoding ASCII characters in
		/// the form of a byte array.
		/// </summary>
		/// <remarks>
		/// Very low-level access to decoding ASCII characters in
		/// the form of a byte array. Does not support automatically
		/// gunzipping or any other "fancy" features.
		/// </remarks>
		/// <param name="source">The Base64 encoded data</param>
		/// <param name="off">The offset of where to begin decoding</param>
		/// <param name="len">The length of characters to decode</param>
		/// <returns>decoded data</returns>
		/// <since>1.3</since>
		public static byte[] DecodeBytes(byte[] source, int off, int len)
		{
			int len34 = len * 3 / 4;
			byte[] outBuff = new byte[len34];
			// Upper limit on size of output
			int outBuffPosn = 0;
			byte[] b4 = new byte[4];
			int b4Posn = 0;
			int i = 0;
			byte sbiCrop = 0;
			byte sbiDecode = 0;
			for (i = off; i < off + len; i++)
			{
				sbiCrop = unchecked((byte)(source[i] & unchecked((int)(0x7f))));
				// Only the low seven bits
				sbiDecode = DECODABET[sbiCrop];
				if (sbiDecode >= WHITE_SPACE_ENC)
				{
					// White space, Equals sign or better
					if (sbiDecode >= EQUALS_SIGN_ENC)
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
				}
				else
				{
					// end if: quartet built
					// end if: equals sign or better
					// end if: white space, equals sign or better
					System.Console.Error.WriteLine(MessageFormat.Format(JGitText.Get().badBase64InputCharacterAt
						, i + source[i]));
					return null;
				}
			}
			// end else:
			// each input character
			byte[] @out = new byte[outBuffPosn];
			System.Array.Copy(outBuff, 0, @out, 0, outBuffPosn);
			return @out;
		}

		// end decode
		/// <summary>
		/// Decodes data from Base64 notation, automatically
		/// detecting gzip-compressed data and decompressing it.
		/// </summary>
		/// <remarks>
		/// Decodes data from Base64 notation, automatically
		/// detecting gzip-compressed data and decompressing it.
		/// </remarks>
		/// <param name="s">the string to decode</param>
		/// <returns>the decoded data</returns>
		/// <since>1.4</since>
		public static byte[] DecodeBytes(string s)
		{
			byte[] bytes;
			try
			{
				bytes = Sharpen.Runtime.GetBytesForString(s, PREFERRED_ENCODING);
			}
			catch (UnsupportedEncodingException)
			{
				// end try
				bytes = Sharpen.Runtime.GetBytesForString(s);
			}
			// end catch
			//</change>
			// Decode
			bytes = DecodeBytes(bytes, 0, bytes.Length);
			// Check to see if it's gzip-compressed
			// GZIP Magic Two-Byte Number: 0x8b1f (35615)
			if (bytes != null && bytes.Length >= 4)
			{
				int head = (bytes[0] & unchecked((int)(0xff))) | ((bytes[1] << 8) & unchecked((int
					)(0xff00)));
				if (GZIPInputStream.GZIP_MAGIC == head)
				{
					ByteArrayInputStream bais = null;
					GZIPInputStream gzis = null;
					ByteArrayOutputStream baos = null;
					byte[] buffer = new byte[2048];
					int length = 0;
					try
					{
						baos = new ByteArrayOutputStream();
						bais = new ByteArrayInputStream(bytes);
						gzis = new GZIPInputStream(bais);
						while ((length = gzis.Read(buffer)) >= 0)
						{
							baos.Write(buffer, 0, length);
						}
						// end while: reading input
						// No error? Get new bytes.
						bytes = baos.ToByteArray();
					}
					catch (IOException)
					{
					}
					finally
					{
						// end try
						// Just return originally-decoded bytes
						// end catch
						CloseStream(baos);
						CloseStream(gzis);
						CloseStream(bais);
					}
				}
			}
			// end finally
			// end if: gzipped
			// end if: bytes.length >= 2
			return bytes;
		}

		// end decode
		/// <summary>
		/// Attempts to decode Base64 data and deserialize a Java
		/// Object within.
		/// </summary>
		/// <remarks>
		/// Attempts to decode Base64 data and deserialize a Java
		/// Object within. Returns <tt>null</tt> if there was an error.
		/// </remarks>
		/// <param name="encodedObject">The Base64 data to decode</param>
		/// <returns>The decoded and deserialized object</returns>
		/// <since>1.5</since>
		public static object DecodeToObject(string encodedObject)
		{
			// Decode and gunzip if necessary
			byte[] objBytes = DecodeBytes(encodedObject);
			ByteArrayInputStream bais = null;
			ObjectInputStream ois = null;
			object obj = null;
			try
			{
				bais = new ByteArrayInputStream(objBytes);
				ois = new ObjectInputStream(bais);
				obj = ois.ReadObject();
			}
			catch (IOException e)
			{
				// end try
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (TypeLoadException e)
			{
				// end catch
				Sharpen.Runtime.PrintStackTrace(e);
			}
			finally
			{
				// end catch
				CloseStream(bais);
				CloseStream(ois);
			}
			// end finally
			return obj;
		}

		// end decodeObject
		/// <summary>Convenience method for encoding data to a file.</summary>
		/// <remarks>Convenience method for encoding data to a file.</remarks>
		/// <param name="dataToEncode">byte array of data to encode in base64 form</param>
		/// <param name="filename">Filename for saving encoded data</param>
		/// <returns><tt>true</tt> if successful, <tt>false</tt> otherwise</returns>
		/// <since>2.1</since>
		public static bool EncodeToFile(byte[] dataToEncode, string filename)
		{
			bool success = false;
			Base64.OutputStream bos = null;
			try
			{
				bos = new Base64.OutputStream(new FileOutputStream(filename), NGit.Util.Base64.ENCODE
					);
				bos.Write(dataToEncode);
				success = true;
			}
			catch (IOException)
			{
				// end try
				success = false;
			}
			finally
			{
				// end catch: IOException
				CloseStream(bos);
			}
			// end finally
			return success;
		}

		// end encodeToFile
		/// <summary>Convenience method for decoding data to a file.</summary>
		/// <remarks>Convenience method for decoding data to a file.</remarks>
		/// <param name="dataToDecode">Base64-encoded data as a string</param>
		/// <param name="filename">Filename for saving decoded data</param>
		/// <returns><tt>true</tt> if successful, <tt>false</tt> otherwise</returns>
		/// <since>2.1</since>
		public static bool DecodeToFile(string dataToDecode, string filename)
		{
			bool success = false;
			Base64.OutputStream bos = null;
			try
			{
				bos = new Base64.OutputStream(new FileOutputStream(filename), NGit.Util.Base64.DECODE
					);
				bos.Write(Sharpen.Runtime.GetBytesForString(dataToDecode, PREFERRED_ENCODING));
				success = true;
			}
			catch (IOException)
			{
				// end try
				success = false;
			}
			finally
			{
				// end catch: IOException
				CloseStream(bos);
			}
			// end finally
			return success;
		}

		// end decodeToFile
		/// <summary>
		/// Convenience method for reading a base64-encoded
		/// file and decoding it.
		/// </summary>
		/// <remarks>
		/// Convenience method for reading a base64-encoded
		/// file and decoding it.
		/// </remarks>
		/// <param name="filename">Filename for reading encoded data</param>
		/// <returns>decoded byte array or null if unsuccessful</returns>
		/// <since>2.1</since>
		public static byte[] DecodeFromFile(string filename)
		{
			byte[] decodedData = null;
			Base64.InputStream bis = null;
			try
			{
				// Set up some useful variables
				FilePath file = new FilePath(filename);
				byte[] buffer = null;
				int length = 0;
				int numBytes = 0;
				// Check for size of file
				if (file.Length() > int.MaxValue)
				{
					System.Console.Error.WriteLine(MessageFormat.Format(JGitText.Get().fileIsTooBigForThisConvenienceMethod
						, file.Length()));
					return null;
				}
				// end if: file too big for int index
				buffer = new byte[(int)file.Length()];
				// Open a stream
				bis = new Base64.InputStream(new BufferedInputStream(new FileInputStream(file)), 
					NGit.Util.Base64.DECODE);
				// Read until done
				while ((numBytes = bis.Read(buffer, length, 4096)) >= 0)
				{
					length += numBytes;
				}
				// Save in a variable to return
				decodedData = new byte[length];
				System.Array.Copy(buffer, 0, decodedData, 0, length);
			}
			catch (IOException)
			{
				// end try
				System.Console.Error.WriteLine(MessageFormat.Format(JGitText.Get().errorDecodingFromFile
					, filename));
			}
			finally
			{
				// end catch: IOException
				CloseStream(bis);
			}
			// end finally
			return decodedData;
		}

		// end decodeFromFile
		/// <summary>
		/// Convenience method for reading a binary file
		/// and base64-encoding it.
		/// </summary>
		/// <remarks>
		/// Convenience method for reading a binary file
		/// and base64-encoding it.
		/// </remarks>
		/// <param name="filename">Filename for reading binary data</param>
		/// <returns>base64-encoded string or null if unsuccessful</returns>
		/// <since>2.1</since>
		public static string EncodeFromFile(string filename)
		{
			string encodedData = null;
			Base64.InputStream bis = null;
			try
			{
				// Set up some useful variables
				FilePath file = new FilePath(filename);
				byte[] buffer = new byte[(int)(file.Length() * 1.4)];
				int length = 0;
				int numBytes = 0;
				// Open a stream
				bis = new Base64.InputStream(new BufferedInputStream(new FileInputStream(file)), 
					NGit.Util.Base64.ENCODE);
				// Read until done
				while ((numBytes = bis.Read(buffer, length, 4096)) >= 0)
				{
					length += numBytes;
				}
				// Save in a variable to return
				encodedData = Sharpen.Extensions.CreateString(buffer, 0, length, NGit.Util.Base64
					.PREFERRED_ENCODING);
			}
			catch (IOException)
			{
				// end try
				System.Console.Error.WriteLine(MessageFormat.Format(JGitText.Get().errorEncodingFromFile
					, filename));
			}
			finally
			{
				// end catch: IOException
				CloseStream(bis);
			}
			// end finally
			return encodedData;
		}

		/// <summary>
		/// A
		/// <see cref="InputStream">InputStream</see>
		/// will read data from another
		/// <tt>java.io.InputStream</tt>, given in the constructor,
		/// and encode/decode to/from Base64 notation on the fly.
		/// </summary>
		/// <seealso cref="Base64">Base64</seealso>
		/// <since>1.3</since>
		public class InputStream : FilterInputStream
		{
			private bool encode;

			private int position;

			private byte[] buffer;

			private int bufferLength;

			private int numSigBytes;

			private int lineLength;

			private bool breakLines;

			/// <summary>
			/// Constructs a
			/// <see cref="InputStream">InputStream</see>
			/// in DECODE mode.
			/// </summary>
			/// <param name="in">the <tt>java.io.InputStream</tt> from which to read data.</param>
			/// <since>1.3</since>
			protected InputStream(Sharpen.InputStream @in) : this(@in, DECODE)
			{
			}

			/// <summary>
			/// Constructs a
			/// <see cref="InputStream">InputStream</see>
			/// in
			/// either ENCODE or DECODE mode.
			/// <p>
			/// Valid options:<pre>
			/// ENCODE or DECODE: Encode or Decode as data is read.
			/// DONT_BREAK_LINES: don't break lines at 76 characters
			/// (only meaningful when encoding)
			/// <i>Note: Technically, this makes your encoding non-compliant.</i>
			/// </pre>
			/// <p>
			/// Example: <code>new Base64.InputStream( in, Base64.DECODE )</code>
			/// </summary>
			/// <param name="in">the <tt>java.io.InputStream</tt> from which to read data.</param>
			/// <param name="options">Specified options</param>
			/// <seealso cref="Base64.ENCODE">Base64.ENCODE</seealso>
			/// <seealso cref="Base64.DECODE">Base64.DECODE</seealso>
			/// <seealso cref="Base64.DONT_BREAK_LINES">Base64.DONT_BREAK_LINES</seealso>
			/// <since>2.0</since>
			public InputStream(Sharpen.InputStream @in, int options) : base(@in)
			{
				// end encodeFromFile
				// Encoding or decoding
				// Current position in the buffer
				// Small buffer holding converted data
				// Length of buffer (3 or 4)
				// Number of meaningful bytes in the buffer
				// Break lines at less than 80 characters
				// end constructor
				this.breakLines = (options & DONT_BREAK_LINES) != DONT_BREAK_LINES;
				this.encode = (options & ENCODE) == ENCODE;
				this.bufferLength = encode ? 4 : 3;
				this.buffer = new byte[bufferLength];
				this.position = -1;
				this.lineLength = 0;
			}

			// end constructor
			/// <summary>
			/// Reads enough of the input stream to convert
			/// to/from Base64 and returns the next byte.
			/// </summary>
			/// <remarks>
			/// Reads enough of the input stream to convert
			/// to/from Base64 and returns the next byte.
			/// </remarks>
			/// <returns>next byte</returns>
			/// <since>1.3</since>
			/// <exception cref="System.IO.IOException"></exception>
			public override int Read()
			{
				// Do we need to get data?
				if (position < 0)
				{
					if (encode)
					{
						byte[] b3 = new byte[3];
						int numBinaryBytes = 0;
						for (int i = 0; i < 3; i++)
						{
							try
							{
								int b = @in.Read();
								// If end of stream, b is -1.
								if (b >= 0)
								{
									b3[i] = unchecked((byte)b);
									numBinaryBytes++;
								}
							}
							catch (IOException e)
							{
								// end if: not end of stream
								// end try: read
								// Only a problem if we got no data at all.
								if (i == 0)
								{
									throw;
								}
							}
						}
						// end catch
						// end for: each needed input byte
						if (numBinaryBytes > 0)
						{
							Encode3to4(b3, 0, numBinaryBytes, buffer, 0);
							position = 0;
							numSigBytes = 4;
						}
						else
						{
							// end if: got data
							return -1;
						}
					}
					else
					{
						// end else
						// end if: encoding
						// Else decoding
						byte[] b4 = new byte[4];
						int i = 0;
						for (i = 0; i < 4; i++)
						{
							// Read four "meaningful" bytes:
							int b = 0;
							do
							{
								b = @in.Read();
							}
							while (b >= 0 && ((sbyte)DECODABET[b & unchecked((int)(0x7f))]) <= WHITE_SPACE_ENC);
							if (b < 0)
							{
								break;
							}
							// Reads a -1 if end of stream
							b4[i] = unchecked((byte)b);
						}
						// end for: each needed input byte
						if (i == 4)
						{
							numSigBytes = Decode4to3(b4, 0, buffer, 0);
							position = 0;
						}
						else
						{
							// end if: got four characters
							if (i == 0)
							{
								return -1;
							}
							else
							{
								// end else if: also padded correctly
								// Must have broken out from above.
								throw new IOException(JGitText.Get().improperlyPaddedBase64Input);
							}
						}
					}
				}
				// end
				// end else: decode
				// end else: get data
				// Got data?
				if (position >= 0)
				{
					// End of relevant data?
					if (position >= numSigBytes)
					{
						return -1;
					}
					if (encode && breakLines && lineLength >= MAX_LINE_LENGTH)
					{
						lineLength = 0;
						return '\n';
					}
					else
					{
						// end if
						lineLength++;
						// This isn't important when decoding
						// but throwing an extra "if" seems
						// just as wasteful.
						int b = buffer[position++];
						if (position >= bufferLength)
						{
							position = -1;
						}
						return b & unchecked((int)(0xFF));
					}
				}
				else
				{
					// This is how you "cast" a byte that's
					// intended to be unsigned.
					// end else
					// end if: position >= 0
					// Else error
					// When JDK1.4 is more accepted, use an assertion here.
					throw new IOException(JGitText.Get().errorInBase64CodeReadingStream);
				}
			}

			// end else
			// end read
			/// <summary>
			/// Calls
			/// <see cref="Read()">Read()</see>
			/// repeatedly until the end of stream
			/// is reached or <var>len</var> bytes are read.
			/// Returns number of bytes read into array or -1 if
			/// end of stream is encountered.
			/// </summary>
			/// <param name="dest">array to hold values</param>
			/// <param name="off">offset for array</param>
			/// <param name="len">max number of bytes to read into array</param>
			/// <returns>bytes read into array or -1 if end of stream is encountered.</returns>
			/// <since>1.3</since>
			/// <exception cref="System.IO.IOException"></exception>
			public override int Read(byte[] dest, int off, int len)
			{
				int i;
				int b;
				for (i = 0; i < len; i++)
				{
					b = Read();
					//if( b < 0 && i == 0 )
					//    return -1;
					if (b >= 0)
					{
						dest[off + i] = unchecked((byte)b);
					}
					else
					{
						if (i == 0)
						{
							return -1;
						}
						else
						{
							break;
						}
					}
				}
				// Out of 'for' loop
				// end for: each byte read
				return i;
			}
			// end read
		}

		/// <summary>
		/// A
		/// <see cref="OutputStream">OutputStream</see>
		/// will write data to another
		/// <tt>java.io.OutputStream</tt>, given in the constructor,
		/// and encode/decode to/from Base64 notation on the fly.
		/// </summary>
		/// <seealso cref="Base64">Base64</seealso>
		/// <since>1.3</since>
		public class OutputStream : FilterOutputStream
		{
			private bool encode;

			private int position;

			private byte[] buffer;

			private int bufferLength;

			private int lineLength;

			private bool breakLines;

			private byte[] b4;

			private bool suspendEncoding;

			/// <summary>
			/// Constructs a
			/// <see cref="OutputStream">OutputStream</see>
			/// in ENCODE mode.
			/// </summary>
			/// <param name="out">the <tt>java.io.OutputStream</tt> to which data will be written.
			/// 	</param>
			/// <since>1.3</since>
			public OutputStream(Sharpen.OutputStream @out) : this(@out, ENCODE)
			{
			}

			/// <summary>
			/// Constructs a
			/// <see cref="OutputStream">OutputStream</see>
			/// in
			/// either ENCODE or DECODE mode.
			/// <p>
			/// Valid options:<pre>
			/// ENCODE or DECODE: Encode or Decode as data is read.
			/// DONT_BREAK_LINES: don't break lines at 76 characters
			/// (only meaningful when encoding)
			/// <i>Note: Technically, this makes your encoding non-compliant.</i>
			/// </pre>
			/// <p>
			/// Example: <code>new Base64.OutputStream( out, Base64.ENCODE )</code>
			/// </summary>
			/// <param name="out">the <tt>java.io.OutputStream</tt> to which data will be written.
			/// 	</param>
			/// <param name="options">Specified options.</param>
			/// <seealso cref="Base64.ENCODE">Base64.ENCODE</seealso>
			/// <seealso cref="Base64.DECODE">Base64.DECODE</seealso>
			/// <seealso cref="Base64.DONT_BREAK_LINES">Base64.DONT_BREAK_LINES</seealso>
			/// <since>1.3</since>
			public OutputStream(Sharpen.OutputStream @out, int options) : base(@out)
			{
				// end inner class InputStream
				// Scratch used in a few places
				// end constructor
				this.breakLines = (options & DONT_BREAK_LINES) != DONT_BREAK_LINES;
				this.encode = (options & ENCODE) == ENCODE;
				this.bufferLength = encode ? 3 : 4;
				this.buffer = new byte[bufferLength];
				this.position = 0;
				this.lineLength = 0;
				this.suspendEncoding = false;
				this.b4 = new byte[4];
			}

			// end constructor
			/// <summary>
			/// Writes the byte to the output stream after
			/// converting to/from Base64 notation.
			/// </summary>
			/// <remarks>
			/// Writes the byte to the output stream after
			/// converting to/from Base64 notation.
			/// When encoding, bytes are buffered three
			/// at a time before the output stream actually
			/// gets a write() call.
			/// When decoding, bytes are buffered four
			/// at a time.
			/// </remarks>
			/// <param name="theByte">the byte to write</param>
			/// <since>1.3</since>
			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(int theByte)
			{
				// Encoding suspended?
				if (suspendEncoding)
				{
					@out.Write(theByte);
					return;
				}
				// end if: suspended
				// Encode?
				if (encode)
				{
					buffer[position++] = unchecked((byte)theByte);
					if (position >= bufferLength)
					{
						// Enough to encode.
						@out.Write(Encode3to4(b4, buffer, bufferLength));
						lineLength += 4;
						if (breakLines && lineLength >= MAX_LINE_LENGTH)
						{
							@out.Write(NEW_LINE);
							lineLength = 0;
						}
						// end if: end of line
						position = 0;
					}
				}
				else
				{
					// end if: enough to output
					// end if: encoding
					// Else, Decoding
					// Meaningful Base64 character?
					if (DECODABET[theByte & unchecked((int)(0x7f))] > WHITE_SPACE_ENC)
					{
						buffer[position++] = unchecked((byte)theByte);
						if (position >= bufferLength)
						{
							// Enough to output.
							int len = Base64.Decode4to3(buffer, 0, b4, 0);
							@out.Write(b4, 0, len);
							//out.write( Base64.decode4to3( buffer ) );
							position = 0;
						}
					}
					else
					{
						// end if: enough to output
						// end if: meaningful base64 character
						if (DECODABET[theByte & unchecked((int)(0x7f))] != WHITE_SPACE_ENC)
						{
							throw new IOException(JGitText.Get().invalidCharacterInBase64Data);
						}
					}
				}
			}

			// end else: not white space either
			// end else: decoding
			// end write
			/// <summary>
			/// Calls
			/// <see cref="Write(int)">Write(int)</see>
			/// repeatedly until <var>len</var>
			/// bytes are written.
			/// </summary>
			/// <param name="theBytes">array from which to read bytes</param>
			/// <param name="off">offset for array</param>
			/// <param name="len">max number of bytes to read into array</param>
			/// <since>1.3</since>
			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(byte[] theBytes, int off, int len)
			{
				// Encoding suspended?
				if (suspendEncoding)
				{
					@out.Write(theBytes, off, len);
					return;
				}
				// end if: suspended
				for (int i = 0; i < len; i++)
				{
					Write(theBytes[off + i]);
				}
			}

			// end for: each byte written
			// end write
			/// <summary>Method added by PHIL.</summary>
			/// <remarks>
			/// Method added by PHIL. [Thanks, PHIL. -Rob]
			/// This pads the buffer without closing the stream.
			/// </remarks>
			/// <exception cref="System.IO.IOException">input was not properly padded.</exception>
			public virtual void FlushBase64()
			{
				if (position > 0)
				{
					if (encode)
					{
						@out.Write(Encode3to4(b4, buffer, position));
						position = 0;
					}
					else
					{
						// end if: encoding
						throw new IOException(JGitText.Get().base64InputNotProperlyPadded);
					}
				}
			}

			// end else: decoding
			// end if: buffer partially full
			// end flush
			/// <summary>Flushes and closes (I think, in the superclass) the stream.</summary>
			/// <remarks>Flushes and closes (I think, in the superclass) the stream.</remarks>
			/// <since>1.3</since>
			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				// 1. Ensure that pending characters are written
				FlushBase64();
				// 2. Actually close the stream
				// Base class both flushes and closes.
				base.Close();
				buffer = null;
				@out = null;
			}

			// end close
			/// <summary>Suspends encoding of the stream.</summary>
			/// <remarks>
			/// Suspends encoding of the stream.
			/// May be helpful if you need to embed a piece of
			/// base640-encoded data in a stream.
			/// </remarks>
			/// <exception cref="System.IO.IOException">input was not properly padded.</exception>
			/// <since>1.5.1</since>
			public virtual void SuspendEncoding()
			{
				FlushBase64();
				this.suspendEncoding = true;
			}

			// end suspendEncoding
			/// <summary>Resumes encoding of the stream.</summary>
			/// <remarks>
			/// Resumes encoding of the stream.
			/// May be helpful if you need to embed a piece of
			/// base640-encoded data in a stream.
			/// </remarks>
			/// <since>1.5.1</since>
			public virtual void ResumeEncoding()
			{
				this.suspendEncoding = false;
			}
			// end resumeEncoding
		}
		// end inner class OutputStream
	}
}
