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
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Mono.Unix;
using Mono.Unix.Native;

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
				string s = Convert (content, "UTF-8", encoding);
				if (s == null)
					throw new Exception ("Invalid text file format");
				text = new StringBuilder (s);
				sourceEncoding = encoding;
			}
			else {
				foreach (TextEncoding co in TextEncoding.ConversionEncodings) {
					string s = Convert (content, "UTF-8", co.Id);
					if (s != null) {
						sourceEncoding = co.Id;
						text = new StringBuilder (s);
						return;
					}
				}
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
		
		static string Convert (byte[] content, string fromEncoding, string toEncoding)
		{
			if (content.LongLength > int.MaxValue)
				throw new Exception ("Cannot handle long arrays");
			
			IntPtr nr = IntPtr.Zero, nw = IntPtr.Zero;
			IntPtr clPtr = new IntPtr (content.Length);
			IntPtr cc = g_convert (content, clPtr, fromEncoding, toEncoding, ref nr, ref nw, IntPtr.Zero);
			if (cc != IntPtr.Zero) {
				//FIXME: check for out-of-range conversions on uints
				int len = (int)(uint)nw.ToInt64 ();
				string s = System.Runtime.InteropServices.Marshal.PtrToStringAuto (cc, len);
				g_free (cc);
				return s;
			} else
				return null;
		}
		
		static byte[] ConvertToBytes (byte[] content, string fromEncoding, string toEncoding)
		{
			if (content.LongLength > int.MaxValue)
				throw new Exception ("Cannot handle long arrays");
			
			IntPtr nr = IntPtr.Zero, nw = IntPtr.Zero;
			IntPtr clPtr = new IntPtr (content.Length);
			IntPtr cc = g_convert (content, clPtr, fromEncoding, toEncoding, ref nr, ref nw, IntPtr.Zero);
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
			
			if (encoding != null && encoding != "UTF-8") {
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
			
			Syscall.rename (tempName, fileName);
		}
	}
}
