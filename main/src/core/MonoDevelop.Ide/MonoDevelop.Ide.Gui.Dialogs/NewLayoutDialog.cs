// NewLayoutDialog.cs
//
// Authors: Gustavo Giráldez  <gustavo.giraldez@gmx.net>

using System;
using Gtk;
using MonoDevelop.Ide.Gui;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class NewLayoutDialog : Gtk.Dialog
	{
		IList<string> existingLayouts;

		public NewLayoutDialog ()
		{
			Build ();
			
			existingLayouts = IdeApp.Workbench.Layouts;
			
			layoutName.Changed += OnNameChanged;
			OnNameChanged (layoutName, EventArgs.Empty);

			newButton.GrabDefault ();
		}
		
		public string LayoutName {
			get { return layoutName.Text; }
		}

		void OnNameChanged (object obj, EventArgs args)
		{
			var txt = LayoutName;
			
			if (string.IsNullOrEmpty (txt)) {
				validationMessage.Text = GettextCatalog.GetString ("Enter a name for the new layout");
			} else if (!char.IsLetterOrDigit (txt[0])) {
				validationMessage.Text = GettextCatalog.GetString ("Name must start with a letter or number");
			} else if (txt.Any (ch => !char.IsLetterOrDigit (ch) && ch != ' ')) {
				validationMessage.Text = GettextCatalog.GetString ("Name must contain only letters, numbers and spaces");
			} else if (existingLayouts.Contains (layoutName.Text)) {
				validationMessage.Text = GettextCatalog.GetString ("There is already a layout with that name");
			} else {
				validationMessage.Text = GettextCatalog.GetString ("Layout name is valid");
				newButton.Sensitive = true;
				return;
			}
			
			newButton.Sensitive = false;
		}
	}
}
