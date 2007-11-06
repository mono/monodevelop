//  HomeEndActions.cs
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

using System;
using System.Drawing;

using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor.Actions 
{
	public class Home : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			LineSegment curLine = textArea.Document.GetLineSegment(textArea.Caret.Line);
			
			if (TextUtilities.IsEmptyLine(textArea.Document, curLine)) {
				if (textArea.Caret.Column != 0) {
					textArea.Caret.Column = 0;
				} else  {
					textArea.Caret.Column = curLine.Length;
				}
			} else {
				int firstCharOffset = TextUtilities.GetFirstNonWSChar(textArea.Document, curLine.Offset);
				int firstCharColumn = firstCharOffset - curLine.Offset;
				
				if (textArea.Caret.Column == firstCharColumn) {
					textArea.Caret.Column  = 0;
				} else {
					textArea.Caret.Column  = firstCharColumn;
				}
			}
			
			textArea.SetDesiredColumn();
		}
	}
	
	public class End : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			LineSegment curLine = textArea.Document.GetLineSegment(textArea.Caret.Line);
			if (textArea.Caret.Column != curLine.Length) {
				textArea.Caret.Column = curLine.Length;
				textArea.SetDesiredColumn();
			}
		}
	}
	
	
	public class MoveToStart : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			if (textArea.Caret.Line != 0 || textArea.Caret.Column != 0) {
				textArea.Caret.Position = new Point(0, 0);
				textArea.SetDesiredColumn();
			}
		}
	}
	
	
	public class MoveToEnd : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			Point endPos = textArea.Document.OffsetToPosition(textArea.Document.TextLength);
			if (textArea.Caret.Position != endPos) {
				textArea.Caret.Position = endPos;
				textArea.SetDesiredColumn();
			}
		}
	}
}
