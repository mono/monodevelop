//  UndoableReplace.cs
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
using System.Diagnostics;
using System.Drawing;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.TextEditor.Undo;

namespace MonoDevelop.TextEditor.Undo
{
	/// <summary>
	/// This class is for the undo of Document insert operations
	/// </summary>
	public class UndoableReplace : IUndoableOperation
	{
		IDocument document;
//		int       oldCaretPos;
		int       offset;
		string    text;
		string    origText;
		
		/// <summary>
		/// Creates a new instance of <see cref="UndoableReplace"/>
		/// </summary>	
		public UndoableReplace(IDocument document, int offset, string origText, string text)
		{
			if (document == null) {
				throw new ArgumentNullException("document");
			}
			if (offset < 0 || offset > document.TextLength) {
				throw new ArgumentOutOfRangeException("offset");
			}
			
			Debug.Assert(text != null, "text can't be null");
//			oldCaretPos   = document.Caret.Offset;
			this.document = document;
			this.offset   = offset;
			this.text     = text;
			this.origText = origText;
		}
		
		/// <remarks>
		/// Undo last operation
		/// </remarks>
		public void Undo()
		{
			// we clear all selection direct, because the redraw
			// is done per refresh at the end of the action
//			document.SelectionCollection.Clear();

			document.UndoStack.AcceptChanges = false;
			document.Replace(offset, text.Length, origText);
//			document.Caret.Offset = Math.Min(document.TextLength, Math.Max(0, oldCaretPos));
			document.UndoStack.AcceptChanges = true;
		}
		
		/// <remarks>
		/// Redo last undone operation
		/// </remarks>
		public void Redo()
		{
			// we clear all selection direct, because the redraw
			// is done per refresh at the end of the action
//			document.SelectionCollection.Clear();

			document.UndoStack.AcceptChanges = false;
			document.Replace(offset, origText.Length, text);
//			document.Caret.Offset = Math.Min(document.TextLength, Math.Max(0, document.Caret.Offset));
			document.UndoStack.AcceptChanges = true;
		}
	}
}
