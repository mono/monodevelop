
using System;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Deployment.Gui
{

	public partial class EditPackageDialog : Gtk.Dialog
	{
		PackageBuilder target;
		Package package;
		
		public EditPackageDialog (Package package)
		{
			this.Build();
			
			this.package = package;
			target = package.PackageBuilder.Clone ();
			this.Title = target.Description;
			
			this.Icon = MonoDevelop.Core.Gui.Services.Resources.GetIcon (target.Icon, Gtk.IconSize.Menu);
			entryName.Text = package.Name;
			
			targetBox.PackStart (new PackageBuilderEditor (target), true, true, 0);
			
			entrySelector.Fill (target, null);
			entrySelector.SetSelection (target.RootCombineEntry, target.GetChildEntries ());
			
			DeployContext ctx = target.CreateDeployContext ();
			if (ctx == null)
				pageFiles.Hide ();
			else
				ctx.Dispose ();
		}

		protected virtual void OnEntryNameChanged(object sender, System.EventArgs e)
		{
			okbutton.Sensitive = entryName.Text.Length > 0;
		}

		protected virtual void OnOkbuttonClicked(object sender, System.EventArgs e)
		{
			string msg = target.Validate ();
			if (!string.IsNullOrEmpty (msg)) {
				IdeApp.Services.MessageService.ShowError (null, msg, this, true);
				return;
			}
			package.Name = entryName.Text;
			package.PackageBuilder = target.Clone ();
			Respond (Gtk.ResponseType.Ok);
		}

		protected virtual void OnEntrySelectorSelectionChanged(object sender, System.EventArgs e)
		{
			CombineEntry ce = entrySelector.GetSelectedEntry ();
			if (ce != null)
				target.SetCombineEntry (ce, entrySelector.GetSelectedChildren ());
		}

		protected virtual void OnNotebookSwitchPage(object o, Gtk.SwitchPageArgs args)
		{
			if (args.PageNum == 2) {
				fileListView.Fill (target);
			}
		}
	}
}
