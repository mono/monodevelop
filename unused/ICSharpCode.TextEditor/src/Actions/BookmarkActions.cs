//  BookmarkActions.cs
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
	public class ToggleBookmark : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			textArea.Document.BookmarkManager.ToggleMarkAt(textArea.Caret.Line);
			textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, textArea.Caret.Line));
			textArea.Document.CommitUpdate();
		}
	}
	
	public class GotoPrevBookmark : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			int lineNumber = textArea.Document.BookmarkManager.GetPrevMark(textArea.Caret.Line);
			if (lineNumber >= 0 && lineNumber < textArea.Document.TotalNumberOfLines) {
				textArea.Caret.Line = lineNumber;
			}
		}
	}
	
	public class GotoNextBookmark : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			int lineNumber = textArea.Document.BookmarkManager.GetNextMark(textArea.Caret.Line);
			if (lineNumber >= 0 && lineNumber < textArea.Document.TotalNumberOfLines) {
				textArea.Caret.Line = lineNumber;
			}
		}
	}
	
	public class ClearAllBookmarks : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			textArea.Document.BookmarkManager.Clear();
			textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
			textArea.Document.CommitUpdate();
		}
	}
}
