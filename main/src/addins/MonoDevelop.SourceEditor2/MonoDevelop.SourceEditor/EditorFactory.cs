//
// EditorFactory.cs
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
using MonoDevelop.SourceEditor.Wrappers;
using Mono.TextEditor;

namespace MonoDevelop.SourceEditor
{
	sealed class EditorFactory : ITextEditorFactory
	{
		#region ITextEditorFactory implementation

		ITextDocument ITextEditorFactory.CreateNewDocument ()
		{
			return new TextDocumentWrapper (new TextDocument ());
		}

		ITextDocument ITextEditorFactory.CreateNewDocument (MonoDevelop.Core.Text.ITextSource textSource, string fileName, string mimeType)
		{
			return new TextDocumentWrapper (new TextDocument (textSource.Text) {
				Encoding = textSource.Encoding,
				UseBom = textSource.UseBOM,
				MimeType = mimeType,
				FileName = fileName
			});
		}

		IReadonlyTextDocument ITextEditorFactory.CreateNewReadonlyDocument (MonoDevelop.Core.Text.ITextSource textSource, string fileName, string mimeType)
		{
			return new TextDocumentWrapper (new TextDocument (textSource.Text) {
				Encoding = textSource.Encoding,
				UseBom = textSource.UseBOM,
				ReadOnly = true,
				MimeType = mimeType,
				FileName = fileName
			});
		}

		ITextEditorImpl ITextEditorFactory.CreateNewEditor ()
		{
			return new SourceEditorView ();
		}

		ITextEditorImpl ITextEditorFactory.CreateNewEditor (IReadonlyTextDocument document)
		{
			return new SourceEditorView (document);
		}

		#endregion
	}
}