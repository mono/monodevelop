//
// DefaultCommandTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor.TextMate;
using System.Threading;

namespace MonoDevelop.Ide.Editor.Extension
{
	class DefaultCommandTextEditorExtension : TextEditorExtension
	{
		#region Commands
		async void ToggleCodeCommentWithBlockComments ()
		{
			var scope = await Editor.SyntaxHighlighting.GetScopeStackAsync (Editor.CaretOffset, CancellationToken.None);
			var lang = TextMateLanguage.Create (scope);
			var lineComments = lang.LineComments.ToArray ();
			var blockStarts = lang.BlockComments.Select (b => b.Item1).ToList ();
			var blockEnds = lang.BlockComments.Select (b => b.Item2).ToList ();

			if (blockStarts.Count == 0 || blockEnds.Count == 0)
				return;

			string blockStart = blockStarts[0];
			string blockEnd = blockEnds[0];

			using (var undo = Editor.OpenUndoGroup ()) {
				IDocumentLine startLine;
				IDocumentLine endLine;

				if (Editor.IsSomethingSelected) {
					startLine = Editor.GetLineByOffset (Editor.SelectionRange.Offset);
					endLine = Editor.GetLineByOffset (Editor.SelectionRange.EndOffset);

					// If selection ends at begining of line... This is visible as previous line
					// is selected, hence we want to select previous line Bug 26287
					if (endLine.Offset == Editor.SelectionRange.EndOffset)
						endLine = endLine.PreviousLine;
				} else {
					startLine = endLine = Editor.GetLine (Editor.CaretLine);
				}
				string startLineText = Editor.GetTextAt (startLine.Offset, startLine.Length);
				string endLineText = Editor.GetTextAt (endLine.Offset, endLine.Length);
				if (startLineText.StartsWith (blockStart, StringComparison.Ordinal) && endLineText.EndsWith (blockEnd, StringComparison.Ordinal)) {
					Editor.RemoveText (endLine.Offset + endLine.Length - blockEnd.Length, blockEnd.Length);
					Editor.RemoveText (startLine.Offset, blockStart.Length);
					if (Editor.IsSomethingSelected) {
						Editor.SelectionAnchorOffset -= blockEnd.Length;
					}
				} else {
					Editor.InsertText (endLine.Offset + endLine.Length, blockEnd);
					Editor.InsertText (startLine.Offset, blockStart);
					if (Editor.IsSomethingSelected) {
						Editor.SelectionAnchorOffset += blockEnd.Length;
					}
				}
			}
		}

		bool TryGetLineCommentTag (out string commentTag)
		{
			var scope = Editor.SyntaxHighlighting.GetScopeStackAsync (Editor.CaretOffset, CancellationToken.None).WaitAndGetResult (CancellationToken.None);
			var lang = TextMateLanguage.Create (scope);

			if (lang.LineComments.Count == 0) {
				commentTag = null;
				return false;
			}
			commentTag = lang.LineComments [0];
			return true;
		}

		[CommandUpdateHandler (EditCommands.AddCodeComment)]
		[CommandUpdateHandler (EditCommands.RemoveCodeComment)]
		[CommandUpdateHandler (EditCommands.ToggleCodeComment)]
		void OnUpdateToggleComment (CommandInfo info)
		{
			var scope = Editor.SyntaxHighlighting.GetScopeStackAsync (Editor.CaretOffset, CancellationToken.None).WaitAndGetResult (CancellationToken.None);
			var lang = TextMateLanguage.Create (scope);
			info.Visible = lang.LineComments.Count + lang.BlockComments.Count > 0;
		}

		[CommandHandler (EditCommands.ToggleCodeComment)]
		internal void ToggleCodeComment ()
		{
			string commentTag;
			if (!TryGetLineCommentTag (out commentTag))
				return;
			bool comment = false;
			foreach (var line in GetSelectedLines (Editor)) {
				int startOffset;
				int offset = line.Offset;
				if (!StartsWith (Editor, offset, line.Length, commentTag, out startOffset)) {
					if (startOffset - offset == line.Length) // case: line consists only of white spaces
						continue;
					comment = true;
					break;
				}
			}

			if (comment) {
				AddCodeComment ();
			} else {
				RemoveCodeComment ();
			}
		}

