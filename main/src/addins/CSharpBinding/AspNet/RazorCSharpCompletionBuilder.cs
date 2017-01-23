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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
//using ICSharpCode.NRefactory6.CSharp.Completion;
using MonoDevelop.AspNet.Razor;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	// Based on AspLanguageBuilder

	class RazorCSharpCompletionBuilder : IRazorCompletionBuilder
	{
		public bool SupportsLanguage (string language)
		{
			return language == "C#";
		}

		CSharpCompletionTextEditorExtension CreateCompletion (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context, UnderlyingDocumentInfo docInfo,
			out CodeCompletionContext codeCompletionContext)
		{
			var documentLocation = docInfo.UnderlyingDocument.Editor.OffsetToLocation (docInfo.CaretPosition);

			codeCompletionContext = new CodeCompletionContext () {
				TriggerOffset = docInfo.CaretPosition,
				TriggerLine = documentLocation.Line,
				TriggerLineOffset = documentLocation.Column - 1
			};

			return new CSharpCompletionTextEditorExtension (docInfo.UnderlyingDocument) {
				CompletionWidget = CreateCompletionWidget (editor, context, docInfo)
			};
		}

		CSharpCompletionTextEditorExtension CreateCompletionAndUpdate (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context, UnderlyingDocumentInfo docInfo,
			out CodeCompletionContext codeCompletionContext)
		{
			var completion = CreateCompletion (editor, context, docInfo, out codeCompletionContext);
			completion.UpdateParsedDocument ();
			return completion;
		}

		public ICompletionWidget CreateCompletionWidget (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context,	UnderlyingDocumentInfo docInfo)
		{
			return new RazorCompletionWidget (editor, context, docInfo);
		}

		public Task<ICompletionDataList> HandlePopupCompletion (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context, UnderlyingDocumentInfo docInfo)
		{
			CodeCompletionContext ccc;
			var completion = CreateCompletionAndUpdate (editor, context, docInfo, out ccc);
			return completion.HandleCodeCompletionAsync (ccc, CompletionTriggerInfo.CodeCompletionCommand);
		}

		public Task<ICompletionDataList> HandleCompletion (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context,	CodeCompletionContext completionContext,
			UnderlyingDocumentInfo docInfo, char currentChar, CancellationToken token)
		{
			CodeCompletionContext ccc;
			var completion = CreateCompletionAndUpdate (editor, context, docInfo, out ccc);
			return completion.HandleCodeCompletionAsync (completionContext, new CompletionTriggerInfo (CompletionTriggerReason.CharTyped, currentChar), token);
		}

		public Task<ParameterHintingResult> HandleParameterCompletion (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context,	CodeCompletionContext completionContext,
			UnderlyingDocumentInfo docInfo, char completionChar)
		{
			CodeCompletionContext ccc;
			var completion = CreateCompletionAndUpdate (editor, context, docInfo, out ccc);
			return completion.HandleParameterCompletionAsync (completionContext, completionChar);
		}

//		public bool GetParameterCompletionCommandOffset (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context,	UnderlyingDocumentInfo docInfo, out int cpos)
//		{
//			CodeCompletionContext ccc;
//			var completion = CreateCompletionAndUpdate (editor, context, docInfo, out ccc);
//			return completion.GetParameterCompletionCommandOffset (out cpos);
//		}

		public Task<int> GetCurrentParameterIndex (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context, UnderlyingDocumentInfo docInfo, int startOffset)
		{
			CodeCompletionContext ccc;
			var completion = CreateCompletionAndUpdate (editor, context, docInfo, out ccc);
			return completion.GetCurrentParameterIndex (startOffset, default(CancellationToken));
		}
	}

	class RazorCompletionWidget : ICompletionWidget
	{
		DocumentContext realDocumentContext;

		MonoDevelop.Ide.Editor.TextEditor realEditor;

		UnderlyingDocumentInfo docInfo;

		public RazorCompletionWidget (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context, UnderlyingDocumentInfo docInfo)
		{
			this.realEditor = editor;
			this.realDocumentContext = context;
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

		protected virtual void OnCompletionContextChanged (EventArgs e)
		{
			var handler = CompletionContextChanged;
			if (handler != null)
				handler (this, e);
		}

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
			var savedCtx = realDocumentContext.GetContent<ICompletionWidget> ().CreateCodeCompletionContext (
				realEditor.CaretOffset + triggerOffset - docInfo.CaretPosition);
			var result = new CodeCompletionContext ();
			result.TriggerOffset = triggerOffset;
			var loc = docInfo.UnderlyingDocument.Editor.OffsetToLocation (triggerOffset);
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
			return docInfo.UnderlyingDocument.Editor.GetTextBetween (min, max);
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
			var loc = docInfo.UnderlyingDocument.Editor.OffsetToLocation (offset);
			translatedCtx.TriggerLine = loc.Line;
			translatedCtx.TriggerLineOffset = loc.Column - 1;
			translatedCtx.TriggerWordLength = ctx.TriggerWordLength;
				realDocumentContext.GetContent<ICompletionWidget> ().SetCompletionText (
				translatedCtx, partial_word, complete_word, wordOffset);
		}

		public int CaretOffset
		{
			get	{
				return docInfo.UnderlyingDocument.Editor.CaretOffset;
			}
			set {
				docInfo.UnderlyingDocument.Editor.CaretOffset = value;
			}
		}

		public int TextLength
		{
			get	{
				return docInfo.UnderlyingDocument.Editor.Length;
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

		public double ZoomLevel {
			get {
				return 1d;
			}
		}
		#endregion
	}
}
