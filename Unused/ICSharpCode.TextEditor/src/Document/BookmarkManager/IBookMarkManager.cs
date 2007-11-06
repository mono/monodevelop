//  IBookMarkManager.cs
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
using System.Collections;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// This class handles the bookmarks for a buffer
	/// </summary>
	public interface IBookMarkManager
	{
		/// <value>
		/// Contains all bookmarks as int values
		/// </value>
		ArrayList Marks {
			get;
		}
		
		/// <value>
		/// The lowest mark, if no marks exists it returns -1
		/// </value>
		int FirstMark {
			get;
		}
		
		/// <value>
		/// The highest mark, if no marks exists it returns -1
		/// </value>
		int LastMark {
			get;
		}
		
		/// <remarks>
		/// Sets the mark at the line <code>lineNr</code> if it is not set, if the
		/// line is already marked the mark is cleared.
		/// </remarks>
		void ToggleMarkAt(int lineNr);
		
		/// <remarks>
		/// Returns true if the line <code>lineNr</code> is marked
		/// </remarks>
		bool IsMarked(int lineNr);
		
		/// <remarks>
		/// Clears all bookmarks
		/// </remarks>
		void Clear();
		
		/// <remarks>
		/// returns first mark higher than <code>lineNr</code>
		/// </remarks>
		/// <returns>
		/// returns the next mark > cur, if it not exists it returns FirstMark()
		/// </returns>
		int GetNextMark(int lineNr);
		
		/// <remarks>
		/// returns first mark lower than <code>lineNr</code>
		/// </remarks>
		/// <returns>
		/// returns the next mark lower than cur, if it not exists it returns LastMark()
		/// </returns>
		int GetPrevMark(int lineNr);
		
		
		/// <remarks>
		/// Is fired before the bookmarks change
		/// </remarks>
		event EventHandler BeforeChanged;
		
		/// <remarks>
		/// Is fired after the bookmarks change
		/// </remarks>
		event EventHandler Changed;
	}
}
