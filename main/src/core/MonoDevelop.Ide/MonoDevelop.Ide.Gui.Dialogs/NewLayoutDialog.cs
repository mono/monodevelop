// NewLayoutDialog.cs
//
// Authors: Gustavo Giráldez  <gustavo.giraldez@gmx.net>

using System;
using Gtk;
using MonoDevelop.Ide.Gui;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class NewLayoutDialog : Gtk.Dialog
	{
		IList<string> existentLayouts;

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
			var txt = LayoutName;
			//FIXME: add message when name invalid
			var valid = !string.IsNullOrEmpty (txt) && txt.All (ch => Char.IsLetterOrDigit (ch) || ch == ' ');
			newButton.Sensitive = valid && !existentLayouts.Contains (layoutName.Text);
		}
	}
}
