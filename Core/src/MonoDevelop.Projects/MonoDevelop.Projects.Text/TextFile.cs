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
				string s = Convert (content, encoding);
				if (s == null)
					throw new Exception ("Invalid text file format");
				text = new StringBuilder (s);
				sourceEncoding = encoding;
			}
			else {
				foreach (TextEncoding co in TextEncoding.ConversionEncodings) {
					string s = Convert (content, co.Id);
					if (s != null) {
						sourceEncoding = co.Id;
						text = new StringBuilder (s);
						return;
					}
				}
				throw new Exception ("Unknown text file encoding");
			}
		}
		
		static string Convert (byte[] content, string encoding)
		{
			int nr=0, nw=0;
			IntPtr cc = g_convert (content, content.Length, "UTF-8", encoding, ref nr, ref nw, IntPtr.Zero);
			if (cc != IntPtr.Zero) {
				string s = System.Runtime.InteropServices.Marshal.PtrToStringAuto (cc, nw);
				g_free (cc);
				return s;
			} else
				return null;
		}
		
		[DllImport("libglib-2.0-0.dll")]
		static extern IntPtr g_convert(byte[] text, int textLength, string toCodeset, string fromCodeset, ref int read, ref int written, IntPtr err);

		[DllImport("libglib-2.0-0.dll")]
		static extern void g_free (IntPtr ptr);
		
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
		
		public void InsertText (int position, string textIn)
		{
			text.Insert (position, textIn);
			modified = true;
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
			if (encoding == null || encoding == "UTF-8") {
				using (StreamWriter sw = new StreamWriter (fileName)) {
					sw.Write (content.ToString ());
				}
			}
			else {
				byte[] bytes = Encoding.UTF8.GetBytes (content);
				
				int nr=0, nw=0;
				IntPtr cc = g_convert (bytes, bytes.Length, encoding, "UTF-8", ref nr, ref nw, IntPtr.Zero);
				bytes = null;
				
				if (cc != IntPtr.Zero) {
					byte[] data = new byte [nw];
					System.Runtime.InteropServices.Marshal.Copy (cc, data, 0, nw);
					g_free (cc);
					using (FileStream fs = File.OpenWrite (fileName)) {
						fs.Write (data, 0, data.Length);
					}
				} else
					throw new Exception ("Invalid encoding: " + encoding);
			}
		}
	}
}
