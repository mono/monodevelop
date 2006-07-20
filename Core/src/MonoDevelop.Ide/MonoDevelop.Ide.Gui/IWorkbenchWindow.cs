// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
		}
		
		/// <summary>
		/// Closes the window, if force == true it closes the window
		/// without ask, even the content is dirty.
		/// </summary>
		void CloseWindow(bool force, bool fromMenu, int pageNum);
		
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
