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
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.SourceEditor
{
	sealed class EditorFactory : ITextEditorFactory
	{

		#region ITextEditorFactory implementation

		ITextDocument ITextEditorFactory.CreateNewDocument ()
		{
			return new TextDocument ();
		}

        ITextDocument ITextEditorFactory.CreateNewDocument(string fileName, string mimeType)
        {
            return new TextDocument(fileName, mimeType);
        }

        ITextDocument ITextEditorFactory.CreateNewDocument (MonoDevelop.Core.Text.ITextSource textSource, string fileName, string mimeType)
		{
			return new TextDocument (textSource.Text, fileName, mimeType) {
				Encoding = textSource.Encoding,
			};
		}

		IReadonlyTextDocument ITextEditorFactory.CreateNewReadonlyDocument (MonoDevelop.Core.Text.ITextSource textSource, string fileName, string mimeType)
		{
			return new TextDocument (textSource.Text, fileName, mimeType) {
				Encoding = textSource.Encoding,
				IsReadOnly = true,
			};
		}

		ITextEditorImpl ITextEditorFactory.CreateNewEditor ()
		{
			return new SourceEditorView ();
		}

		ITextEditorImpl ITextEditorFactory.CreateNewEditor (string fileName, string mimeType)
		{
			return new SourceEditorView (fileName, mimeType);
		}

		ITextEditorImpl ITextEditorFactory.CreateNewEditor (IReadonlyTextDocument document)
		{
			return new SourceEditorView (document);
		}

		ITextEditorImpl ITextEditorFactory.CreateNewEditor (TextEditorType textEditorType)
		{
			return new SourceEditorView (textEditorType);
		}

		ITextEditorImpl ITextEditorFactory.CreateNewEditor (string fileName, string mimeType, TextEditorType textEditorType)
		{
			return new SourceEditorView (fileName, mimeType, textEditorType);
		}

		ITextEditorImpl ITextEditorFactory.CreateNewEditor (IReadonlyTextDocument document, TextEditorType textEditorType)
		{
			return new SourceEditorView (document, textEditorType);
		}

		string[] ITextEditorFactory.GetSyntaxProperties (string mimeType, string name)
		{
			var mode = SyntaxHighlightingService.GetSyntaxHighlightingDefinition (null, mimeType);
			if (mode == null)
				return null;
			// TODO: EditorTheme - remove the syntax properties or translate them to new language properties/services
//			System.Collections.Generic.List<string> value;
//			if (!mode.Properties.TryGetValue (name, out value))
				return null;
//			return value.ToArray ();
		}
		#endregion
	}
}