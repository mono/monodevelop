//  IViewContent.cs
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

using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// IViewContent is the base interface for all editable data
	/// inside SharpDevelop.
	/// </summary>
	public interface IViewContent : IBaseViewContent
	{
		
		/// <summary>
		/// A generic name for the file, when it does have no file name
		/// (e.g. newly created files)
		/// </summary>
		string UntitledName {
			get;
			set;
		}
		
		/// <summary>
		/// This is the whole name of the content, e.g. the file name or
		/// the url depending on the type of the content.
		/// </summary>
		string ContentName {
			get;
			set;
		}
		
		/// <summary>
		/// If this property returns true the view is untitled.
		/// </summary>
		bool IsUntitled {
			get;
		}
		
		/// <summary>
		/// If this property returns true the content has changed since
		/// the last load/save operation.
		/// </summary>
		bool IsDirty {
			get;
			set;
		}
		
		/// <summary>
		/// If this property returns true the content could not be altered.
		/// </summary>
		bool IsReadOnly {
			get;
		}
		
		/// <summary>
		/// If this property returns true the content can't be written.
		/// </summary>
		bool IsViewOnly {
			get;
		}
		
		bool IsFile {
			get;
		}

		string StockIconId {
			get;
		}
		
		/// <summary>
		/// Saves this content to the last load/save location.
		/// </summary>
		void Save();
		
		/// <summary>
		/// Saves the content to the location <code>fileName</code>
		/// </summary>
		void Save(string fileName);
		
		/// <summary>
		/// Loads the content from the location <code>fileName</code>
		/// </summary>
		void Load(string fileName);
		
		/// <summary>
		/// The name of the project the content is attached to
		/// </summary>
		Project Project {
			get;
			set;
		}
	
		/// <summary>
		/// The path relative to the project
		/// </summary>
		string PathRelativeToProject {
			get;
		}
		
		/// <summary>
		/// Is called each time the name for the content has changed.
		/// </summary>
		event EventHandler ContentNameChanged;
		
		/// <summary>
		/// Is called when the content is changed after a save/load operation
		/// and this signals that changes could be saved.
		/// </summary>
		event EventHandler DirtyChanged;
		
		event EventHandler BeforeSave;

		event EventHandler ContentChanged;
	}
}
