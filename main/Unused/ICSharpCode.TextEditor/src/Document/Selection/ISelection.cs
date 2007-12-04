//  ISelection.cs
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
using MonoDevelop.TextEditor.Undo;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// An interface representing a portion of the current selection.
	/// </summary>
	public interface ISelection
	{
		Point StartPosition {
			get;
			set;
		}
		
		Point EndPosition {
			get;
			set;
		}
		
		int Offset {
			get;
		}
		
		int EndOffset {
			get;
		}
		
		int Length {
			get;
		}
		
		/// <value>
		/// Returns true, if the selection is rectangular
		/// </value>
		bool IsRectangularSelection {
			get;
		}
		
		/// <value>
		/// Returns true, if the selection is empty
		/// </value>
		bool IsEmpty {
			get;
		}

		/// <value>
		/// The text which is selected by this selection.
		/// </value>
		string SelectedText {
			get;
		}
		
		bool ContainsOffset(int offset);
		
		bool ContainsPosition(Point position);
	}
}
