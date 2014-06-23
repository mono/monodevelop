// SourceEditorView.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.SourceEditor.Wrappers
{
	class TextPasteHandlerWrapper : ICSharpCode.NRefactory.Editor.ITextPasteHandler, IDisposable
	{
		readonly MonoDevelop.Ide.Editor.Extension.ITextPasteHandler textPasteHandler;
		readonly Mono.TextEditor.TextEditorData data;

		public TextPasteHandlerWrapper (Mono.TextEditor.TextEditorData data, MonoDevelop.Ide.Editor.Extension.ITextPasteHandler textPasteHandler)
		{
			this.data = data;
			this.textPasteHandler = textPasteHandler;
			data.Paste += HandlePaste;
		}

		void HandlePaste (int insertionOffset, string text, int insertedChars)
		{
			textPasteHandler.PostFomatPastedText (insertionOffset, insertedChars);
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			data.Paste -= HandlePaste;
		}

		#endregion

		#region ITextPasteHandler implementation

		string ICSharpCode.NRefactory.Editor.ITextPasteHandler.FormatPlainText (int offset, string text, byte[] copyData)
		{
			return textPasteHandler.FormatPlainText (offset, text, copyData);
		}

		byte[] ICSharpCode.NRefactory.Editor.ITextPasteHandler.GetCopyData (ICSharpCode.NRefactory.Editor.ISegment segment)
		{
			return textPasteHandler.GetCopyData (segment.Offset, segment.Length);
		}
		#endregion
	}
}