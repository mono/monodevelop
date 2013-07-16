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
using MonoDevelop.Projects.Utility;

namespace MonoDevelop.Projects.Text
{
	public class TextFile: IEditableTextFile
	{
		FilePath name;
		StringBuilder text;
		string sourceEncoding;
		bool modified;
		
		public TextFile ()
		{
		}

		public TextFile (FilePath name)
		{
			Read (name);
		}

		public void Read (FilePath fileName)
		{
			Read (fileName, null);
		}

		public static TextFile ReadFile (FilePath fileName)
		{
			TextFile tf = new TextFile ();
			tf.Read (fileName);
			return tf;
		}

		public static TextFile ReadFile (FilePath fileName, string encoding)
		{
			TextFile tf = new TextFile ();
			tf.Read (fileName, encoding);
			return tf;
		}

		public static TextFile ReadFile (string path, Stream content)
		{
			TextFile tf = new TextFile ();
			tf.name = path;
			tf.Read (content, null);
			return tf;
		}

		public void Read (Stream stream, string encoding)
		{
			ByteOrderMark bom = null;
			byte[] content = null;
			long nread;

		retry:
			stream.Seek (0, SeekOrigin.Begin);

			if (encoding == null) {
				if (ByteOrderMark.TryParse (stream, out bom))
					stream.Seek (bom.Length, SeekOrigin.Begin);
				else
					stream.Seek (0, SeekOrigin.Begin);
			}

			content = new byte [bom != null ? stream.Length - bom.Length : stream.Length];
			nread = 0;

			int n;
			while ((n = stream.Read (content, (int) nread, (content.Length - (int) nread))) > 0)
				nread += n;

			if (encoding != null) {
				string s = ConvertFromEncoding (content, nread, encoding);
				if (s == null) {
					// The encoding provided was wrong, fall back to trying to use the BOM if it exists...
					encoding = null;
					content = null;
					goto retry;
				}

				text = new StringBuilder (s);
				sourceEncoding = encoding;
				return;
			}
			if (bom != null) {
				string s = ConvertFromEncoding (content, nread, bom.Name);

				if (s != null) {
					HadBOM = true;
					sourceEncoding = bom.Name;
					text = new StringBuilder (s);
					return;
				}
			}

			// Fall back to trying all the encodings...
			foreach (TextEncoding co in TextEncoding.ConversionEncodings) {
				string s = ConvertFromEncoding (content, nread, co.Id);
				if (s != null) {
					sourceEncoding = co.Id;
					text = new StringBuilder (s);
					return;
				}
			}
			throw new Exception ("Unknown text file encoding");
		}

		public void Read (FilePath fileName, string encoding)
		{
			// Reads the file using the specified encoding.
			// If the encoding is null, it autodetects the
			// required encoding.
			
			this.name = fileName;

			using (FileStream stream = new FileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				Read (stream, encoding);
			}
		}

		public static string GetFileEncoding (FilePath fileName)
		{
			// Maybe this can be optimized later.
			TextFile file = TextFile.ReadFile (fileName);
			return file.SourceEncoding;
		}
		
		#region g_convert

		static string ConvertFromEncoding (byte[] content, long nread, string fromEncoding)
		{
			try {
				return Encoding.UTF8.GetString (ConvertToBytes (content, nread, "UTF-8", fromEncoding));
			} catch (Exception e) {
				LoggingService.LogWarning ("Fail to use encoding " + fromEncoding, e);
				return null;
			}
		}

		struct GError {
			public int Domain;
			public int Code;
			public IntPtr Msg;
		}

		static unsafe int strlen (IntPtr str)
		{
			byte *s = (byte *) str;
			int n = 0;
			
			while (*s != 0) {
				s++; n++;
			}
			
			return n;
		}

		public static string Utf8PtrToString (IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return null;

			int len = strlen (ptr);
			byte[] bytes = new byte [len];
			Marshal.Copy (ptr, bytes, 0, len);
			return System.Text.Encoding.UTF8.GetString (bytes);
		}
		
		static byte[] ConvertToBytes (byte[] content, long nread, string toEncoding, string fromEncoding)
		{
			if (nread > int.MaxValue)
				throw new Exception ("Content too large.");
			if (toEncoding == fromEncoding) {
				if (content.LongLength == nread)
					return content;
				byte[] result = new byte[nread];
				Array.Copy (content, result, nread);
				return result;
			}
			
			IntPtr nr = IntPtr.Zero, nw = IntPtr.Zero;
			IntPtr clPtr = new IntPtr (nread);
			IntPtr errptr = IntPtr.Zero;
			
			IntPtr cc = g_convert (content, clPtr, toEncoding, fromEncoding, ref nr, ref nw, ref errptr);
			if (cc != IntPtr.Zero) {
				//FIXME: check for out-of-range conversions on uints
				int len = (int)(uint)nw.ToInt64 ();
				byte[] buf = new byte [len];
				System.Runtime.InteropServices.Marshal.Copy (cc, buf, 0, buf.Length);
				g_free (cc);
				return buf;
			} else {
				GError err = (GError)Marshal.PtrToStructure (errptr, typeof(GError));
				string reason = Utf8PtrToString (err.Msg);
				string message = string.Format ("Failed to convert content from {0} to {1}: {2}.", fromEncoding, toEncoding, reason);
				InvalidEncodingException ex = new InvalidEncodingException (message);
				g_error_free (errptr);
				throw ex;
			}
		}
		
