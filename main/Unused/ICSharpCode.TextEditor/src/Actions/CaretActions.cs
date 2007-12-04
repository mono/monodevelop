//  CaretActions.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
