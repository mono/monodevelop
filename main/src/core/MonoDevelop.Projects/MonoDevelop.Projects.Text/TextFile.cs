//
// TextFile.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Mono.Unix;
using Mono.Unix.Native;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Text
{
	public class TextFile: IEditableTextFile
	{
		string name;
		StringBuilder text;
		string sourceEncoding;
		bool modified;
		
		public TextFile ()
		{
		}
		
		public TextFile (string name)
		{
			Read (name);
		}
		
		public void Read (string fileName)
		{
			Read (fileName, null);
		}
		
		public static TextFile ReadFile (string fileName)
		{
			TextFile tf = new TextFile ();
			tf.Read (fileName);
			return tf;
		}
		
		public static TextFile ReadFile (string fileName, string encoding)
		{
			TextFile tf = new TextFile ();
			tf.Read (fileName, encoding);
			return tf;
		}
		
		public void Read (string fileName, string encoding)
		{
			// Reads the file using the specified encoding.
			// If the encoding is null, it autodetects the
			// required encoding.
			
			this.name = fileName;
			
			FileInfo f = new FileInfo (fileName);
			byte[] content = new byte [f.Length];
			
			using (FileStream stream = f.Open (FileMode.Open, FileAccess.Read, FileShare.Read)) {
				int n = 0, nc;
				while ((nc = stream.Read (content, n, (content.Length - n))) > 0)
					n += nc;
			}
			
			if (encoding != null) {
				string s = ConvertFromEncoding (content, encoding);
				if (s == null)
					throw new Exception ("Invalid text file format");
				text = new StringBuilder (s);
				sourceEncoding = encoding;
			}
			else {
				var bomTable = new [] {
					new { Enc = "UTF-8", Bytes = new byte[] {0xEF, 0xBB, 0xBF} },
					new { Enc = "UTF-32BE", Bytes = new byte[] {0x00, 0x00, 0xFE, 0xFF} },
					new { Enc = "UTF-32LE", Bytes = new byte[] {0xFF, 0xFE, 0x00, 0x00} },
					new { Enc = "UTF-16BE", Bytes = new byte[] {0xFE, 0xFF} },
					new { Enc = "UTF-16LE", Bytes = new byte[] {0xFF, 0xFE} },
					new { Enc = "UTF-7", Bytes = new byte[] {0x2B, 0x2F, 0x76, 0x38} },
					new { Enc = "UTF-7", Bytes = new byte[] {0x2B, 0x2F, 0x76, 0x39} },
					new { Enc = "UTF-7", Bytes = new byte[] {0x2B, 0x2F, 0x76, 0x2B} },
					new { Enc = "UTF-7", Bytes = new byte[] {0x2B, 0x2F, 0x76, 0x2F} },
					new { Enc = "UTF-1", Bytes = new byte[] {0xF7, 0x64, 0x4C} },
					new { Enc = "UTF-EBCDIC", Bytes = new byte[] {0xDD, 0x73, 0x66, 073} },
					new { Enc = "SCSU", Bytes = new byte[] {0x0E, 0xFE, 0xFF} },
					new { Enc = "BOCU-1", Bytes = new byte[] {0xFB, 0xEE, 0x28} },
					new { Enc = "GB18030", Bytes = new byte[] {0x84, 0x31, 0x95, 0x33} },
				};
				string enc = (from bom in bomTable where content.StartsWith (bom.Bytes) select bom.Enc).FirstOrDefault ();

				if (!string.IsNullOrEmpty (enc)) {
					string s = ConvertFromEncoding (content, enc);
					if (s != null) {
						sourceEncoding = enc;
						text = new StringBuilder (s);
						return;
					}
				}
				
				foreach (TextEncoding co in TextEncoding.ConversionEncodings) {
					string s = ConvertFromEncoding (content, co.Id);
					if (s != null) {
						sourceEncoding = co.Id;
						text = new StringBuilder (s);
						return;
					}
				}
				
/*				if (string.IsNullOrEmpty (enc))
					enc = "UTF-8";
				string s = Convert (content, "UTF-8", enc);
				if (s != null) {
					sourceEncoding = enc;
					text = new StringBuilder (s);
					return;
				}
				enc = "ISO-8859-15";
				s = Convert (content, "UTF-8", enc);
				sourceEncoding = enc;
				text = new StringBuilder (s);
*/
				throw new Exception ("Unknown text file encoding");
				
			}
		}
		
		public static string GetFileEncoding (string fileName)
		{
			// Maybe this can be optimized later.
			TextFile file = TextFile.ReadFile (fileName);
			return file.SourceEncoding;
		}
		
		#region g_convert

		static string ConvertFromEncoding (byte[] content, string fromEncoding)
		{
			if (content.LongLength > int.MaxValue)
				throw new Exception ("Cannot handle long arrays");

			byte[] utfBytes = ConvertToBytes (content, "UTF-8", fromEncoding);
			if (utfBytes != null)
				return Encoding.UTF8.GetString (utfBytes);
			else
				return null;
		}
		
		static byte[] ConvertToBytes (byte[] content, string toEncoding, string fromEncoding)
		{
			if (content.LongLength > int.MaxValue)
				throw new Exception ("Cannot handle long arrays");
			
			IntPtr nr = IntPtr.Zero, nw = IntPtr.Zero;
			IntPtr clPtr = new IntPtr (content.Length);
			IntPtr cc = g_convert (content, clPtr, toEncoding, fromEncoding, ref nr, ref nw, IntPtr.Zero);
			if (cc != IntPtr.Zero) {
				//FIXME: check for out-of-range conversions on uints
				int len = (int)(uint)nw.ToInt64 ();
				byte[] buf = new byte [len];
				System.Runtime.InteropServices.Marshal.Copy (cc, buf, 0, buf.Length);
				g_free (cc);
				return buf;
			} else
				return null;
		}
		
		[DllImport("libglib-2.0-0.dll")]
		//note: textLength is signed, read/written are not
		static extern IntPtr g_convert(byte[] text, IntPtr textLength, string toCodeset, string fromCodeset, 
		                               ref IntPtr read, ref IntPtr written, IntPtr err);
		
		[DllImport("libglib-2.0-0.dll")]
		static extern void g_free (IntPtr ptr);
		
		#endregion
		
		public string Name {
			get { return name; } 
		}

		public bool Modified {
			get { return modified; }
		}
		
		public string SourceEncoding {
			get { return sourceEncoding; }
			set { sourceEncoding = value; }
		}
		
		public string Text {
			get { return text.ToString (); }
			set { text = new StringBuilder (value); modified = true; }
		}
		
		public int Length {
			get { return text.Length;  } 
		}
		
		public string GetText (int startPosition, int endPosition)
		{
			return text.ToString (startPosition, endPosition - startPosition);
		}
		
		public char GetCharAt (int position)
		{
			if (position < text.Length)
				return text [position];
			else
				return (char)0;
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			int lin = 1;
			int col = 1;
			for (int i = 0; i < text.Length && lin <= line; i++) {
				if (line == lin && column == col)
					return i;
				if (text[i] == '\n') {
					lin++; col = 1;
				} else
					col++;
			}
			return -1;
		}
		
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			int lin = 1;
			int col = 1;
			for (int i = 0; i < position; i++) {
				if (text[i] == '\n') {
					lin++; col = 1;
				} else
					col++;
			}
			line = lin;
			column = col;
		}
		
		public int GetLineLength (int line)
		{
			int pos = GetPositionFromLineColumn (line, 1);
			if (pos == -1) return 0;
			int len = 0;
			while (pos < text.Length && text [pos] != '\n') {
				pos++;
				len++;
			}
			return len;
		}
		
		public int InsertText (int position, string textIn)
		{
			text.Insert (position, textIn);
			modified = true;
			return textIn != null ? textIn.Length : 0;
		}
		
		public void DeleteText (int position, int length)
		{
			text.Remove (position, length);
			modified = true;
		}
		
		public void Save ()
		{
			WriteFile (name, text.ToString (), sourceEncoding);
			modified = false;
		}
		
		public static void WriteFile (string fileName, string content, string encoding)
		{
			byte[] buf = Encoding.UTF8.GetBytes (content);
			
			if (encoding != null) {
				buf = ConvertToBytes (buf, encoding, "UTF-8");
				if (buf == null)
					throw new Exception ("Invalid encoding: " + encoding);
			}
			
			string tempName = Path.GetDirectoryName (fileName) + 
				Path.DirectorySeparatorChar + ".#" + Path.GetFileName (fileName);
			FileStream fs = new FileStream (tempName, FileMode.Create, FileAccess.Write);
			fs.Write (buf, 0, buf.Length);
			fs.Flush ();
			fs.Close ();

			FileService.SystemRename (tempName, fileName);
		}
	}
	
	public static class ExtensionMethods
	{
		public static bool StartsWith<T> (this IEnumerable<T> t, IEnumerable<T> s)
		{
			using (IEnumerator<T> te = t.GetEnumerator()) {
				using (IEnumerator<T> se = s.GetEnumerator()) {
					bool didMoveTe = false;
					while ((didMoveTe = te.MoveNext ()) && se.MoveNext ()) {
						if (!te.Current.Equals (se.Current))
							return false;
					}
					return didMoveTe;
				}
			}
		}
	}
}
