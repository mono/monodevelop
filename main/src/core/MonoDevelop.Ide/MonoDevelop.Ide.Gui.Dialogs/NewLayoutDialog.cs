// NewLayoutDialog.cs
//
// Authors: Gustavo Giráldez  <gustavo.giraldez@gmx.net>

using System;
using Gtk;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class NewLayoutDialog : Gtk.Dialog
	{
		string[] existentLayouts;

		public NewLayoutDialog ()
		{
			Build ();
			
			newButton.Sensitive = false;
			newButton.GrabDefault ();
			
			layoutName.Changed += new EventHandler (OnNameChanged);

			existentLayouts = IdeApp.Workbench.Layouts;
		}
		
		public string LayoutName {
			get { return layoutName.Text; }
		}

		void OnNameChanged (object obj, EventArgs args)
		{
			newButton.Sensitive = (layoutName.Text != "" &&
			                       Array.IndexOf (existentLayouts, layoutName.Text) == -1);
		}
	}
}