		static bool StartsWith (ITextSource text, int offset, int length, string commentTag, out int startOffset)
		{
			int max = Math.Min (offset + length, text.Length);
			int i = offset;
			for (; i < max; i++) {
				char ch = text.GetCharAt (i);
				if (ch != ' ' && ch != '\t')
					break;
			}
			startOffset = i;
			for (int j = 0; j < commentTag.Length && i < text.Length; j++) {
				if (text.GetCharAt (i) != commentTag [j])
					return false;
				i++;
			}

			return true;
		}

		static IEnumerable<IDocumentLine> GetSelectedLines (TextEditor Editor)
		{
			if (!Editor.IsSomethingSelected) {
				yield return Editor.GetLine (Editor.CaretLine);
				yield break;
			}
			var selection = Editor.SelectionRegion;
			var line = Editor.GetLine(selection.EndLine);
			if (selection.EndColumn == 1)
				line = line.PreviousLine;

			while (line != null && line.LineNumber >= selection.BeginLine) {
				yield return line;
				line = line.PreviousLine;
			}
		}

		[CommandHandler (EditCommands.AddCodeComment)]
		internal void AddCodeComment ()
		{
			string commentTag;
			if (!TryGetLineCommentTag (out commentTag))
				return;

			using (var undo = Editor.OpenUndoGroup ()) {
				var wasSelected = Editor.IsSomethingSelected;
				var lead = Editor.SelectionLeadOffset;
				var anchor = Editor.SelectionAnchorOffset;
				string indent = null;
				var oldVersion = Editor.Version;
				var changes = new List<Microsoft.CodeAnalysis.Text.TextChange> ();

				foreach (var line in GetSelectedLines (Editor)) {
					var curIndent = line.GetIndentation (Editor);
					if (line.Length == curIndent.Length) {
						continue;
					}
					if (indent == null || curIndent.Length < indent.Length)
						indent = curIndent;
					changes.Add (new Microsoft.CodeAnalysis.Text.TextChange (new Microsoft.CodeAnalysis.Text.TextSpan (line.Offset + indent.Length, 0), commentTag));
				}
				Editor.ApplyTextChanges (changes);
				if (wasSelected) {
					Editor.SelectionAnchorOffset = oldVersion.MoveOffsetTo (Editor.Version, anchor);
					Editor.SelectionLeadOffset = oldVersion.MoveOffsetTo (Editor.Version, lead);
				}
			}
		}

		[CommandHandler (EditCommands.RemoveCodeComment)]
		internal void RemoveCodeComment ()
		{
			string commentTag;
			if (!TryGetLineCommentTag (out commentTag))
				return;

			using (var undo = Editor.OpenUndoGroup ()) {
				var wasSelected = Editor.IsSomethingSelected;
				var lead = Editor.SelectionLeadOffset;
				var anchor = Editor.SelectionAnchorOffset;
				int lines = 0;

				//IDocumentLine first = null;
				IDocumentLine last  = null;
				var oldVersion = Editor.Version;
				var changes = new List<Microsoft.CodeAnalysis.Text.TextChange> ();
				
				foreach (var line in GetSelectedLines (Editor)) {
					int startOffset;
					if (StartsWith (Editor, line.Offset, line.Length, commentTag, out startOffset)) {
						changes.Add (new Microsoft.CodeAnalysis.Text.TextChange (new Microsoft.CodeAnalysis.Text.TextSpan (startOffset, commentTag.Length), ""));
						lines++;
					}

					//first = line;
					if (last == null)
						last = line;
				}
				Editor.ApplyTextChanges (changes);
				
				if (wasSelected) {
					//					if (IdeApp.Workbench != null)
					//						CodeFormatterService.Format (Editor, IdeApp.Workbench.ActiveDocument, TextSegment.FromBounds (first.Offset, last.EndOffset));

					Editor.SelectionAnchorOffset = oldVersion.MoveOffsetTo (Editor.Version, anchor);
					Editor.SelectionLeadOffset = oldVersion.MoveOffsetTo (Editor.Version, lead);
				}
			}
		}

