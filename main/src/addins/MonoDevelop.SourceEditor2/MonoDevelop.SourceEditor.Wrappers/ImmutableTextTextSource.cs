//
// RopeTextSource.cs
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
using MonoDevelop.Core.Text;
using Mono.TextEditor.Utils;

namespace MonoDevelop.SourceEditor.Wrappers
{
	class ImmutableTextTextSource : ITextSource
	{
		readonly ImmutableText immutableText;
		readonly ITextSourceVersion version;

		public ImmutableTextTextSource (ImmutableText immutableText, System.Text.Encoding encoding, bool useBom, ITextSourceVersion version = null)
		{
			if (immutableText == null)
				throw new ArgumentNullException (nameof (immutableText));
			this.immutableText = immutableText;
			this.encoding = encoding;
			UseBOM = useBom;
			this.version = version;
		}

		#region ITextSource implementation
		char ITextSource.GetCharAt (int offset)
		{
			return immutableText [offset];
		}

		char ITextSource.this [int offset] {
			get {
				return immutableText [offset];
			}
		}

		string ITextSource.GetTextAt (int offset, int length)
		{
			return immutableText.ToString (offset, length);
		}

		System.IO.TextReader ITextSource.CreateReader ()
		{
			return new ImmutableTextTextReader (immutableText);
		}

		System.IO.TextReader ITextSource.CreateReader (int offset, int length)
		{
			return new ImmutableTextTextReader (immutableText.GetText (offset, length));
		}

		void ITextSource.WriteTextTo (System.IO.TextWriter writer)
		{
			immutableText.WriteTo (writer, 0, immutableText.Length);
		}

		void ITextSource.WriteTextTo (System.IO.TextWriter writer, int offset, int length)
		{
			immutableText.WriteTo (writer, offset, length);
		}

		ITextSource ITextSource.CreateSnapshot ()
		{
			return this;
		}

		ITextSource ITextSource.CreateSnapshot (int offset, int length)
		{
			return new ImmutableTextTextSource (immutableText.GetText (offset, length), Encoding, UseBOM);
		}

		ITextSourceVersion ITextSource.Version {
			get {
				return version;
			}
		}

		public bool UseBOM {
			get;
			private set;
		}

		System.Text.Encoding encoding;
		public System.Text.Encoding Encoding {
			get {
				return encoding ?? System.Text.Encoding.UTF8;
			}
		}

		int ITextSource.Length {
			get {
				return immutableText.Length;
			}
		}

		string ITextSource.Text {
			get {
				return immutableText.ToString ();
			}
		}
		#endregion
	}
}