//  IBaseViewContent.cs
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
//using System.Windows.Forms;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// The base functionalty all view contents must provide
	/// </summary>
	public interface IBaseViewContent : IDisposable
	{
		/// <summary>
		/// This is the Windows.Forms control for the view.
		/// </summary>
		Gtk.Widget Control {
			get;
		}
		
		/// <summary>
		/// The workbench window in which this view is displayed.
		/// </summary>
		IWorkbenchWindow  WorkbenchWindow {
			get;
			set;
		}
		
		/// <summary>
		/// The text on the tab page when more than one view content
		/// is attached to a single window.
		/// </summary>
		string TabPageLabel {
			get;
		}
		
		/// <summary>
		/// Reinitializes the content. (Re-initializes all add-in tree stuff)
		/// and redraws the content. Call this not directly unless you know
		/// what you do.
		/// </summary>
		void RedrawContent();
		
		bool CanReuseView (string fileName);
		
		object GetContent (Type contentType);
	}
}