		[CommandHandler (EditCommands.InsertGuid)]
		void InsertGuid ()
		{
			Editor.InsertAtCaret (Guid.NewGuid ().ToString ());
		}

		[CommandUpdateHandler (MessageBubbleCommands.Toggle)]
		public void OnUpdateToggleErrorTextMarker (CommandInfo info)
		{
			var line = Editor.GetLine (Editor.CaretLine);
			if (line == null) {
				info.Visible = false;
				return;
			}

			var marker = (IMessageBubbleLineMarker)Editor.GetLineMarkers (line).FirstOrDefault (m => m is IMessageBubbleLineMarker);
			info.Visible = marker != null;

			if (info.Visible)
				info.Text = marker.IsVisible ? GettextCatalog.GetString ("_Hide Current Message") : GettextCatalog.GetString ("_Show Hidden Message");
		}

		[CommandHandler (MessageBubbleCommands.Toggle)]
		public void OnToggleErrorTextMarker ()
		{
			var line = Editor.GetLine (Editor.CaretLine);
			if (line == null)
				return;
			var marker = (IMessageBubbleLineMarker)Editor.GetLineMarkers (line).FirstOrDefault (m => m is IMessageBubbleLineMarker);
			if (marker != null) {
				marker.IsVisible = !marker.IsVisible;
			}
		}
		#endregion


		#region Key bindings

		[CommandHandler (TextEditorCommands.LineEnd)]
		void OnLineEnd ()
		{
			EditActions.MoveCaretToLineEnd (Editor);
		}

		[CommandHandler (TextEditorCommands.LineStart)]
		void OnLineStart ()
		{
			EditActions.MoveCaretToLineStart (Editor);
		}

		[CommandHandler (TextEditorCommands.DeleteLeftChar)]
		void OnDeleteLeftChar ()
		{
			EditActions.Backspace (Editor);
		}

		[CommandHandler (TextEditorCommands.DeleteRightChar)]
		void OnDeleteRightChar ()
		{
			EditActions.Delete (Editor);
		}

		[CommandHandler (TextEditorCommands.CharLeft)]
		void OnCharLeft ()
		{
			EditActions.MoveCaretLeft (Editor);
		}

		[CommandHandler (TextEditorCommands.CharRight)]
		void OnCharRight ()
		{
			EditActions.MoveCaretRight (Editor);
		}

		[CommandHandler (TextEditorCommands.LineUp)]
		void OnLineUp ()
		{
			EditActions.MoveCaretUp (Editor);
		}

		[CommandHandler (TextEditorCommands.LineDown)]
		void OnLineDown ()
		{
			EditActions.MoveCaretDown (Editor);
		}

		[CommandHandler (TextEditorCommands.DocumentStart)]
		void OnDocumentStart ()
		{
			EditActions.MoveCaretToDocumentStart (Editor);
		}

		[CommandHandler (TextEditorCommands.DocumentEnd)]
		void OnDocumentEnd ()
		{
			EditActions.MoveCaretToDocumentEnd (Editor);
		}

		[CommandHandler (TextEditorCommands.PageUp)]
		void OnPageUp ()
		{
			EditActions.PageUp (Editor);
		}

		[CommandHandler (TextEditorCommands.PageDown)]
		void OnPageDown ()
		{
			EditActions.PageDown (Editor);
		}

		[CommandHandler (TextEditorCommands.DeleteLine)]
		void OnDeleteLine ()
		{
			EditActions.DeleteCurrentLine (Editor);
		}

		[CommandHandler (TextEditorCommands.DeleteToLineEnd)]
		void OnDeleteToLineEnd ()
		{
			EditActions.DeleteCurrentLineToEnd (Editor);
		}

