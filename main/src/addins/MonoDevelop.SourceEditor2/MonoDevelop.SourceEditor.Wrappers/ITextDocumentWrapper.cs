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
		}
		
		#region ITextDocument implementation

		event EventHandler<MonoDevelop.Core.Text.TextChangeEventArgs> ITextDocument.TextChanging {
			add {
				throw new NotImplementedException ();
			}
			remove {
				throw new NotImplementedException ();
			}
		}

		event EventHandler<MonoDevelop.Core.Text.TextChangeEventArgs> ITextDocument.TextChanged {
			add {
				throw new NotImplementedException ();
			}
			remove {
				throw new NotImplementedException ();
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
			throw new NotImplementedException ();
		}

		IDocumentLine IReadonlyTextDocument.GetLine (int lineNumber)
		{
			throw new NotImplementedException ();
		}

		IDocumentLine IReadonlyTextDocument.GetLineByOffset (int offset)
		{
			throw new NotImplementedException ();
		}

		bool IReadonlyTextDocument.IsReadOnly {
			get {
				throw new NotImplementedException ();
			}
		}

		string IReadonlyTextDocument.FileName {
			get {
				throw new NotImplementedException ();
			}
		}

		string IReadonlyTextDocument.MimeType {
			get {
				throw new NotImplementedException ();
			}
		}

		string IReadonlyTextDocument.EolMarker {
			get {
				throw new NotImplementedException ();
			}
		}

		int IReadonlyTextDocument.LineCount {
			get {
				throw new NotImplementedException ();
			}
		}

		#endregion

		#region ITextSource implementation

		string MonoDevelop.Core.Text.ITextSource.GetTextAt (int offset, int length)
		{
			throw new NotImplementedException ();
		}

		MonoDevelop.Core.Text.ITextSourceVersion MonoDevelop.Core.Text.ITextSource.Version {
			get {
				throw new NotImplementedException ();
			}
		}

		bool MonoDevelop.Core.Text.ITextSource.UseBOM {
			get {
				throw new NotImplementedException ();
			}
		}

		System.Text.Encoding MonoDevelop.Core.Text.ITextSource.Encoding {
			get {
				throw new NotImplementedException ();
			}
		}

		int MonoDevelop.Core.Text.ITextSource.TextLength {
			get {
				throw new NotImplementedException ();
			}
		}

		string MonoDevelop.Core.Text.ITextSource.Text {
			get {
				throw new NotImplementedException ();
			}
		}

		char MonoDevelop.Core.Text.ITextSource.this [int offset] {
			get {
				throw new NotImplementedException ();
			}
		}

		#endregion
	}
}

