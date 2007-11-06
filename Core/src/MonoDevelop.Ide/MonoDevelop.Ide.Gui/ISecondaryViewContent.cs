//  ISecondaryViewContent.cs
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

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// The base interface for secondary view contents
	/// (designer, doc viewer etc.)
	/// </summary>
	public interface ISecondaryViewContent : IBaseViewContent
	{
		/// <summary>
		/// Is called when the view content is selected inside the window
		/// tab. NOT when the windows is selected.
		/// </summary>
		void Selected();
		
		/// <summary>
		/// Is called when the view content is deselected inside the window
		/// tab before the other window is selected. NOT when the windows is deselected.
		/// </summary>
		void Deselected();
		
		/// <summary>
		/// Is called before the save operation of the main IViewContent
		/// </summary>
		void NotifyBeforeSave();

		void BaseContentChanged ();
	}
}
