//
// ReadonlyDocumentSnapshot.cs
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
using MonoDevelop.Ide.Editor;
using Mono.TextEditor.Utils;

namespace MonoDevelop.SourceEditor.Wrappers
{
	class ReadonlyDocumentSnapshot : IReadonlyTextDocument
	{
		readonly Mono.TextEditor.TextDocument snapshot;
		readonly MonoDevelop.Core.Text.ITextSourceVersion version;

		public ReadonlyDocumentSnapshot (Mono.TextEditor.TextDocument textDocument)
		{
			snapshot = textDocument.CreateDocumentSnapshot ();
			version = new TextSourceVersionWrapper (textDocument.Version);
		}

		#region IReadonlyTextDocument implementation

		int IReadonlyTextDocument.LocationToOffset (int line, int column)
		{
			return snapshot.LocationToOffset (line, column);
		}

		DocumentLocation IReadonlyTextDocument.OffsetToLocation (int offset)
		{
			var loc = snapshot.OffsetToLocation (offset);
			return new MonoDevelop.Ide.Editor.DocumentLocation (loc.Line, loc.Column);
		}

		IDocumentLine IReadonlyTextDocument.GetLine (int lineNumber)
		{
			var line = snapshot.GetLine (lineNumber);
			return line != null ? new DocumentLineWrapper (line) : null;
		}

		IDocumentLine IReadonlyTextDocument.GetLineByOffset (int offset)
		{
			var line = snapshot.GetLineByOffset (offset);
			return line != null ? new DocumentLineWrapper (line) : null;
		}

		bool IReadonlyTextDocument.IsReadOnly {
			get {
				return true;
			}
		}

		MonoDevelop.Core.FilePath IReadonlyTextDocument.FileName {
			get {
				return snapshot.FileName;
			}
		}

		string IReadonlyTextDocument.MimeType {
			get {
				return snapshot.MimeType;
			}
		}

		int IReadonlyTextDocument.LineCount {
			get {
				return snapshot.LineCount;
			}
		}

		#endregion

		#region ITextSource implementation

		char MonoDevelop.Core.Text.ITextSource.GetCharAt (int offset)
		{
			return snapshot.GetCharAt (offset);
		}

		string MonoDevelop.Core.Text.ITextSource.GetTextAt (int offset, int length)
		{
			return snapshot.GetTextAt (offset, length);
		}

		System.IO.TextReader MonoDevelop.Core.Text.ITextSource.CreateReader ()
		{
			return snapshot.CreateReader ();
		}

		System.IO.TextReader MonoDevelop.Core.Text.ITextSource.CreateReader (int offset, int length)
		{
			return snapshot.CreateReader (offset, length);
		}

		void MonoDevelop.Core.Text.ITextSource.WriteTextTo (System.IO.TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException ("writer");
			writer.Write (snapshot.Text);
		}

		void MonoDevelop.Core.Text.ITextSource.WriteTextTo (System.IO.TextWriter writer, int offset, int length)
		{
			if (writer == null)
				throw new ArgumentNullException ("writer");
			writer.Write (snapshot.GetTextAt (offset, length));
		}

		MonoDevelop.Core.Text.ITextSource MonoDevelop.Core.Text.ITextSource.CreateSnapshot ()
		{
			return this;
		}

		MonoDevelop.Core.Text.ITextSource MonoDevelop.Core.Text.ITextSource.CreateSnapshot (int offset, int length)
		{
			return new RopeTextSource (snapshot.CloneRope (offset, length), snapshot.Encoding, snapshot.UseBom);
		}

		MonoDevelop.Core.Text.ITextSourceVersion MonoDevelop.Core.Text.ITextSource.Version {
			get {
				return version;
			}
		}

		bool MonoDevelop.Core.Text.ITextSource.UseBOM {
			get {
				return snapshot.UseBom;
			}
		}

		System.Text.Encoding MonoDevelop.Core.Text.ITextSource.Encoding {
			get {
				return snapshot.Encoding;
			}
		}

		int MonoDevelop.Core.Text.ITextSource.Length {
			get {
				return snapshot.TextLength;
			}
		}

		string MonoDevelop.Core.Text.ITextSource.Text {
			get {
				return snapshot.Text;
			}
		}

		#endregion
	}
}

