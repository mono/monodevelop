
using System;
using MonoDevelop.Projects;

namespace MonoDevelop.Deployment.Gui
{
	internal partial class InstallDialog : Gtk.Dialog
	{
		public InstallDialog (SolutionItem entry)
		{
			this.Build();
			nameEntry.Text = entry.Name;
			UpdateControls ();
		}
		
		void UpdateControls ()
		{
			buttonOk.Sensitive = folderEntry.Path.Length > 0 && nameEntry.Text.Length > 0;
		}

		protected virtual void OnFolderEntryPathChanged(object sender, System.EventArgs e)
		{
			UpdateControls ();
		}

		protected virtual void OnNameEntryChanged(object sender, System.EventArgs e)
		{
			UpdateControls ();
		}
		
		public string Prefix {
			get { return folderEntry.Path; }
		}
		
		public string AppName {
			get { return nameEntry.Text; }
		}
	}
}
