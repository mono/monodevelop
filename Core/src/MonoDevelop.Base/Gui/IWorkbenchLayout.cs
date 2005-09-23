// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Gui
{
	/// <summary>
	/// The IWorkbenchLayout object is responsible for the layout of 
	/// the workspace, it shows the contents, chooses the IWorkbenchWindow
	/// implementation etc. it could be attached/detached at the runtime
	/// to a workbench.
	/// </summary>
	public interface IWorkbenchLayout: IMementoCapable
	{
		/// <summary>
		/// The active workbench window.
		/// </summary>

		Gtk.Widget LayoutWidget {
			get;
		}
		
		IWorkbenchWindow ActiveWorkbenchwindow {
			get;
		}

		/// <summary>
		/// The name of the active layout.
		/// </summary>
		string CurrentLayout {
			get;
			set;
		}

		/// <summary>
		/// A list of the currently available layouts for the current workbench context.
		/// </summary>
		string[] Layouts {
			get;
		}
		
		void DeleteLayout (string name);

		/// <summary>
		/// Attaches this layout manager to a workbench object.
		/// </summary>
		void Attach(IWorkbench workbench);
		
		/// <summary>
		/// Detaches this layout manager from the current workspace.
		/// </summary>
		void Detach();
		
		/// <summary>
		/// Shows a new <see cref="IPadContent"/>.
		/// </summary>
		void ShowPad(IPadContent content);
		
		/// <summary>
		/// Activates a pad (Show only makes it visible but Activate does
		/// bring it to foreground)
		/// </summary>
		void ActivatePad(IPadContent content);
		
		/// <summary>
		/// Hides a new <see cref="IPadContent"/>.
		/// </summary>
		void HidePad(IPadContent content);
		
		/// <summary>
		/// returns true, if padContent is visible;
		/// </summary>
		bool IsVisible(IPadContent padContent);
		
		/// <summary>
		/// Re-initializes all components of the layout manager.
		/// </summary>
		void RedrawAllComponents();
		
		/// <summary>
		/// Shows a new <see cref="IViewContent"/>.
		/// </summary>
		IWorkbenchWindow ShowView(IViewContent content);

		void RemoveTab (int pageNum);	

		/// <summary>
		/// Moves to the next tab.
		/// </summary>          
		void NextTab();
		
		/// <summary>
		/// Moves to the previous tab.
		/// </summary>          
		void PreviousTab();
		
		/// <summary>
		/// Is called, when the workbench window which the user has into
		/// the foreground (e.g. editable) changed to a new one.
		/// </summary>
		event EventHandler ActiveWorkbenchWindowChanged;

		/// <summary>
		/// A collection of all valid pads in the layout for the workbench context.
		/// </summary>
		PadContentCollection PadContentCollection {
			get;
		}
		
		void ActiveMdiChanged(object sender, Gtk.SwitchPageArgs e);
	}
}
