//
// TextEditorToIDocumentWrapper.cs
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
using ICSharpCode.NRefactory.Editor;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CSharp.NRefactoryWrapper
{
	public class DocumentWrapper : IDocument
	{
		readonly ITextDocument document;

		public DocumentWrapper (ITextDocument document)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			this.document = document;
		}
		
		#region IDocument implementation

		event EventHandler<TextChangeEventArgs> IDocument.TextChanging {
			add {
				throw new NotImplementedException ();
			}
			remove {
				throw new NotImplementedException ();
			}
		}

		event EventHandler<TextChangeEventArgs> IDocument.TextChanged {
			add {
				throw new NotImplementedException ();
			}
			remove {
				throw new NotImplementedException ();
			}
		}

		event EventHandler IDocument.ChangeCompleted {
			add {
				throw new NotImplementedException ();
			}
			remove {
				throw new NotImplementedException ();
			}
		}

		event EventHandler IDocument.FileNameChanged {
			add {
				throw new NotImplementedException ();
			}
			remove {
				throw new NotImplementedException ();
			}
		}

		IDocument IDocument.CreateDocumentSnapshot ()
		{
			return this;
		}

		ICSharpCode.NRefactory.Editor.IDocumentLine IDocument.GetLineByNumber (int lineNumber)
		{
			var line = document.GetLine (lineNumber);
			return line != null ? new DocumentLineWrapper (line) : null;
		}

		ICSharpCode.NRefactory.Editor.IDocumentLine IDocument.GetLineByOffset (int offset)
		{
			var line = document.GetLineByOffset (offset);
			return line != null ? new DocumentLineWrapper (line) : null;
		}

		int IDocument.GetOffset (int line, int column)
		{
			return document.LocationToOffset (line, column);
		}

		int IDocument.GetOffset (ICSharpCode.NRefactory.TextLocation location)
		{
			return document.LocationToOffset (location.Line, location.Column);
		}

		ICSharpCode.NRefactory.TextLocation IDocument.GetLocation (int offset)
		{
			var loc = document.OffsetToLocation (offset);
			return new ICSharpCode.NRefactory.TextLocation (loc.Line, loc.Column);
		}

		void IDocument.Insert (int offset, string text)
		{
			document.Insert (offset, text);
		}

		void IDocument.Insert (int offset, ITextSource text)
		{
			document.Insert (offset, text.Text);
		}

		void IDocument.Insert (int offset, string text, AnchorMovementType defaultAnchorMovementType)
		{
			document.Insert (offset, text);
		}

		void IDocument.Insert (int offset, ITextSource text, AnchorMovementType defaultAnchorMovementType)
		{
			document.Insert (offset, text.Text);
		}

		void IDocument.Remove (int offset, int length)
		{
			document.Remove (offset, length);
		}

		void IDocument.Replace (int offset, int length, string newText)
		{
			document.Replace (offset, length, newText);
		}

		void IDocument.Replace (int offset, int length, ITextSource newText)
		{
			document.Replace (offset, length, newText.Text);
		}

		void IDocument.StartUndoableAction ()
		{
		}

		void IDocument.EndUndoableAction ()
		{
		}

		IDisposable IDocument.OpenUndoGroup ()
		{
			return document.OpenUndoGroup ();
		}

		ITextAnchor IDocument.CreateAnchor (int offset)
		{
			throw new NotImplementedException ();
		}

		string IDocument.Text {
			get {
				return document.Text;
			}
			set {
				document.Text = value;
			}
		}

		int IDocument.LineCount {
			get {
				return document.LineCount;
			}
		}

		string IDocument.FileName {
			get {
				return document.FileName;
			}
		}

		#endregion

		#region IServiceProvider implementation

		object IServiceProvider.GetService (Type serviceType)
		{
			throw new NotImplementedException ();
		}

		#endregion

		#region ITextSource implementation

		ITextSource ITextSource.CreateSnapshot ()
		{
			throw new NotImplementedException ();
		}

		ITextSource ITextSource.CreateSnapshot (int offset, int length)
		{
			throw new NotImplementedException ();
		}

		System.IO.TextReader ITextSource.CreateReader ()
		{
			return document.CreateReader ();
		}

		System.IO.TextReader ITextSource.CreateReader (int offset, int length)
		{
			return document.CreateReader (offset, length);
		}

		char ITextSource.GetCharAt (int offset)
		{
			return document.GetCharAt (offset);
		}

		string ITextSource.GetText (int offset, int length)
		{
			return document.GetTextAt (offset, length);
		}

		string ITextSource.GetText (ISegment segment)
		{
			return document.GetTextAt (segment.Offset, segment.Length);
		}

		void ITextSource.WriteTextTo (System.IO.TextWriter writer)
		{
			document.WriteTextTo (writer);
		}

		void ITextSource.WriteTextTo (System.IO.TextWriter writer, int offset, int length)
		{
			document.WriteTextTo (writer, offset, length);
		}

		int ITextSource.IndexOf (char c, int startIndex, int count)
		{
			throw new NotImplementedException ();
		}

		int ITextSource.IndexOfAny (char[] anyOf, int startIndex, int count)
		{
			throw new NotImplementedException ();
		}

		int ITextSource.IndexOf (string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			throw new NotImplementedException ();
		}

		int ITextSource.LastIndexOf (char c, int startIndex, int count)
		{
			throw new NotImplementedException ();
		}

		int ITextSource.LastIndexOf (string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			throw new NotImplementedException ();
		}

		ITextSourceVersion ITextSource.Version {
			get {
				return new TextSourceVersionWrapper (document.Version);
			}
		}

		int ITextSource.TextLength {
			get {
				return document.Length;
			}
		}

		string ITextSource.Text {
			get {
				return document.Text;
			}
		}

		#endregion
	}
}