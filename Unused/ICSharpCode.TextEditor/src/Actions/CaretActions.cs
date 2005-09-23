// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System.Drawing;
using System;

using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor.Actions 
{
	public class CaretLeft : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			if (textArea.Caret.Column > 0) {
				--textArea.Caret.Column;
			} else if (textArea.Caret.Line  > 0) {
				LineSegment lineAbove = textArea.Document.GetLineSegment(textArea.Document.GetNextVisibleLineBelow(textArea.Caret.Line, 1));
				textArea.Caret.Position = new Point(lineAbove.Length, textArea.Caret.Line - 1);
			}
			textArea.SetDesiredColumn();
		}
	}
	
	public class CaretRight : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			LineSegment curLine = textArea.Document.GetLineSegment(textArea.Caret.Line);
			if (textArea.Caret.Column < curLine.Length || textArea.TextEditorProperties.AllowCaretBeyondEOL) {
				++textArea.Caret.Column;
			} else if (textArea.Caret.Line + 1 < textArea.Document.TotalNumberOfLines) {
				textArea.Caret.Position = new Point(0, textArea.Caret.Line + 1);
			}
			textArea.SetDesiredColumn();
		}
	}
	
	public class CaretUp : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			if (textArea.Caret.Line  > 0) {
				textArea.SetCaretToDesiredColumn(textArea.Caret.Line - 1);
			}
		}
	}
	
	public class CaretDown : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			if (textArea.Caret.Line + 1 < textArea.Document.TotalNumberOfLines) {
				textArea.SetCaretToDesiredColumn(textArea.Caret.Line + 1);
			}
		}
	}
	
	public class WordRight : CaretRight
	{
		public override void Execute(TextArea textArea)
		{
			LineSegment line   = textArea.Document.GetLineSegment(textArea.Caret.Position.Y);
			if (textArea.Caret.Column >= line.Length) {
				textArea.Caret.Position = new Point(0, textArea.Caret.Line + 1);
			} else {
				
				int nextWordStart = TextUtilities.FindNextWordStart(textArea.Document, textArea.Caret.Offset);
				textArea.Caret.Position = textArea.Document.OffsetToPosition(nextWordStart);
				textArea.SetDesiredColumn();
			}
			
		}
	}
	
	public class WordLeft : CaretLeft
	{
		public override void Execute(TextArea textArea)
		{
			if (textArea.Caret.Column == 0) {
				base.Execute(textArea);
			} else {
				LineSegment line   = textArea.Document.GetLineSegment(textArea.Caret.Position.Y);
				int prevWordStart = TextUtilities.FindPrevWordStart(textArea.Document, textArea.Caret.Offset);
				textArea.Caret.Position = textArea.Document.OffsetToPosition(prevWordStart);
				textArea.SetDesiredColumn();

			}
		}
	}
	
	public class ScrollLineUp : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			textArea.AutoClearSelection = false;
			textArea.TextView.FirstVisibleLine = Math.Max(0, textArea.TextView.FirstVisibleLine - 1);
		}
	}
	
	public class ScrollLineDown : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			textArea.AutoClearSelection = false;
			textArea.TextView.FirstVisibleLine = Math.Max(0, Math.Min(textArea.Document.TotalNumberOfLines - 3, textArea.TextView.FirstVisibleLine + 1));
		}
	}
}
