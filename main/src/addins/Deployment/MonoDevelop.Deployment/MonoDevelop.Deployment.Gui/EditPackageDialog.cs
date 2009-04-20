
using System;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Deployment.Gui
{

	internal partial class EditPackageDialog : Gtk.Dialog
	{
		PackageBuilder target;
		Package package;
		
		public EditPackageDialog (Package package)
		{
			this.Build();
			
			this.package = package;
			target = package.PackageBuilder.Clone ();
			this.Title = target.Description;
			
			this.Icon = MonoDevelop.Core.Gui.ImageService.GetPixbuf (target.Icon, Gtk.IconSize.Menu);
			entryName.Text = package.Name;
			
			targetBox.PackStart (new PackageBuilderEditor (target), true, true, 0);
			
			entrySelector.Fill (target, null);
			entrySelector.SetSelection (target.RootSolutionItem, target.GetChildEntries ());
			
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
				MonoDevelop.Core.Gui.MessageService.ShowError (this, msg);
				return;
			}
			package.Name = entryName.Text;
			package.PackageBuilder = target.Clone ();
			Respond (Gtk.ResponseType.Ok);
		}

		protected virtual void OnEntrySelectorSelectionChanged(object sender, System.EventArgs e)
		{
			SolutionItem ce = entrySelector.GetSelectedEntry ();
			if (ce != null)
				target.SetSolutionItem (ce, entrySelector.GetSelectedChildren ());
		}

		protected virtual void OnNotebookSwitchPage(object o, Gtk.SwitchPageArgs args)
		{
			if (args.PageNum == 2) {
				fileListView.Fill (target);
			}
		}
	}
}
