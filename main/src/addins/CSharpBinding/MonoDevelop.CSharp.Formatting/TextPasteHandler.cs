//
// TextPasteIndentEngine.cs
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
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.CodeCompletion;
using ICSharpCode.NRefactory.Utils;

namespace MonoDevelop.CSharp.Formatting
{
	class TextPasteHandler : ICSharpCode.NRefactory.CSharp.TextPasteIndentEngine, ITextPasteHandler
	{
		readonly CSharpTextEditorIndentation indent;

		public TextPasteHandler (CSharpTextEditorIndentation indent, ICSharpCode.NRefactory.CSharp.IStateMachineIndentEngine decoratedEngine, ICSharpCode.NRefactory.CSharp.TextEditorOptions textEditorOptions, ICSharpCode.NRefactory.CSharp.CSharpFormattingOptions formattingOptions) : base (decoratedEngine, textEditorOptions, formattingOptions)
		{
			this.indent = indent;
		}

		string ITextPasteHandler.FormatPlainText (int offset, string text, byte[] copyData)
		{
			return ((ICSharpCode.NRefactory.Editor.ITextPasteHandler)this).FormatPlainText (offset, text, copyData);
		}

		byte[] ITextPasteHandler.GetCopyData (int offset, int length)
		{
			return ((ICSharpCode.NRefactory.Editor.ITextPasteHandler)this).GetCopyData (new Segment (offset, length));
		}

		class Segment : ICSharpCode.NRefactory.Editor.ISegment
		{
			readonly int offset;
			readonly int length;

			public int Offset {
				get { return offset; }
			}

			public int Length {
				get { return length; }
			}

			public int EndOffset {
				get { return Offset + Length; }
			}

			public Segment (int offset, int length)
			{
				this.offset = offset;
				this.length = length;
			}

			public override string ToString ()
			{
				return string.Format ("[Script.Segment: Offset={0}, Length={1}, EndOffset={2}]", Offset, Length, EndOffset);
			}
		}
		public void PostFomatPastedText (int insertionOffset, int insertedChars)
		{
			if (indent.textEditorData.Options.IndentStyle == IndentStyle.None ||
				indent.textEditorData.Options.IndentStyle == IndentStyle.Auto)
				return;

			// Just correct the start line of the paste operation - the text is already indented.
			var curLine = indent.textEditorData.GetLineByOffset (insertionOffset);
			var curLineOffset = curLine.Offset;
			indent.SafeUpdateIndentEngine (curLineOffset);
			if (!indent.stateTracker.IsInsideOrdinaryCommentOrString) {
				int pos = curLineOffset;
				string curIndent = curLine.GetIndentation (indent.textEditorData);
				int nlwsp = curIndent.Length;

				if (!indent.stateTracker.LineBeganInsideMultiLineComment || (nlwsp < curLine.LengthIncludingDelimiter && indent.textEditorData[curLineOffset + nlwsp] == '*')) {
					// Possibly replace the indent
					indent.SafeUpdateIndentEngine (curLineOffset + curLine.Length);
					string newIndent = indent.stateTracker.ThisLineIndent;
					if (newIndent != curIndent) {
						if (CompletionWindowManager.IsVisible) {
							if (pos < CompletionWindowManager.CodeCompletionContext.TriggerOffset)
								CompletionWindowManager.CodeCompletionContext.TriggerOffset -= nlwsp;
						}
						indent.textEditorData.Replace (pos, nlwsp, newIndent);
						//						textEditorData.Document.CommitLineUpdate (textEditorData.CaretLine);
					}
				}
			}
			indent.textEditorData.FixVirtualIndentation ();
		}

	}
}