		[DllImport("libglib-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		//note: textLength is signed, read/written are not
		static extern IntPtr g_convert(byte[] text, IntPtr textLength, string toCodeset, string fromCodeset, 
		                               ref IntPtr read, ref IntPtr written, ref IntPtr err);
		
		[DllImport("libglib-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void g_free (IntPtr ptr);
		
		[DllImport("libglib-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void g_error_free (IntPtr err);
		
		#endregion
		
		public FilePath Name {
			get { return name; } 
		}
		
		public bool HadBOM {
			get; private set;
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
				if (text[i] == '\r') {
					if (i + 1 < text.Length && text[i + 1] == '\n')
						i++;
					lin++; col = 1;
				} else if (text[i] == '\n') {
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
				if (text[i] == '\r') {
					if (i + 1 < position && text[i + 1] == '\n')
						i++;
					lin++; col = 1;
				} else if (text[i] == '\n') {
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
			while (pos < text.Length && text [pos] != '\n' && text [pos] != '\r') {
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
			WriteFile (name, text.ToString (), sourceEncoding, HadBOM);
			modified = false;
		}
		
		public static void WriteFile (FilePath fileName, byte[] content, string encoding, ByteOrderMark bom, bool onlyIfChanged)
		{
			int contentLength = content.Length + (bom != null ? bom.Length : 0);
			byte[] converted;
			
			if (encoding != null)
				converted = ConvertToBytes (content, content.LongLength, encoding, "UTF-8");
			else
				converted = content;
			
			if (onlyIfChanged) {
				FileInfo finfo = new FileInfo (fileName);
				if (finfo.Exists && finfo.Length == contentLength) {
					bool changed = false;
					
					// Open the file on disk and compare them byte by byte...
					using (FileStream stream = finfo.Open (FileMode.Open, FileAccess.Read, FileShare.Read)) {
						byte[] buf = new byte [4096];
						int bomOffset = 0;
						int offset = 0;
						int nread;
						int i;
						
						while (!changed && (nread = stream.Read (buf, 0, buf.Length)) > 0) {
							i = 0;
							
							if (bom != null && bomOffset < bom.Length) {
								while (i < nread && bomOffset < bom.Length) {
									if (bom.Bytes[bomOffset] != buf[i]) {
										changed = true;
										break;
									}
									
									bomOffset++;
									i++;
								}
								
								if (changed)
									break;
							}
							
							while (i < nread && offset < converted.Length) {
								if (converted[offset] != buf[i]) {
									changed = true;
									break;
								}
								
								offset++;
								i++;
							}
							
							if (offset == converted.Length && i < nread)
								changed = true;
						}
						
						if (offset < converted.Length)
							changed = true;
					}
					
					if (!changed)
						return;
				}
				
				// Content has changed...
			}
			
			string tempName = Path.GetDirectoryName (fileName) + 
				Path.DirectorySeparatorChar + ".#" + Path.GetFileName (fileName);
			FileStream fs = new FileStream (tempName, FileMode.Create, FileAccess.Write);
			
			if (bom != null)
				fs.Write (bom.Bytes, 0, bom.Length);
			
			fs.Write (converted, 0, converted.Length);
			fs.Flush ();
			fs.Close ();

			FileService.SystemRename (tempName, fileName);
		}
		
		public static void WriteFile (FilePath fileName, string content, string encoding, ByteOrderMark bom, bool onlyIfChanged)
		{
			byte[] buf = Encoding.UTF8.GetBytes (content);
			
			WriteFile (fileName, buf, encoding, bom, onlyIfChanged);
		}
		
		public static void WriteFile (FilePath fileName, string content, ByteOrderMark bom, bool onlyIfChanged)
		{
			WriteFile (fileName, content, bom != null ? bom.Name : null, bom, onlyIfChanged);
		}
		
		public static void WriteFile (FilePath fileName, string content, string encoding)
		{
			WriteFile (fileName, content, encoding, false);
		}
		
		public static void WriteFile (FilePath fileName, string content, string encoding, bool saveBOM)
		{
			ByteOrderMark bom = saveBOM && encoding != null ? ByteOrderMark.GetByName (encoding) : null;
			
			WriteFile (fileName, content, encoding, bom, false);
		}
	}
	
	
	[Serializable]
	public class InvalidEncodingException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:InvalidEncodingException"/> class
		/// </summary>
		public InvalidEncodingException ()
		{
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="T:InvalidEncodingException"/> class
		/// </summary>
		/// <param name="message">A <see cref="T:System.String"/> that describes the exception. </param>
		public InvalidEncodingException (string message) : base (message)
		{
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="T:InvalidEncodingException"/> class
		/// </summary>
		/// <param name="message">A <see cref="T:System.String"/> that describes the exception. </param>
		/// <param name="inner">The exception that is the cause of the current exception. </param>
		public InvalidEncodingException (string message, Exception inner) : base (message, inner)
		{
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