		[CommandHandler (TextEditorCommands.ScrollLineUp)]
		void OnScrollLineUp ()
		{
			EditActions.ScrollLineUp (Editor);
		}

		[CommandHandler (TextEditorCommands.ScrollLineDown)]
		void OnScrollLineDown ()
		{
			EditActions.ScrollLineDown (Editor);
		}

		[CommandHandler (TextEditorCommands.ScrollPageUp)]
		void OnScrollPageUp ()
		{
			EditActions.ScrollPageUp (Editor);
		}

		[CommandHandler (TextEditorCommands.ScrollPageDown)]
		void OnScrollPageDown ()
		{
			EditActions.ScrollPageDown (Editor);
		}

		[CommandHandler (TextEditorCommands.GotoMatchingBrace)]
		void OnGotoMatchingBrace ()
		{
			EditActions.GotoMatchingBrace (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMoveLeft)]
		void OnSelectionMoveLeft ()
		{
			EditActions.SelectionMoveLeft (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMoveRight)]
		void OnSelectionMoveRight ()
		{
			EditActions.SelectionMoveRight (Editor);
		}

		[CommandHandler (TextEditorCommands.MovePrevWord)]
		void OnMovePrevWord ()
		{
			EditActions.MovePrevWord (Editor);
		}

		[CommandHandler (TextEditorCommands.MoveNextWord)]
		void OnMoveNextWord ()
		{
			EditActions.MoveNextWord (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMovePrevWord)]
		void OnSelectionMovePrevWord ()
		{
			EditActions.SelectionMovePrevWord (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMoveNextWord)]
		void OnSelectionMoveNextWord ()
		{
			EditActions.SelectionMoveNextWord (Editor);
		}

		[CommandHandler (TextEditorCommands.MovePrevSubword)]
		void OnMovePrevSubword ()
		{
			EditActions.MovePrevSubWord (Editor);
		}

		[CommandHandler (TextEditorCommands.MoveNextSubword)]
		void OnMoveNextSubword ()
		{
			EditActions.MoveNextSubWord (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMovePrevSubword)]
		void OnSelectionMovePrevSubword ()
		{
			EditActions.SelectionMovePrevSubWord (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMoveNextSubword)]
		void OnSelectionMoveNextSubword ()
		{
			EditActions.SelectionMoveNextSubWord (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMoveUp)]
		void OnSelectionMoveUp ()
		{
			EditActions.SelectionMoveUp (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMoveDown)]
		void OnSelectionMoveDown ()
		{
			EditActions.SelectionMoveDown (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMoveHome)]
		void OnSelectionMoveHome ()
		{
			EditActions.SelectionMoveLineStart (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMoveEnd)]
		void OnSelectionMoveEnd ()
		{
			EditActions.SelectionMoveLineEnd (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMoveToDocumentStart)]
		void OnSelectionMoveToDocumentStart ()
		{
			EditActions.SelectionMoveToDocumentStart (Editor);
		}

		[CommandHandler (TextEditorCommands.ExpandSelectionToLine)]
		void OnExpandSelectionToLine ()
		{
			EditActions.ExpandSelectionToLine (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionMoveToDocumentEnd)]
		void OnSelectionMoveToDocumentEnd ()
		{
			EditActions.SelectionMoveToDocumentEnd (Editor);
		}

		[CommandHandler (TextEditorCommands.SwitchCaretMode)]
		void OnSwitchCaretMode ()
		{
			EditActions.SwitchCaretMode (Editor);
		}

		[CommandHandler (TextEditorCommands.InsertTab)]
		void OnInsertTab ()
		{
			EditActions.InsertTab (Editor);
		}

		[CommandHandler (TextEditorCommands.RemoveTab)]
		void OnRemoveTab ()
		{
			EditActions.RemoveTab (Editor);
		}

		[CommandHandler (TextEditorCommands.InsertNewLine)]
		void OnInsertNewLine ()
		{
			EditActions.InsertNewLine (Editor);
		}

