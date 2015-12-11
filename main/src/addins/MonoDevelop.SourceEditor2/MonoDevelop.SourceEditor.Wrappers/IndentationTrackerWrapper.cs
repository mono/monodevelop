// IndentationTrackerWrapper.cs
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
using Mono.TextEditor;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.SourceEditor.Wrappers
{
	class IndentationTrackerWrapper : Mono.TextEditor.IIndentationTracker
	{
		readonly IReadonlyTextDocument document;
		readonly MonoDevelop.Ide.Editor.Extension.IndentationTracker indentationTracker;

		readonly Mono.TextEditor.TextEditorData textEditorData;


		public IndentationTrackerWrapper (Mono.TextEditor.TextEditorData textEditorData, IReadonlyTextDocument document, MonoDevelop.Ide.Editor.Extension.IndentationTracker indentationTracker)
		{
			if (textEditorData == null)
				throw new System.ArgumentNullException ("textEditorData");
			if (document == null)
				throw new System.ArgumentNullException ("document");
			if (indentationTracker == null)
				throw new System.ArgumentNullException ("indentationTracker");
			this.textEditorData = textEditorData;
			this.document = document;
			this.indentationTracker = indentationTracker;
		}

		#region IIndentationTracker implementation

		IndentatitonTrackerFeatures IIndentationTracker.SupportedFeatures {
			get {
				return (IndentatitonTrackerFeatures)indentationTracker.SupportedFeatures;
			}
		}

		string Mono.TextEditor.IIndentationTracker.GetIndentationString (int offset)
		{
			return indentationTracker.GetIndentationString (document.OffsetToLineNumber (offset));
		}

		string Mono.TextEditor.IIndentationTracker.GetIndentationString (int lineNumber, int column)
		{
			return indentationTracker.GetIndentationString (lineNumber);
		}

		static int CountIndent (string str)
		{
			// '\t' == 1 - virtual indent is here the character indent not the visual one.
			return str.Length;
		}

		int Mono.TextEditor.IIndentationTracker.GetVirtualIndentationColumn (int offset)
		{
			return 1 + CountIndent(((Mono.TextEditor.IIndentationTracker)this).GetIndentationString (offset));
		}

		int Mono.TextEditor.IIndentationTracker.GetVirtualIndentationColumn (int lineNumber, int column)
		{
			return 1 + CountIndent(((Mono.TextEditor.IIndentationTracker)this).GetIndentationString (lineNumber, column));
		}
		#endregion
	}
}
