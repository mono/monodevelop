//  IWorkbenchWindow.cs
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

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// The IWorkbenchWindow is the basic interface to a window which
	/// shows a view (represented by the IViewContent object).
	/// </summary>
	public interface IWorkbenchWindow
	{
		/// <summary>
		/// The window title.
		/// </summary>
		string Title {
			get;
			set;
		}
		
		string DocumentType {
			get;
			set;
		}

		bool ShowNotification {
			get;
			set;
		}
		
		/// <summary>
		/// The current view content which is shown inside this window.
		/// </summary>
		IViewContent ViewContent {
			get;
		}
		
		/// <summary>
		/// returns null if no sub view contents are attached.
		/// </summary>
		ArrayList SubViewContents {
			get;
		}
		
		IBaseViewContent ActiveViewContent {
			get;
			set;
		}
		
		/// <summary>
		/// Closes the window, if force == true it closes the window
		/// without ask, even the content is dirty.
		/// </summary>
		bool CloseWindow(bool force, bool fromMenu, int pageNum);
		
		/// <summary>
		/// Brings this window to front and sets the user focus to this
		/// window.
		/// </summary>
		void SelectWindow();
		
		void SwitchView(int viewNumber);
		
		void AttachSecondaryViewContent(ISecondaryViewContent secondaryViewContent);
		
		/// <summary>
		/// Is called when the title of this window has changed.
		/// </summary>
		event EventHandler TitleChanged;
		
		/// <summary>
		/// Is called after the window closes.
		/// </summary>
		event WorkbenchWindowEventHandler Closing;
		
		/// <summary>
		/// Is called after the window closes.
		/// </summary>
		event EventHandler Closed;
		
		event ActiveViewContentEventHandler ActiveViewContentChanged;
	}
	
	public delegate void WorkbenchWindowEventHandler (object sender, WorkbenchWindowEventArgs args);
	
	public class WorkbenchWindowEventArgs: System.ComponentModel.CancelEventArgs
	{
		bool forced;
		
		public WorkbenchWindowEventArgs (bool forced)
		{
			this.forced = forced;
		}
		
		public bool Forced {
			get { return forced; }
		}
	}
	
		
	public delegate void ActiveViewContentEventHandler (object sender, ActiveViewContentEventArgs args);
	
	public class ActiveViewContentEventArgs: System.EventArgs
	{
		IBaseViewContent content;
		
		public ActiveViewContentEventArgs (IBaseViewContent content)
		{
			this.content = content;
		}
		
		public IBaseViewContent Content {
			get { return content; }
		}
	}
}