		[CommandHandler (TextEditorCommands.InsertNewLineAtEnd)]
		void OnInsertNewLineAtEnd ()
		{
			EditActions.InsertNewLineAtEnd (Editor);
		}

		[CommandHandler (TextEditorCommands.InsertNewLinePreserveCaretPosition)]
		void OnInsertNewLinePreserveCaretPosition ()
		{
			EditActions.InsertNewLinePreserveCaretPosition (Editor);
		}

		[CommandHandler (TextEditorCommands.CompleteStatement)]
		void OnCompleteStatement ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var generator = CodeGenerator.CreateGenerator (doc);
			if (generator != null) {
				generator.CompleteStatement (doc);
			}
		}

		[CommandHandler (TextEditorCommands.DeletePrevWord)]
		void OnDeletePrevWord ()
		{
			EditActions.DeletePreviousWord (Editor);
		}

		[CommandHandler (TextEditorCommands.DeleteNextWord)]
		void OnDeleteNextWord ()
		{
			EditActions.DeleteNextWord (Editor);
		}

		[CommandHandler (TextEditorCommands.DeletePrevSubword)]
		void OnDeletePrevSubword ()
		{
			EditActions.DeletePreviousSubword (Editor);
		}

		[CommandHandler (TextEditorCommands.DeleteNextSubword)]
		void OnDeleteNextSubword ()
		{
			EditActions.DeleteNextSubword (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionPageDownAction)]
		void OnSelectionPageDownAction ()
		{
			EditActions.SelectionPageDown (Editor);
		}

		[CommandHandler (TextEditorCommands.SelectionPageUpAction)]
		void OnSelectionPageUpAction ()
		{
			EditActions.SelectionPageUp (Editor);
		}

		[CommandHandler (TextEditorCommands.PulseCaret)]
		void OnPulseCaretCommand ()
		{
			EditActions.StartCaretPulseAnimation (Editor);
		}

		[CommandHandler (TextEditorCommands.TransposeCharacters)]
		void TransposeCharacters ()
		{
			EditActions.TransposeCharacters (Editor);
		}

		[CommandHandler (TextEditorCommands.DuplicateLine)]
		void DuplicateLine ()
		{	
			EditActions.DuplicateCurrentLine (Editor);
		}

		[CommandHandler (TextEditorCommands.RecenterEditor)]
		void RecenterEditor ()
		{
			EditActions.RecenterEditor (Editor);
		}

		[CommandHandler (EditCommands.JoinWithNextLine)]
		void JoinLines ()
		{
			EditActions.JoinLines (Editor);
		}

		[CommandHandler (TextEditorCommands.MoveBlockUp)]
		void OnMoveBlockUp ()
		{
			EditActions.MoveBlockUp (Editor);
		}

		[CommandHandler (TextEditorCommands.MoveBlockDown)]
		void OnMoveBlockDown ()
		{
			EditActions.MoveBlockDown (Editor);
		}

		[CommandHandler (TextEditorCommands.ToggleBlockSelectionMode)]
		void OnToggleBlockSelectionMode ()
		{
			EditActions.ToggleBlockSelectionMode (Editor);
		}

		[CommandHandler (EditCommands.IndentSelection)]
		void IndentSelection ()
		{
			EditActions.IndentSelection (Editor);
		}

		[CommandHandler (EditCommands.UnIndentSelection)]
		void UnIndentSelection ()
		{
			EditActions.UnIndentSelection (Editor);
		}


		[CommandHandler (EditCommands.SortSelectedLines)]
		void SortSelectedLines ()
		{
			EditActions.SortSelectedLines (Editor);
		}

		[CommandUpdateHandler (EditCommands.SortSelectedLines)]
		void UpdateSortSelectedLines (CommandInfo ci)
		{
			var region = Editor.SelectionRegion;
			ci.Enabled = region.BeginLine != region.EndLine;
		}
		#endregion	
	}
}