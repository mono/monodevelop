//  SelectionActions.cs
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
	public class ShiftCaretRight : CaretRight
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class ShiftCaretLeft : CaretLeft
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class ShiftCaretUp : CaretUp
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class ShiftCaretDown : CaretDown
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class ShiftWordRight : WordRight
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class ShiftWordLeft : WordLeft
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class ShiftHome : Home
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class ShiftEnd : End
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class ShiftMoveToStart : MoveToStart
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class ShiftMoveToEnd : MoveToEnd
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class ShiftMovePageUp : MovePageUp
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class ShiftMovePageDown : MovePageDown
	{
		public override void Execute(TextArea textArea)
		{
			Point oldCaretPos  = textArea.Caret.Position;
			base.Execute(textArea);
			textArea.AutoClearSelection = false;
			textArea.SelectionManager.ExtendSelection(oldCaretPos, textArea.Caret.Position);
		}
	}
	
	public class SelectWholeDocument : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			textArea.AutoClearSelection = false;
			Point startPoint = new Point(0, 0);
			Point endPoint   = textArea.Document.OffsetToPosition(textArea.Document.TextLength);
			if (textArea.SelectionManager.HasSomethingSelected) {
				if (textArea.SelectionManager.SelectionCollection[0].StartPosition == startPoint &&
				    textArea.SelectionManager.SelectionCollection[0].EndPosition   == endPoint) {
					return;
				}
			}
			textArea.SelectionManager.SetSelection(new DefaultSelection(textArea.Document, startPoint, endPoint));
		}
	}
	
	public class ClearAllSelections : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			textArea.SelectionManager.ClearSelection();
		}
	}
}
