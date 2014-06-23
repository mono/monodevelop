//
// StringTextSource.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using System.IO;

namespace MonoDevelop.Core.Text
{
	/// <summary>
	/// Implements the ITextSource interface using a string.
	/// Note that objects from this class are immutable.
	/// </summary>
	[Serializable]
	public class StringTextSource : ITextSource
	{
		/// <summary>
		/// Gets a text source containing the empty string.
		/// </summary>
		public static readonly StringTextSource Empty = new StringTextSource (string.Empty);

		readonly string text;
		readonly ITextSourceVersion version;

		/// <summary>
		/// Determines if a byte order mark was read or is going to be written.
		/// </summary>
		public bool UseBOM { get; private set; }

		/// <summary>
		/// Encoding of the text that was read from or is going to be saved to.
		/// </summary>
		public Encoding Encoding { get; private set; }

		/// <summary>
		/// Creates a new StringTextSource with the given text.
		/// </summary>
		public StringTextSource (string text, Encoding encoding = null, bool useBom = true)
		{
			if (text == null)
				throw new ArgumentNullException ("text");
			this.text = text;
			this.UseBOM = useBom;
			this.Encoding = encoding ?? Encoding.UTF8;
		}

		/// <summary>
		/// Creates a new StringTextSource with the given text.
		/// </summary>
		public StringTextSource (string text, ITextSourceVersion version, Encoding encoding = null, bool useBom = true)
		{
			if (text == null)
				throw new ArgumentNullException ("text");
			this.text = text;
			this.version = version;
			this.UseBOM = useBom;
			this.Encoding = encoding ?? Encoding.UTF8;
		}

		/// <inheritdoc/>
		public ITextSourceVersion Version {
			get { return version; }
		}

		/// <inheritdoc/>
		public int Length {
			get { return text.Length; }
		}

		/// <inheritdoc/>
		public string Text {
			get { return text; }
		}

		/// <inheritdoc/>
		public ITextSource CreateSnapshot ()
		{
			return this; // StringTextSource is immutable
		}

		/// <inheritdoc/>
		public ITextSource CreateSnapshot (int offset, int length)
		{
			return new StringTextSource (text.Substring (offset, length));
		}

		/// <inheritdoc/>
		public char this [int offset] {
			get {
				return text [offset];
			}
		}

		/// <inheritdoc/>
		public string GetTextAt (int offset, int length)
		{
			return text.Substring (offset, length);
		}

		public StringTextSource WithEncoding (Encoding encoding)
		{
			return new StringTextSource (text, encoding, UseBOM);
		}

		public StringTextSource WithBom (bool useBom)
		{
			return new StringTextSource (text, Encoding, useBom);
		}

		public static StringTextSource ReadFrom (string fileName)
		{
			bool hadBom;
			Encoding encoding;
			var text = TextFileUtility.ReadAllText (fileName, out hadBom, out encoding);
			return new StringTextSource (text, encoding, hadBom);
		}

		/// <inheritdoc/>
		public TextReader CreateReader ()
		{
			return new StringReader (text);
		}

		/// <inheritdoc/>
		public TextReader CreateReader (int offset, int length)
		{
			return new StringReader (text.Substring (offset, length));
		}

		/// <inheritdoc/>
		public void WriteTextTo (TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException ("writer");
			writer.Write (text);
		}

		/// <inheritdoc/>
		public void WriteTextTo (TextWriter writer, int offset, int length)
		{
			if (writer == null)
				throw new ArgumentNullException ("writer");
			writer.Write (text.Substring (offset, length));
		}
	}
}