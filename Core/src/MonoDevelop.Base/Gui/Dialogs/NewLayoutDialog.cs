// NewLayoutDialog.cs
//
// Authors: Gustavo Giráldez  <gustavo.giraldez@gmx.net>

using System;

using Gtk;

using MonoDevelop.Gui;

namespace MonoDevelop.Gui.Dialogs
{
	internal class NewLayoutDialog : IDisposable
	{
		IWorkbenchLayout wbLayout = WorkbenchSingleton.Workbench.WorkbenchLayout;
		string[] existentLayouts;

		[Glade.Widget] Entry layoutName;
//		[Glade.Widget] Button cancelButton;
		[Glade.Widget] Button newButton;
		[Glade.Widget] Dialog newLayoutDialog;

		public NewLayoutDialog ()
		{
			Glade.XML xml = new Glade.XML (null, "Base.glade",
			                               "newLayoutDialog", null);
			xml.Autoconnect (this);
			
			newButton.Sensitive = false;
			newButton.GrabDefault ();
			
			layoutName.Changed += new EventHandler (OnNameChanged);

			if (wbLayout != null)
				existentLayouts = wbLayout.Layouts;
		}

		public void Dispose ()
		{
			newLayoutDialog.Dispose ();
		}

		void OnNameChanged (object obj, EventArgs args)
		{
			newButton.Sensitive = (layoutName.Text != "" &&
			                       Array.IndexOf (existentLayouts, layoutName.Text) == -1);
		}

		public void Run ()
		{
			if (wbLayout == null)
				return;
			
			ResponseType response = (ResponseType) newLayoutDialog.Run ();
			switch (response)
			{
			case ResponseType.Ok:
				wbLayout.CurrentLayout = layoutName.Text;
				break;
			}

			newLayoutDialog.Destroy ();
		}
	}
}
