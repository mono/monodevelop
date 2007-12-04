//  IPositionable.cs
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

namespace MonoDevelop.Ide.Gui.Content
{
	/// <summary>
	/// If a IViewContent object is from the type IPositionable it signals
	/// that it's a texteditor which could set the caret to a position inside
	/// a file. 
	/// </summary>
	public interface IPositionable
	{
		/// <summary>
		/// Sets the 'caret' to the position pos, where pos.Y is the line (starting from 0).
		/// And pos.X is the column (starting from 0 too).
		/// </summary>
		void JumpTo(int line, int column);
	}
}
