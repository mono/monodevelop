// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Text;

using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor.Actions
{
	public class Tab : AbstractEditAction
	{
		public static string GetIndentationString(IDocument document)
		{
			return document.IndentString;
		}
		
		void InsertTabs(IDocument document, ISelection selection, int y1, int y2)
		{
			int    redocounter = 0;
			string indentationString = GetIndentationString(document);
			for (int i = y2; i >= y1; --i) {
				LineSegment line = document.GetLineSegment(i);
				if (i == y2 && i == selection.EndPosition.Y && selection.EndPosition.X  == 0) {
					continue;
				}
				
				// this bit is optional - but useful if you are using block tabbing to sort out
				// a source file with a mixture of tabs and spaces
//				string newLine = document.GetText(line.Offset,line.Length);
//				document.Replace(line.Offset,line.Length,newLine);
//				++redocounter;
				
				document.Insert(line.Offset, indentationString);
				++redocounter;
			}
			
			if (redocounter > 0) {
				document.UndoStack.UndoLast(redocounter); // redo the whole operation (not the single deletes)
			}
		}
		
		void InsertTabAtCaretPosition(TextArea textArea)
		{
			switch (textArea.Caret.CaretMode) {
				case CaretMode.InsertMode:
					textArea.InsertString(GetIndentationString(textArea.Document));
					break;
				case CaretMode.OverwriteMode:
					string indentStr = GetIndentationString(textArea.Document);
					textArea.ReplaceChar(indentStr[0]);
					if (indentStr.Length > 1) {
						textArea.InsertString(indentStr.Substring(1));
					}
					break;
			}
			textArea.SetDesiredColumn();
		}
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			if (textArea.Document.ReadOnly) {
				return;
			}
			if (textArea.SelectionManager.HasSomethingSelected) {
				foreach (ISelection selection in textArea.SelectionManager.SelectionCollection) {
					int startLine = selection.StartPosition.Y;
					int endLine   = selection.EndPosition.Y;
					if (startLine != endLine) {
						textArea.BeginUpdate();
						InsertTabs(textArea.Document, selection, startLine, endLine);
						textArea.Document.UpdateQueue.Clear();
						textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, startLine, endLine));
						textArea.EndUpdate();
					} else {
						InsertTabAtCaretPosition(textArea);
						break;
					}
				}
				textArea.Document.CommitUpdate();
				textArea.AutoClearSelection = false;
			} else {
				InsertTabAtCaretPosition(textArea);
			}
		}
	}
	
	public class ShiftTab : AbstractEditAction
	{
		void RemoveTabs(IDocument document, ISelection selection, int y1, int y2) 
		{
			int  redocounter = 0;
			for (int i = y2; i >= y1; --i) {
				LineSegment line = document.GetLineSegment(i);
				if (i == y2 && line.Offset == selection.EndOffset) {
					continue;
				}
				if (line.Length > 0) {
					/**** TextPad Strategy:
					/// first convert leading whitespace to tabs (controversial! - not all editors work like this)
					string newLine = TextUtilities.LeadingWhiteSpaceToTabs(document.GetText(line.Offset,line.Length),document.Properties.GetProperty("TabIndent", 4));
					if(newLine.Length > 0 && newLine[0] == '\t') {
						document.Replace(line.Offset,line.Length,newLine.Substring(1));
						++redocounter;
					}
					else if(newLine.Length > 0 && newLine[0] == ' ') { 
						/// there were just some leading spaces but less than TabIndent of them
						int leadingSpaces = 1;
						for(leadingSpaces = 1; leadingSpaces < newLine.Length && newLine[leadingSpaces] == ' '; leadingSpaces++) {
							/// deliberately empty
						}
						document.Replace(line.Offset,line.Length,newLine.Substring(leadingSpaces));
						++redocounter;
					}
					/// else
					/// there were no leading tabs or spaces on this line so do nothing
					/// MS Visual Studio 6 strategy:
					****/
//					string temp = document.GetText(line.Offset,line.Length);
					if (line.Length > 0) {
						int charactersToRemove = 0;
						if(document.GetCharAt(line.Offset) == '\t') { // first character is a tab - just remove it
							charactersToRemove = 1;
						} else if(document.GetCharAt(line.Offset) == ' ') {
							int leadingSpaces = 1;
							int tabIndent = document.TextEditorProperties.TabIndent;
							for (leadingSpaces = 1; leadingSpaces < line.Length && document.GetCharAt(line.Offset + leadingSpaces) == ' '; leadingSpaces++) {
								// deliberately empty
							}
							if(leadingSpaces >= tabIndent) {
								// just remove tabIndent
								charactersToRemove = tabIndent;
							}
							else if(line.Length > leadingSpaces && document.GetCharAt(line.Offset + leadingSpaces) == '\t') {
								// remove the leading spaces and the following tab as they add up
								// to just one tab stop
								charactersToRemove = leadingSpaces+1;
							}
							else {
								// just remove the leading spaces
								charactersToRemove = leadingSpaces;
							}
						}
						if (charactersToRemove > 0) {
							document.Remove(line.Offset,charactersToRemove);
							++redocounter;
						}
					}
				}
			}
			
			if (redocounter > 0) {
				document.UndoStack.UndoLast(redocounter); // redo the whole operation (not the single deletes)
			}
		}
		
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			if (textArea.SelectionManager.HasSomethingSelected) {
				foreach (ISelection selection in textArea.SelectionManager.SelectionCollection) {
					int startLine = selection.StartPosition.Y;
					int endLine   = selection.EndPosition.Y;
					textArea.BeginUpdate();
					RemoveTabs(textArea.Document, selection, startLine, endLine);
					textArea.Document.UpdateQueue.Clear();
					textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, startLine, endLine));
					textArea.EndUpdate();
				
				}
				textArea.AutoClearSelection = false;
			} else {
				// Pressing Shift-Tab with nothing selected the cursor will move back to the 
				// previous tab stop. It will stop at the beginning of the line. Also, the desired
				// column is updated to that column.
				LineSegment line = textArea.Document.GetLineSegmentForOffset(textArea.Caret.Offset);
				string startOfLine = textArea.Document.GetText(line.Offset,textArea.Caret.Offset - line.Offset);
				int tabIndent = textArea.Document.TextEditorProperties.TabIndent;
				int currentColumn = textArea.Caret.Column;
				int remainder = currentColumn % tabIndent;
				if (remainder == 0) {
					textArea.Caret.DesiredColumn = Math.Max(0, currentColumn - tabIndent);
				} else {
					textArea.Caret.DesiredColumn = Math.Max(0, currentColumn - remainder);
				}
				textArea.SetCaretToDesiredColumn(textArea.Caret.Line);
			}
		}		
	}
	
	public class ToggleComment : AbstractEditAction
	{
		int firstLine;
		int lastLine;
		
		void RemoveCommentAt(IDocument document, string comment, ISelection selection, int y1, int y2)
		{
			int  redocounter = 0;
			firstLine = y1;
			lastLine  = y2;
			
			for (int i = y2; i >= y1; --i) {
				LineSegment line = document.GetLineSegment(i);
				if (selection != null && i == y2 && line.Offset == selection.Offset + selection.Length) {
					--lastLine;
					continue;
				}
				
				string lineText = document.GetText(line.Offset, line.Length);
				if (lineText.Trim().StartsWith(comment)) {
					document.Remove(line.Offset + lineText.IndexOf(comment), comment.Length);
					++redocounter;
				} 
			}
			
			if (redocounter > 0) {
				document.UndoStack.UndoLast(redocounter); // redo the whole operation (not the single deletes)
			}
		}
		
		void SetCommentAt(IDocument document, string comment, ISelection selection, int y1, int y2)
		{
			int  redocounter = 0;
			firstLine = y1;
			lastLine  = y2;
			
			for (int i = y2; i >= y1; --i) {
				LineSegment line = document.GetLineSegment(i);
				if (selection != null && i == y2 && line.Offset == selection.Offset + selection.Length) {
					--lastLine;
					continue;
				}
				
				string lineText = document.GetText(line.Offset, line.Length);
				document.Insert(line.Offset, comment);
				++redocounter;
			}
			
			if (redocounter > 0) {
				document.UndoStack.UndoLast(redocounter); // redo the whole operation (not the single deletes)
			}
		}
		
		bool ShouldComment(IDocument document, string comment, ISelection selection, int startLine, int endLine)
		{
			for (int i = endLine; i >= startLine; --i) {
				LineSegment line = document.GetLineSegment(i);
				if (selection != null && i == endLine && line.Offset == selection.Offset + selection.Length) {
					--lastLine;
					continue;
				}
				string lineText = document.GetText(line.Offset, line.Length);
				if (!lineText.Trim().StartsWith(comment)) {
					return true;
				}
			}
			return false;
		}
		
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			if (textArea.Document.ReadOnly) {
				return;
			}
			
			string comment = null;
			if (textArea.Document.HighlightingStrategy.Properties["LineComment"] != null) {
				comment = textArea.Document.HighlightingStrategy.Properties["LineComment"].ToString();
			}
			
			if (comment == null || comment.Length == 0) {
				return;
			}
			
			if (textArea.SelectionManager.HasSomethingSelected) {
				bool shouldComment = true;
				foreach (ISelection selection in textArea.SelectionManager.SelectionCollection) {
					if (!ShouldComment(textArea.Document, comment, selection, selection.StartPosition.Y, selection.EndPosition.Y)) {
						shouldComment = false;
						break;
					}
				}
				
				foreach (ISelection selection in textArea.SelectionManager.SelectionCollection) {
					textArea.BeginUpdate();
					if (shouldComment) {
						SetCommentAt(textArea.Document, comment, selection, selection.StartPosition.Y, selection.EndPosition.Y);
					} else {
						RemoveCommentAt(textArea.Document, comment, selection, selection.StartPosition.Y, selection.EndPosition.Y);
					}
					textArea.Document.UpdateQueue.Clear();
					textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, firstLine, lastLine));
					textArea.EndUpdate();
				}
				textArea.Document.CommitUpdate();
				textArea.AutoClearSelection = false;
			} else {
				textArea.BeginUpdate();
				int caretLine = textArea.Caret.Line;
				if (ShouldComment(textArea.Document, comment, null, caretLine, caretLine)) {
					SetCommentAt(textArea.Document, comment, null, caretLine, caretLine);
				} else {
					RemoveCommentAt(textArea.Document, comment, null, caretLine, caretLine);
				}
				textArea.Document.UpdateQueue.Clear();
				textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, caretLine));
				textArea.EndUpdate();
			}
		}
	}
	
	public class IndentSelection : AbstractEditAction
	{
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			IDocument d = textArea.Document;
			
			if (d.ReadOnly)
				return;
			
			textArea.BeginUpdate();
			if (textArea.SelectionManager.HasSomethingSelected) {
				foreach (ISelection selection in textArea.SelectionManager.SelectionCollection)
					d.FormattingStrategy.IndentLines (d, selection.StartPosition.Y, selection.EndPosition.Y);
			} else
				d.FormattingStrategy.IndentLines (d, 0, textArea.Document.TotalNumberOfLines - 1);
			
			textArea.EndUpdate();
			//textArea.Refresh();
		}
	}
	
	public class Backspace : AbstractEditAction
	{
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			if (textArea.Document.ReadOnly) {
				return;
			}
			if (textArea.SelectionManager.HasSomethingSelected) {
				textArea.BeginUpdate();
				textArea.Caret.Position = textArea.SelectionManager.SelectionCollection[0].StartPosition;
				textArea.SelectionManager.RemoveSelectedText();
				textArea.ScrollToCaret();
				textArea.EndUpdate();
			} else {
				if (textArea.Caret.Offset > 0) {
					textArea.BeginUpdate();
					int curLineNr     = textArea.Document.GetLineNumberForOffset(textArea.Caret.Offset);
					int curLineOffset = textArea.Document.GetLineSegment(curLineNr).Offset;
						
					if (curLineOffset == textArea.Caret.Offset) {
						LineSegment line = textArea.Document.GetLineSegment(curLineNr - 1);
						bool lastLine = curLineNr == textArea.Document.TotalNumberOfLines;
						int lineEndOffset = line.Offset + line.Length;
						int lineLength = line.Length;
						textArea.Document.Remove(lineEndOffset, curLineOffset - lineEndOffset);
						textArea.Caret.Position = new Point(lineLength, curLineNr - 1);
						textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToEnd, new Point(0, curLineNr - 1)));
						textArea.EndUpdate();
					} else {
						int caretOffset = textArea.Caret.Offset - 1;
						textArea.Caret.Position = textArea.Document.OffsetToPosition(caretOffset);
						textArea.Document.Remove(caretOffset, 1);
						
//						textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToLineEnd, new Point(textArea.Caret.Offset - textArea.Document.GetLineSegment(curLineNr).Offset, curLineNr)));
						textArea.EndUpdate();
					}
				}
			}
		}
	}
	
	public class Delete : AbstractEditAction
	{
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			if (textArea.Document.ReadOnly) {
				return;
			}
			if (textArea.SelectionManager.HasSomethingSelected) {
				textArea.BeginUpdate();
				textArea.Caret.Position = textArea.SelectionManager.SelectionCollection[0].StartPosition;
				textArea.SelectionManager.RemoveSelectedText();
				textArea.ScrollToCaret();
				textArea.EndUpdate();
			} else {
			
				if (textArea.Caret.Offset < textArea.Document.TextLength) {
					textArea.BeginUpdate();
					int curLineNr   = textArea.Document.GetLineNumberForOffset(textArea.Caret.Offset);
					LineSegment curLine = textArea.Document.GetLineSegment(curLineNr);
					
					if (curLine.Offset + curLine.Length == textArea.Caret.Offset) {
						if (curLineNr + 1 < textArea.Document.TotalNumberOfLines) {
							LineSegment nextLine = textArea.Document.GetLineSegment(curLineNr + 1);
							
							textArea.Document.Remove(textArea.Caret.Offset, nextLine.Offset - textArea.Caret.Offset);
							textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToEnd, new Point(0, curLineNr)));
						}
					} else {
						textArea.Document.Remove(textArea.Caret.Offset, 1);
//						textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToLineEnd, new Point(textArea.Caret.Offset - textArea.Document.GetLineSegment(curLineNr).Offset, curLineNr)));
					}
					textArea.UpdateMatchingBracket();
					textArea.EndUpdate();
				}
			}
		}
	}
	
	public class MovePageDown : AbstractEditAction
	{
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			int curLineNr           = textArea.Caret.Line;
			int requestedLineNumber = Math.Min(textArea.Document.GetNextVisibleLineAbove(curLineNr, textArea.TextView.VisibleLineCount), textArea.Document.TotalNumberOfLines - 1);
			
			if (curLineNr != requestedLineNumber) {
				textArea.Caret.Position = new Point(textArea.Caret.DesiredColumn, requestedLineNumber);
			}
		} 
	}
	
	public class MovePageUp : AbstractEditAction
	{
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			int curLineNr           = textArea.Caret.Line;
			int requestedLineNumber = Math.Max(textArea.Document.GetNextVisibleLineBelow(curLineNr, textArea.TextView.VisibleLineCount), 0);
			
			if (curLineNr != requestedLineNumber) {
				textArea.Caret.Position = new Point(textArea.Caret.DesiredColumn, requestedLineNumber);
			}
		}
	}
	
	public class Return : AbstractEditAction
	{
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			IDocument d = textArea.Document;
			
			if (d.ReadOnly)
				return;
			
			textArea.BeginUpdate();
			textArea.InsertChar('\n');
			
			++textArea.Caret.Line;
			int curLineNr = textArea.Caret.Line;
			textArea.Caret.Column = d.FormattingStrategy.FormatLine (d, curLineNr, textArea.Caret.Offset, '\n');
			textArea.SetDesiredColumn();
			
			textArea.Document.UpdateQueue.Clear();
			textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToEnd, new Point(0, curLineNr - 1)));
			textArea.EndUpdate();
		}
	}
	
	public class ToggleEditMode : AbstractEditAction
	{
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			if (textArea.Document.ReadOnly) {
				return;
			}
			switch (textArea.Caret.CaretMode) {
				case CaretMode.InsertMode:
					textArea.Caret.CaretMode = CaretMode.OverwriteMode;
					break;
				case CaretMode.OverwriteMode:
					textArea.Caret.CaretMode = CaretMode.InsertMode;
					break;
			}
		}
	}
	
	public class Undo : AbstractEditAction
	{
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			textArea.MotherTextEditorControl.Undo();
		}
	}
	
	public class Redo : AbstractEditAction
	{
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			textArea.MotherTextEditorControl.Redo();
		}
	}
	
	/// <summary>
	/// handles the ctrl-backspace key
	/// functionality attempts to roughly mimic MS Developer studio
	/// I will implement this as deleting back to the point that ctrl-leftarrow would
	/// take you to
	/// </summary>
	public class WordBackspace : AbstractEditAction 
	{
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			// if anything is selected we will just delete it first
			textArea.BeginUpdate();
			if (textArea.SelectionManager.HasSomethingSelected) {
				textArea.SelectionManager.RemoveSelectedText();
				textArea.ScrollToCaret();
			}
			// now delete from the caret to the beginning of the word
			LineSegment line =
			textArea.Document.GetLineSegmentForOffset(textArea.Caret.Offset);
			// if we are not at the beginning of a line
			if(textArea.Caret.Offset > line.Offset) {
				int prevWordStart = TextUtilities.FindPrevWordStart(textArea.Document,
				                                                    textArea.Caret.Offset);
				if(prevWordStart < textArea.Caret.Offset) {
					textArea.Document.Remove(prevWordStart,textArea.Caret.Offset -
					                         prevWordStart);
					textArea.Caret.Position = textArea.Document.OffsetToPosition(prevWordStart);
				}
			}
			// if we are now at the beginning of a line
			if(textArea.Caret.Offset == line.Offset) {
				// if we are not on the first line
				int curLineNr =
				textArea.Document.GetLineNumberForOffset(textArea.Caret.Offset);
				if(curLineNr > 0) {
					// move to the end of the line above
					LineSegment lineAbove = textArea.Document.GetLineSegment(curLineNr -
					                                                         1);
					int endOfLineAbove = lineAbove.Offset + lineAbove.Length;
					int charsToDelete = textArea.Caret.Offset - endOfLineAbove;
					textArea.Document.Remove(endOfLineAbove,charsToDelete);
					textArea.Caret.Position = textArea.Document.OffsetToPosition(endOfLineAbove);
				}
			}
			textArea.SetDesiredColumn();
			textArea.EndUpdate();
			// if there are now less lines, we need this or there are redraw problems
			textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToEnd, new Point(0, textArea.Document.GetLineNumberForOffset(textArea.Caret.Offset))));
			textArea.Document.CommitUpdate();
		}
	}
	
	/// <summary>
	/// handles the ctrl-delete key
	/// functionality attempts to mimic MS Developer studio
	/// I will implement this as deleting forwardto the point that 
	/// ctrl-leftarrow would take you to
	/// </summary>
	public class DeleteWord : Delete 
	{
		/// <remarks>
		/// Executes this edit action
		/// </remarks>
		/// <param name="textArea">The <see cref="ItextArea"/> which is used for callback purposes</param>
		public override void Execute(TextArea textArea)
		{
			// if anything is selected we will just delete it first
			textArea.BeginUpdate();
			if (textArea.SelectionManager.HasSomethingSelected) {
				textArea.SelectionManager.RemoveSelectedText();
				textArea.ScrollToCaret();
			}
			// now delete from the caret to the beginning of the word
			LineSegment line =
			textArea.Document.GetLineSegmentForOffset(textArea.Caret.Offset);
			if(textArea.Caret.Offset == line.Offset + line.Length) {
				// if we are at the end of a line
				base.Execute(textArea);
			} else {
				int nextWordStart = TextUtilities.FindNextWordStart(textArea.Document,
				                                                    textArea.Caret.Offset);
				if(nextWordStart > textArea.Caret.Offset) {
					textArea.Document.Remove(textArea.Caret.Offset,nextWordStart -
					                         textArea.Caret.Offset);
					// cursor never moves with this command
				}
			}
			textArea.UpdateMatchingBracket();
			textArea.EndUpdate();
			// if there are now less lines, we need this or there are redraw problems
			textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToEnd, new Point(0, textArea.Document.GetLineNumberForOffset(textArea.Caret.Offset))));
			textArea.Document.CommitUpdate();
		}
	}
	
	public class DeleteLine : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			int lineNr = textArea.Caret.Line;
			LineSegment line = textArea.Document.GetLineSegment(lineNr);
			textArea.Document.Remove(line.Offset, line.TotalLength);
			textArea.Caret.Position = textArea.Document.OffsetToPosition(line.Offset);

			textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToEnd, new Point(0, lineNr)));
			textArea.UpdateMatchingBracket();
			textArea.Document.CommitUpdate();
		}
	}
	
	public class DeleteToLineEnd : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			int lineNr = textArea.Caret.Line;
			LineSegment line = textArea.Document.GetLineSegment(lineNr);
			
			int numRemove = (line.Offset + line.Length) - textArea.Caret.Offset;
			if (numRemove > 0) {
				textArea.Document.Remove(textArea.Caret.Offset, numRemove);
				textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, new Point(0, lineNr)));
				textArea.Document.CommitUpdate();
			}
		}
	}
	
	public class GotoMatchingBrace : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			if (textArea.TextView.Highlight != null) {
				Point p1 = new Point(textArea.TextView.Highlight.CloseBrace.X + 1, textArea.TextView.Highlight.CloseBrace.Y);
				Point p2 = new Point(textArea.TextView.Highlight.OpenBrace.X + 1, textArea.TextView.Highlight.OpenBrace.Y);
				if (p1 == textArea.Caret.Position) {
					textArea.Caret.Position = p2;
				} else {
					textArea.Caret.Position = p1;
				}
				textArea.SetDesiredColumn();
			}
		}
	}
}
