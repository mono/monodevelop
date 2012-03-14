// 
// TextFileReader.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mono.TextEditor.Utils
{
	/// <summary>
	/// This class handles text input from files, streams and byte arrays with auto-detect encoding.
	/// </summary>
	public static class TextFileReader
	{
		readonly static int maxBomLength = 0;
		readonly static Encoding[] encodingsWithBom;

		static TextFileReader ()
		{
			var encodings = new List<Encoding> ();

			foreach (var info in Encoding.GetEncodings ()) {
				var encoding = info.GetEncoding ();
				var bom = encoding.GetPreamble ();
				if (bom == null || bom.Length == 0)
					continue;
				maxBomLength = System.Math.Max (maxBomLength, bom.Length);
				encodings.Add (encoding);
			}
			encodingsWithBom = encodings.ToArray ();
		}

		#region stream reader methods
		public static StreamReader OpenStream (byte[] bytes)
		{
			bool hadBom;
			return OpenStream (bytes, out hadBom);
		}

		public static StreamReader OpenStream (byte[] bytes, out bool hadBom)
		{
			if (bytes == null)
				throw new ArgumentNullException ("bytes");
			return OpenStream (new MemoryStream (bytes, false), out hadBom);
		}

		public static StreamReader OpenStream (Stream stream)
		{
			bool hadBom;
			return OpenStream (stream, out hadBom);
		}

		public static StreamReader OpenStream (Stream stream, out bool hadBom)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");
			byte[] possibleBom = new byte[maxBomLength];
			stream.Read (possibleBom, 0, System.Math.Min ((int)stream.Length, maxBomLength));

			foreach (var encoding in encodingsWithBom) {
				var bom = encoding.GetPreamble ();
				bool invalid = false;
				for (int i = 0; i < bom.Length; i++) {
					if (bom[i] != possibleBom[i]) {
						invalid = true;
						break;
					}
				}

				if (!invalid) {
					hadBom = true;
					stream.Position = bom.Length;
					return new StreamReader (stream, encoding);
				}
			}
			stream.Position = 0;
			hadBom = false;
			return new StreamReader (stream, AutoDetectEncoding (stream));
		}
		#endregion

		#region string methods
		public static string GetText (byte[] bytes)
		{
			using (var stream = OpenStream (bytes)) {
				return stream.ReadToEnd ();
			}
		}

		public static string GetText (byte[] bytes, out bool hadBom, out Encoding encoding)
		{
			if (bytes == null)
				throw new ArgumentNullException ("bytes");
			using (var stream = OpenStream (bytes, out hadBom)) {
				encoding = stream.CurrentEncoding;
				return stream.ReadToEnd ();
			}
		}

		public static string GetText (Stream inputStream)
		{
			using (var stream = OpenStream (inputStream)) {
				return stream.ReadToEnd ();
			}
		}

		public static string GetText (Stream inputStream, out bool hadBom, out Encoding encoding)
		{
			if (inputStream == null)
				throw new ArgumentNullException ("inputStream");
			using (var stream = OpenStream (inputStream, out hadBom)) {
				encoding = stream.CurrentEncoding;
				return stream.ReadToEnd ();
			}
		}

		#endregion

		#region file methods

		public static string ReadAllText (string fileName)
		{
			bool hadBom;
			Encoding encoding;
			return ReadAllText (fileName, out hadBom, out encoding);
		}

		public static string ReadAllText (string fileName, out bool hadBom, out Encoding encoding)
		{
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			using (var stream = new FileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				return GetText (stream, out hadBom, out encoding);
			}
		}

		#endregion

		#region Encoding autodetection
		readonly static Encoding fallbackEncoding = Encoding.GetEncoding (1252);

		static Encoding AutoDetectEncoding (Stream stream)
		{
			try {
				int max = (int)System.Math.Min (stream.Length, 50 * 1024);
	
				var utf8 = new Utf8Verifier ();
				var utf16 = new UnicodeVerifier ();
				var utf16BE = new BigEndianUnicodeVerifier ();

				for (int i = 0; i < max; i++) {
					var b = (byte)stream.ReadByte ();
					utf8.Advance (b);
					utf16.Advance (b);
					utf16BE.Advance (b);
				}
				if (utf8.IsValid)
					return Encoding.UTF8;
				if (utf16.IsValid)
					return Encoding.Unicode;
				if (utf16BE.IsValid)
					return Encoding.BigEndianUnicode;
			} catch (Exception) {
			}
			return fallbackEncoding;
		}

		static bool IsBinary (byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException ("bytes");
			return IsBinary (new MemoryStream (bytes, false));
		}

		static bool IsBinary (string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			using (var stream = new FileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				return IsBinary (stream);
			}
		}

		static bool IsBinary (Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");

			// Uses the same approach as GIT
			const int FIRST_FEW_BYTES = 8 * 1024;
			int length = System.Math.Min (FIRST_FEW_BYTES, (int)stream.Length);
			for (int i = 0; i < length; i++) {
				if (stream.ReadByte () == 0)
					return true;
			}
			return false;
		}

		#endregion
	}

	class Utf8Verifier
	{
		const byte Error   = 0;

		const byte UTF1     = 1;
		const byte UTFTail1 = 2;
		const byte UTFTail2 = 3;
		const byte UTFTail3 = 4;
		const byte UTF8_3_TailPre1 = 5;
		const byte UTF8_3_TailPre2 = 6;
		const byte UTF8_4_TailPre1 = 7;
		const byte UTF8_4_TailPre2 = 8;

		const byte LAST = 8;

		byte state = UTF1;

		public bool IsValid {
			get {
				return state != Error;
			}
		}

		static byte[][] table;

		static Utf8Verifier ()
		{
			table = new byte[LAST][];
			for (int i = 0; i < LAST; i++)
				table[i] = new byte[(int)byte.MaxValue + 1];

			// UTF8-1      = %x00-7F
			for (int i = 0x00; i <= 0x7F; i++) {
				table[UTF1][i] = UTF1;
			}

			// UTF8-tail   = %x80-BF
			for (int i = 0x80; i <= 0xBF; i++) {
				table[UTFTail1][i] = UTF1;
				table[UTFTail2][i] = UTFTail1;
				table[UTFTail3][i] = UTFTail2;
			}
		
			// UTF8-2 = %xC2-DF UTF8-tail
			for (int i = 0xC2; i <= 0xDF;i++)
				table[UTF1][i] = UTFTail1;

			// UTF8-3      = %xE0 %xA0-BF UTF8-tail / %xE1-EC 2( UTF8-tail ) /
			//               %xED %x80-9F UTF8-tail / %xEE-EF 2( UTF8-tail )
			for (int i = 0xA0; i <= 0xBF; i++) {
				table[UTF8_3_TailPre1][i] = UTFTail1;
			}
			for (int i = 0x80; i <= 0x9F; i++) {
				table[UTF8_3_TailPre2][i] = UTFTail1;
			}

			table[UTF1][0xE0] = UTF8_3_TailPre1;
			for (int i = 0xE1; i <= 0xEC;i++)
				table[UTF1][i] = UTFTail2;
			table[UTF1][0xED] = UTF8_3_TailPre2;
			for (int i = 0xEE; i <= 0xEE;i++)
				table[UTF1][i] = UTFTail2;

			// UTF8-4      = %xF0 %x90-BF 2( UTF8-tail ) / %xF1-F3 3( UTF8-tail ) /
			//               %xF4 %x80-8F 2( UTF8-tail )

			for (int i = 0x90; i <= 0xBF; i++) {
				table[UTF8_4_TailPre1][i] = UTFTail2;
			}
			for (int i = 0x80; i <= 0xBF; i++) {
				table[UTF8_4_TailPre2][i] = UTFTail2;
			}
			table[UTF1][0xF0] = UTF8_4_TailPre1;
			for (int i = 0xF1; i <= 0xF3;i++)
				table[UTF1][i] = UTFTail3;
			table[UTF1][0xF4] = UTF8_4_TailPre2;

			// always invalid.
			for (int i = 0; i < table.Length; i++) {
				table[i][0xC0] = Error;
				table[i][0xC1] = Error;
				table[i][0xF5] = Error;
				table[i][0xFF] = Error;
			}
		}

		public void Advance (byte b)
		{
			state = table[state][b];
		}
	}

	class UnicodeVerifier
	{
		const byte Error   = 0;
		const byte Running = 1;

		byte state = Running;

		public bool IsValid {
			get {
				return state != Error;
			}
		}
		int number = 0;
		public void Advance (byte b)
		{
			if (number % 2 == 1 && b != 0)
				state = Error;
			number++;
		}
	}

	class BigEndianUnicodeVerifier
	{
		const byte Error   = 0;
		const byte Running = 1;

		byte state = Running;

		public bool IsValid {
			get {
				return state != Error;
			}
		}
		int number = 0;
		public void Advance (byte b)
		{
			if (number % 2 == 0 && b != 0)
				state = Error;
			number++;
		}
	}

}

