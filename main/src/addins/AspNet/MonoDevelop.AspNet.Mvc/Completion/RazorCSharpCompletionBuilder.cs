//
// RazorCSharpCompletionBuilder.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.AspNet.Gui;
using ICSharpCode.NRefactory.Completion;
using Mono.TextEditor;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AspNet.Mvc.Completion
{
	// Based on AspLanguageBuilder

	public class RazorCSharpCompletionBuilder : IRazorCompletionBuilder
	{
		public bool SupportsLanguage (string language)
		{
			return language == "C#";
		}

		CSharpCompletionTextEditorExtension CreateCompletion (Document realDocument, UnderlyingDocumentInfo docInfo,
			out CodeCompletionContext codeCompletionContext)
		{
			var documentLocation = docInfo.UnderlyingDocument.Editor.OffsetToLocation (docInfo.CaretPosition);

			codeCompletionContext = new CodeCompletionContext () {
				TriggerOffset = docInfo.CaretPosition,
				TriggerLine = documentLocation.Line,
				TriggerLineOffset = documentLocation.Column - 1
			};

			return new CSharpCompletionTextEditorExtension (docInfo.UnderlyingDocument) {
				CompletionWidget = CreateCompletionWidget (realDocument, docInfo)
			};
		}

		CSharpCompletionTextEditorExtension CreateCompletionAndUpdate (Document realDocument, UnderlyingDocumentInfo docInfo,
			out CodeCompletionContext codeCompletionContext)
		{
			var completion = CreateCompletion (realDocument, docInfo, out codeCompletionContext);
			completion.UpdateParsedDocument ();
			return completion;
		}

		public ICompletionWidget CreateCompletionWidget (Document realDocument,	UnderlyingDocumentInfo docInfo)
		{
			return new RazorCompletionWidget (realDocument, docInfo);
		}

		public ICompletionDataList HandlePopupCompletion (Document realDocument, UnderlyingDocumentInfo docInfo)
		{
			CodeCompletionContext ccc;
			var completion = CreateCompletionAndUpdate (realDocument, docInfo, out ccc);
			return completion.CodeCompletionCommand (ccc);
		}

		public ICompletionDataList HandleCompletion (Document realDocument,	CodeCompletionContext completionContext,
			UnderlyingDocumentInfo docInfo, char currentChar, ref int triggerWordLength)
		{
			CodeCompletionContext ccc;
			var completion = CreateCompletionAndUpdate (realDocument, docInfo, out ccc);
			return completion.HandleCodeCompletion (completionContext, currentChar, ref triggerWordLength);
		}

		public ParameterDataProvider HandleParameterCompletion (Document realDocument,	CodeCompletionContext completionContext,
			UnderlyingDocumentInfo docInfo, char completionChar)
		{
			CodeCompletionContext ccc;
			var completion = CreateCompletionAndUpdate (realDocument, docInfo, out ccc);
			return completion.HandleParameterCompletion (completionContext, completionChar);
		}

		public bool GetParameterCompletionCommandOffset (Document realDocument,	UnderlyingDocumentInfo docInfo, out int cpos)
		{
			CodeCompletionContext ccc;
			var completion = CreateCompletionAndUpdate (realDocument, docInfo, out ccc);
			return completion.GetParameterCompletionCommandOffset (out cpos);
		}

		public int GetCurrentParameterIndex (Document realDocument, UnderlyingDocumentInfo docInfo, int startOffset)
		{
			CodeCompletionContext ccc;
			var completion = CreateCompletionAndUpdate (realDocument, docInfo, out ccc);
			return completion.GetCurrentParameterIndex (startOffset);
		}
	}

	class RazorCompletionWidget : ICompletionWidget
	{
		Document realDocument;
		UnderlyingDocumentInfo docInfo;

		public RazorCompletionWidget (Document realDocument, UnderlyingDocumentInfo docInfo)
		{
			this.realDocument = realDocument;
			this.docInfo = docInfo;
		}

		#region ICompletionWidget implementation

		public CodeCompletionContext CurrentCodeCompletionContext
		{
			get	{
				return CreateCodeCompletionContext (CaretOffset);
			}
		}

		public event EventHandler CompletionContextChanged;

		public string GetText (int startOffset, int endOffset)
		{
			endOffset = Math.Min (endOffset, TextLength);
			if (endOffset <= startOffset)
				return String.Empty;
			return docInfo.UnderlyingDocument.Editor.GetTextAt (startOffset, endOffset - startOffset);
		}

		public char GetChar (int offset)
		{
			if (offset < 0 || offset >= TextLength)
				return '\0';
			return docInfo.UnderlyingDocument.Editor.GetCharAt (offset);
		}

		public void Replace (int offset, int count, string text)
		{
			if (count > 0)
				docInfo.UnderlyingDocument.Editor.Text = docInfo.UnderlyingDocument.Editor.Text.Remove (offset, count);
			if (!string.IsNullOrEmpty (text))
				docInfo.UnderlyingDocument.Editor.Text = docInfo.UnderlyingDocument.Editor.Text.Insert (offset, text);
		}

		public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset)
		{
			var savedCtx = realDocument.GetContent<ICompletionWidget> ().CreateCodeCompletionContext (
				realDocument.Editor.Caret.Offset + triggerOffset - docInfo.CaretPosition);
			var result = new CodeCompletionContext ();
			result.TriggerOffset = triggerOffset;
			var loc = docInfo.UnderlyingDocument.Editor.Document.OffsetToLocation (triggerOffset);
			result.TriggerLine = loc.Line;
			result.TriggerLineOffset = loc.Column - 1;

			result.TriggerXCoord = savedCtx.TriggerXCoord;
			result.TriggerYCoord = savedCtx.TriggerYCoord;
			result.TriggerTextHeight = savedCtx.TriggerTextHeight;

			return result;
		}

		public string GetCompletionText (CodeCompletionContext ctx)
		{
			if (ctx == null)
				return null;
			int min = Math.Min (ctx.TriggerOffset, CaretOffset);
			int max = Math.Max (ctx.TriggerOffset, CaretOffset);
			return docInfo.UnderlyingDocument.Editor.Document.GetTextBetween (min, max);
		}

		public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
		{
			SetCompletionText (ctx, partial_word, complete_word, complete_word.Length);
		}

		public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int wordOffset)
		{
			int offset = docInfo.OriginalCaretPosition + ctx.TriggerOffset - docInfo.CaretPosition;
			if (offset < 0 || offset > TextLength)
				return;

			var translatedCtx = new CodeCompletionContext ();
			translatedCtx.TriggerOffset = offset;
			var loc = docInfo.UnderlyingDocument.Editor.Document.OffsetToLocation (offset);
			translatedCtx.TriggerLine = loc.Line;
			translatedCtx.TriggerLineOffset = loc.Column - 1;
			translatedCtx.TriggerWordLength = ctx.TriggerWordLength;
				realDocument.GetContent<ICompletionWidget> ().SetCompletionText (
				translatedCtx, partial_word, complete_word, wordOffset);
		}

		public int CaretOffset
		{
			get	{
				return docInfo.UnderlyingDocument.Editor.Caret.Offset;
			}
		}

		public int TextLength
		{
			get	{
				return docInfo.UnderlyingDocument.Editor.Document.TextLength;
			}
		}

		public int SelectedLength
		{
			get	{
				return 0;
			}
		}

		public Gtk.Style GtkStyle
		{
			get	{
				return Gtk.Widget.DefaultStyle;
			}
		}

		#endregion
	}
}
