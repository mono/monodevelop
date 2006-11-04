using System;
using System.Collections;
using VersionControl.Service;

namespace VersionControl.AddIn.Dialogs
{
	public class EditRepositoryDialog : Gtk.Dialog
	{
		Repository repo;
		protected Gtk.EventBox repoEditorContainer;
		protected Gtk.ComboBox versionControlType;
		ArrayList systems = new ArrayList ();
		protected Gtk.Entry entryName;
		
		public EditRepositoryDialog (Repository editedRepository)
		{
			Stetic.Gui.Build(this, typeof(EditRepositoryDialog));
			this.repo = editedRepository;
			
			if (repo != null) {
				versionControlType.Sensitive = false;
				versionControlType.AppendText (repo.VersionControlSystem.Name);
				versionControlType.Active = 0;
				
				Gtk.Widget editor = repo.VersionControlSystem.CreateRepositoryEditor (repo);
				repoEditorContainer.Add (editor);
				editor.Show ();
			}
			else {
				foreach (VersionControlSystem vcs in VersionControlService.GetVersionControlSystems ()) {
					systems.Add (vcs);
					versionControlType.AppendText (vcs.Name);
				}
				versionControlType.Active = -1;
				entryName.Sensitive = false;
			}
			if (repo != null)
				entryName.Text = repo.Name;
		}
		
		void UpdateEditor ()
		{
			if (systems.Count == 0)
				return;
				
			if (repoEditorContainer.Child != null)
				repoEditorContainer.Remove (repoEditorContainer.Child);
			
			if (versionControlType.Active == -1) {
				entryName.Sensitive = false;
				return;
			}

			string oldname = repo != null ? repo.Name : "";

			VersionControlSystem vcs = (VersionControlSystem) systems [versionControlType.Active];
			repo = vcs.CreateRepositoryInstance ();
			repo.Name = oldname;
			Gtk.Widget editor = vcs.CreateRepositoryEditor (repo);
			repoEditorContainer.Add (editor);
			editor.Show ();
			entryName.Sensitive = true;
		}

		protected virtual void OnVersionControlTypeChanged(object sender, System.EventArgs e)
		{
			UpdateEditor ();
		}

		protected virtual void OnEntryNameChanged(object sender, System.EventArgs e)
		{
			repo.Name = entryName.Text;
		}
		
		public Repository Repository {
			get { return repo; }
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			Destroy ();
		}
	}
}
