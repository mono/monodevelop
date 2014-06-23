//
// ITextDocumentWrapper.cs
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
using Mono.TextEditor;
using System.IO;

namespace MonoDevelop.SourceEditor.Wrappers
{
	public class TextDocumentWrapper : ITextDocument
	{
		readonly TextDocument document;

		public TextDocument Document {
			get {
				return document;
			}
		}

		public TextDocumentWrapper (TextDocument document)
		{
			this.document = document;
			this.document.TextReplaced += HandleTextReplaced;
			this.document.TextReplacing += HandleTextReplacing;
		}

		void HandleTextReplacing (object sender, DocumentChangeEventArgs e)
		{
			var handler = textChanging;
			if (handler != null)
				handler (this, new MonoDevelop.Core.Text.TextChangeEventArgs (e.Offset, e.RemovedText.Text, e.InsertedText.Text));
		}

		void HandleTextReplaced (object sender, DocumentChangeEventArgs e)
		{
			var handler = textChanged;
			if (handler != null)
				handler (this, new MonoDevelop.Core.Text.TextChangeEventArgs (e.Offset, e.RemovedText.Text, e.InsertedText.Text));
		}
		
		#region ITextDocument implementation
		event EventHandler<MonoDevelop.Core.Text.TextChangeEventArgs> textChanging;
		event EventHandler<MonoDevelop.Core.Text.TextChangeEventArgs> ITextDocument.TextChanging {
			add {
				textChanging += value;
			}
			remove {
				textChanging -= value;
			}
		}

		event EventHandler<MonoDevelop.Core.Text.TextChangeEventArgs> textChanged;
		event EventHandler<MonoDevelop.Core.Text.TextChangeEventArgs> ITextDocument.TextChanged {
			add {
				textChanged += value;
			}
			remove {
				textChanged -= value;
			}
		}

		void ITextDocument.Insert (int offset, string text)
		{
			document.Insert (offset, text);
		}

		void ITextDocument.Remove (int offset, int length)
		{
			document.Remove (offset, length);
		}

		void ITextDocument.Replace (int offset, int length, string value)
		{
			document.Replace (offset, length, value);
		}

		void ITextDocument.Undo ()
		{
			document.Undo ();
		}

		void ITextDocument.Redo ()
		{
			document.Redo ();
		}

		IDisposable ITextDocument.OpenUndoGroup ()
		{
			return document.OpenUndoGroup ();
		}

		IReadonlyTextDocument ITextDocument.CreateDocumentSnapshot ()
		{
			throw new NotImplementedException ();
		}

		string ITextDocument.Text {
			get {
				return document.Text;
			}
			set {
				document.Text = value;
			}
		}

		bool ITextDocument.IsReadOnly {
			get {
				return document.ReadOnly;
			}
			set {
				document.ReadOnly = value;
			}
		}

		string ITextDocument.FileName {
			get {
				return document.FileName;
			}
			set {
				document.FileName = value;
			}
		}

		string ITextDocument.MimeType {
			get {
				return document.MimeType;
			}
			set {
				document.MimeType = value;
			}
		}

		bool ITextDocument.UseBOM {
			get {
				return document.UseBom;
			}
			set {
				document.UseBom = value;
			}
		}

		System.Text.Encoding ITextDocument.Encoding {
			get {
				return document.Encoding;
			}
			set {
				document.Encoding = value;
			}
		}

		bool ITextDocument.IsInAtomicUndo {
			get {
				return document.IsInAtomicUndo;
			}
		}

		#endregion

		#region IReadonlyTextDocument implementation

		int IReadonlyTextDocument.LocationToOffset (int line, int column)
		{
			return document.LocationToOffset (line, column);
		}

		MonoDevelop.Core.Text.TextLocation IReadonlyTextDocument.OffsetToLocation (int offset)
		{
			var loc = document.OffsetToLocation (offset);
			return new MonoDevelop.Core.Text.TextLocation (loc.Line, loc.Column);
		}

		IDocumentLine IReadonlyTextDocument.GetLine (int lineNumber)
		{
			return new DocumentLineWrapper (document.GetLine (lineNumber));
		}

		IDocumentLine IReadonlyTextDocument.GetLineByOffset (int offset)
		{
			return new DocumentLineWrapper (document.GetLineByOffset (offset));
		}

		bool IReadonlyTextDocument.IsReadOnly {
			get {
				return document.ReadOnly;
			}
		}

		string IReadonlyTextDocument.FileName {
			get {
				return document.FileName;
			}
		}

		string IReadonlyTextDocument.MimeType {
			get {
				return document.MimeType;
			}
		}

		int IReadonlyTextDocument.LineCount {
			get {
				return document.LineCount;
			}
		}

		#endregion

		#region ITextSource implementation

		string MonoDevelop.Core.Text.ITextSource.GetTextAt (int offset, int length)
		{
			return document.GetTextAt (offset, length);
		}

		MonoDevelop.Core.Text.ITextSourceVersion MonoDevelop.Core.Text.ITextSource.Version {
			get {
				return new TextSourceVersionWrapper (document.Version);
			}
		}

		bool MonoDevelop.Core.Text.ITextSource.UseBOM {
			get {
				return document.UseBom;
			}
		}

		System.Text.Encoding MonoDevelop.Core.Text.ITextSource.Encoding {
			get {
				return document.Encoding;
			}
		}

		int MonoDevelop.Core.Text.ITextSource.Length {
			get {
				return document.TextLength;
			}
		}

		string MonoDevelop.Core.Text.ITextSource.Text {
			get {
				return document.Text;
			}
		}

		char MonoDevelop.Core.Text.ITextSource.GetCharAt (int offset)
		{
			return document.GetCharAt (offset);
		}


		TextReader MonoDevelop.Core.Text.ITextSource.CreateReader ()
		{
			return new StringReader (document.Text);
		}

		TextReader MonoDevelop.Core.Text.ITextSource.CreateReader (int offset, int length)
		{
			return new StringReader (document.GetTextAt (offset, length));
		}

		void MonoDevelop.Core.Text.ITextSource.WriteTextTo (TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException ("writer");
			writer.Write (document.Text);
		}

		void MonoDevelop.Core.Text.ITextSource.WriteTextTo (TextWriter writer, int offset, int length)
		{
			if (writer == null)
				throw new ArgumentNullException ("writer");
			writer.Write (document.GetTextAt (offset, length));
		}
		#endregion
	}
}

