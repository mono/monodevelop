// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
//using System.Windows.Forms;

namespace MonoDevelop.Gui
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
		
		/// <summary>
		/// Reinitializes the content. (Re-initializes all add-in tree stuff)
		/// and redraws the content. Call this not directly unless you know
		/// what you do.
		/// </summary>
		void RedrawContent();
	}
}
