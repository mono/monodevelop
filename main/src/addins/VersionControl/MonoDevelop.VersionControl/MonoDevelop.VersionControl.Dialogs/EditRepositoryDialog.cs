using System;
using System.Collections;

namespace MonoDevelop.VersionControl.Dialogs
{
	internal partial class EditRepositoryDialog : Gtk.Dialog
	{
		Repository repo;
		ArrayList systems = new ArrayList ();
		IRepositoryEditor editor;
		
		public EditRepositoryDialog (Repository editedRepository)
		{
			Build ();
			this.repo = editedRepository;
			
			if (repo != null) {
				versionControlType.Sensitive = false;
				versionControlType.AppendText (repo.VersionControlSystem.Name);
				versionControlType.Active = 0;
				
				editor = repo.VersionControlSystem.CreateRepositoryEditor (repo);
				repoEditorContainer.Add (editor.Widget);
				editor.Widget.Show ();
			}
			else {
				foreach (VersionControlSystem vcs in VersionControlService.GetVersionControlSystems ()) {
					if (vcs.IsInstalled) {
						versionControlType.AppendText (vcs.Name);
						systems.Add (vcs);
					}
				}
				versionControlType.Active = 0;
			}
			if (repo != null) {
				entryName.Text = repo.Name;
				repo.NameChanged += OnNameChanged;
			}
		}
		
		void OnNameChanged (object s, EventArgs a)
		{
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
			repo.NameChanged += OnNameChanged;
			editor = vcs.CreateRepositoryEditor (repo);
			repoEditorContainer.Add (editor.Widget);
			editor.Widget.Show ();
			entryName.Sensitive = true;
		}

		protected virtual void OnVersionControlTypeChanged(object sender, System.EventArgs e)
		{
			UpdateEditor ();
		}

		protected virtual void OnEntryNameChanged(object sender, System.EventArgs e)
		{
			repo.NameChanged -= OnNameChanged;
			repo.Name = entryName.Text;
			repo.NameChanged += OnNameChanged;
		}
		
		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			if (editor.Validate ())
				Respond (Gtk.ResponseType.Ok);
		}
		
		
		public Repository Repository {
			get { return repo; }
		}
		
	}
}
