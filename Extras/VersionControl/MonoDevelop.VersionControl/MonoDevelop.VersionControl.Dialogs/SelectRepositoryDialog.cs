
using System;
using System.Collections;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.VersionControl.Dialogs
{
	public enum SelectRepositoryMode
	{
		Checkout,
		Publish
	}
	
	public partial class SelectRepositoryDialog : Gtk.Dialog
	{
		Repository repo;
		ArrayList systems = new ArrayList ();
		Gtk.TreeStore store;
		SelectRepositoryMode mode;
		
		const int RepositoryCol = 0;
		const int RepoNameCol = 1;
		const int VcsName = 2;
		const int FilledCol = 3;
		const int IconCol = 4;
		
		public SelectRepositoryDialog (SelectRepositoryMode mode)
		{
			Build ();
			
			foreach (VersionControlSystem vcs in VersionControlService.GetVersionControlSystems ()) {
				if (vcs.IsInstalled) {
					repCombo.AppendText (vcs.Name);
					systems.Add (vcs);
				}
			}
			repCombo.Active = 0;
			this.mode = mode;
			
			store = new Gtk.TreeStore (typeof(object), typeof(string), typeof(string), typeof(bool), typeof(string));
			repoTree.Model = store;
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = "Repository";
			CellRendererText crt = new CellRendererText ();
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			col.PackStart (crp, false);
			col.PackStart (crt, true);
			col.AddAttribute (crp, "stock-id", IconCol);
			col.AddAttribute (crt, "text", RepoNameCol);
			repoTree.AppendColumn (col);
			repoTree.AppendColumn ("Type", new CellRendererText (), "text", VcsName);
			repoTree.TestExpandRow += new Gtk.TestExpandRowHandler (OnTestExpandRow);
			LoadRepositories ();
			
			if (mode == SelectRepositoryMode.Checkout) {
				labelName.Visible = false;
				entryName.Visible = false;
				boxMessage.Visible = false;
				labelMessage.Visible = false;
				string pathName = MonoDevelop.Core.Runtime.Properties.GetProperty ("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", Environment.GetFolderPath (Environment.SpecialFolder.Personal)).ToString ();
				entryFolder.Text = pathName;
			} else {
				labelTargetDir.Visible = false;
				boxFolder.Visible = false;
			}
		}
		
		public Repository Repository {
			get {
				if (notebook.Page == 0)
					return repo;
				else
					return GetSelectedRepository ();
			}
		}
		
		public string ModuleName {
			get { return entryName.Text; }
			set { entryName.Text = value; }
		}
		
		public string Message {
			get { return entryMessage.Text; }
			set { entryMessage.Text = value; }
		}
		
		public string TargetPath {
			get { return entryFolder.Text; }
			set { entryFolder.Text = value; }
		}

		void UpdateRepoDescription ()
		{
			if (Repository != null)
				labelRepository.Text = Repository.LocationDescription;
			else
				labelRepository.Text = "";
		}

		protected virtual void OnRepComboChanged(object sender, System.EventArgs e)
		{
			if (repoContainer.Child != null)
				repoContainer.Remove (repoContainer.Child);
			
			if (repCombo.Active == -1)
				return;

			VersionControlSystem vcs = (VersionControlSystem) systems [repCombo.Active];
			repo = vcs.CreateRepositoryInstance ();
			Gtk.Widget editor = vcs.CreateRepositoryEditor (repo);
			repoContainer.Add (editor);
			editor.Show ();
			UpdateRepoDescription ();
		}
		
		public void LoadRepositories ()
		{
			store.Clear ();
			foreach (Repository r in VersionControlService.GetRepositories ()) {
				LoadRepositories (r, Gtk.TreeIter.Zero);
			}
		}
		
		public void LoadRepositories (Repository r, Gtk.TreeIter parent)
		{
			if (r.VersionControlSystem == null)
				return;

			TreeIter it;
			if (!parent.Equals (TreeIter.Zero))
				it = store.AppendValues (parent, r, r.Name, r.VersionControlSystem.Name, false, "vcs-repository");
			else
				it = store.AppendValues (r, r.Name, r.VersionControlSystem.Name, false, "vcs-repository");

			try {
				if (r.HasChildRepositories)
					store.AppendValues (it, null, "", "", true, "vcs-repository");
			}
			catch (Exception ex) {
				Runtime.LoggingService.Error (ex);
			}
		}

		protected virtual void OnButtonAddClicked(object sender, System.EventArgs e)
		{
			EditRepositoryDialog dlg = new EditRepositoryDialog (null);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					VersionControlService.AddRepository (dlg.Repository);
					VersionControlService.SaveConfiguration ();
					LoadRepositories (dlg.Repository, Gtk.TreeIter.Zero);
				}
			} finally {
				dlg.Destroy ();
			}
		}

		protected virtual void OnButtonRemoveClicked(object sender, System.EventArgs e)
		{
			TreeIter iter;
			TreeModel model;
			if (repoTree.Selection.GetSelected (out model, out iter)) {
				VersionControlService.RemoveRepository (
					(Repository) store.GetValue (iter, RepositoryCol));
				VersionControlService.SaveConfiguration ();
				store.Remove (ref iter);
			}
		}

		protected virtual void OnButtonEditClicked(object sender, System.EventArgs e)
		{
			try {
			Repository rep = GetSelectedRepository ();
			if (rep != null) {
				EditRepositoryDialog dlg = new EditRepositoryDialog (rep);
				try {
					if (dlg.Run () != (int) Gtk.ResponseType.Ok) {
						VersionControlService.ResetConfiguration ();
						return;
					}

					VersionControlService.SaveConfiguration ();

					TreeIter iter;
					TreeModel model;
					if (repoTree.Selection.GetSelected (out model, out iter)) {
						// Update values
						store.SetValue (iter, RepoNameCol, rep.Name);
						store.SetValue (iter, VcsName, rep.VersionControlSystem.Name);
						store.SetValue (iter, FilledCol, false);
					}
					UpdateRepoDescription ();
				} finally {
					dlg.Destroy ();
				}
			}
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		Repository GetSelectedRepository ()
		{
			TreeIter iter;
			TreeModel model;
			if (repoTree.Selection.GetSelected (out model, out iter))
				return (Repository) store.GetValue (iter, RepositoryCol);
			else
				return null;
		}

		private void OnTestExpandRow (object sender, Gtk.TestExpandRowArgs args)
		{
			bool filled = (bool) store.GetValue (args.Iter, FilledCol);
			Repository parent = (Repository) store.GetValue (args.Iter, RepositoryCol);
			if (!filled) {
				store.SetValue (args.Iter, FilledCol, true);
				
				// Remove the dummy child
				TreeIter iter;
				store.IterChildren (out iter, args.Iter);
				store.Remove (ref iter);
				
				try {
					// Add child repositories
					foreach (Repository rep in parent.ChildRepositories)
						LoadRepositories (rep, args.Iter);
				} catch (Exception ex) {
					store.AppendValues (args.Iter, null, "ERROR: " + ex.Message, "", true);
					Runtime.LoggingService.Error (ex);
				}
					
				// If after all there are no children, return false
				if (!store.IterChildren (out iter, args.Iter)) {
					args.RetVal = true;
				}
			} else
				args.RetVal = false;
		}
		
		protected virtual void OnRepoTreeCursorChanged(object sender, System.EventArgs e)
		{
			UpdateControls ();
			UpdateRepoDescription ();
		}
		
		void UpdateControls ()
		{
			TreeIter iter;
			TreeModel model;
			if (repoTree.Selection.GetSelected (out model, out iter)) {
				if (store.IterParent (out iter, iter)) {
					buttonRemove.Sensitive = false;
					buttonEdit.Sensitive = false;
					return;
				}
			}
			buttonRemove.Sensitive = true;
			buttonEdit.Sensitive = true;
		}

		protected virtual void OnButtonBrowseClicked(object sender, System.EventArgs e)
		{
			FileChooserDialog dialog =
				new FileChooserDialog ("Select target directory", null, FileChooserAction.SelectFolder,
						       Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
						       Gtk.Stock.Open, Gtk.ResponseType.Ok);
			int response = dialog.Run ();
			try {
				if (response == (int)Gtk.ResponseType.Ok) {
					entryFolder.Text = dialog.Filename;
				}
			} finally {
				dialog.Destroy ();
			}
		}

		protected virtual void OnNotebookChangeCurrentPage(object o, Gtk.ChangeCurrentPageArgs args)
		{
			UpdateRepoDescription ();
		}

		protected virtual void OnEntryFolderChanged(object sender, System.EventArgs e)
		{
			if (mode == SelectRepositoryMode.Checkout)
				buttonOk.Sensitive = entryFolder.Text.Length > 0;
		}
	}
}
