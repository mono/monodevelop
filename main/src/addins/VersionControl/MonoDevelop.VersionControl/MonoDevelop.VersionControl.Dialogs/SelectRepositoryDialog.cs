
using System;
using System.Threading;
using System.Collections;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Dialogs
{
	internal enum SelectRepositoryMode
	{
		Checkout,
		Publish
	}
	
	internal partial class SelectRepositoryDialog : Gtk.Dialog
	{
		Repository repo;
		List<VersionControlSystem> systems = new List<VersionControlSystem> ();
		Gtk.TreeStore store;
		SelectRepositoryMode mode;
		List<Repository> loadingRepos = new List<Repository> ();
		IRepositoryEditor currentEditor;
		string defaultPath;
		public readonly ConfigurationProperty<string> VersionControlDefaultPath = ConfigurationProperty.Create ("MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog.DefaultPath", System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Projects"));

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
			col.Title = GettextCatalog.GetString ("Repository");
			CellRendererText crt = new CellRendererText ();
			CellRendererImage crp = new CellRendererImage ();
			col.PackStart (crp, false);
			col.PackStart (crt, true);
			col.AddAttribute (crp, "stock-id", IconCol);
			col.AddAttribute (crt, "text", RepoNameCol);
			repoTree.AppendColumn (col);
			repoTree.AppendColumn (GettextCatalog.GetString ("Type"), new CellRendererText (), "text", VcsName);
			repoTree.TestExpandRow += new Gtk.TestExpandRowHandler (OnTestExpandRow);
			LoadRepositories ();

			if (mode == SelectRepositoryMode.Checkout) {
				labelName.Visible = false;
				entryName.Visible = false;
				boxMessage.Visible = false;
				labelMessage.Visible = false;
				defaultPath = VersionControlDefaultPath;
				entryFolder.Text = defaultPath;
				buttonOk.Label = GettextCatalog.GetString ("_Checkout");
			} else {
				labelTargetDir.Visible = false;
				boxFolder.Visible = false;
			}

			repoContainer.SetFlag (WidgetFlags.NoWindow);
		}
		
		public Repository Repository {
			get {
				if (notebook.Page == 0)
					return repo;
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
			else {
				labelRepository.Text = String.Empty;
				entryMessage.Text = String.Empty;
				entryFolder.Text = String.Empty;
			}
		}

		protected virtual void OnRepComboChanged(object sender, System.EventArgs e)
		{
			if (repoContainer.Child != null)
				repoContainer.Remove (repoContainer.Child);
			
			if (repCombo.Active == -1)
				return;

			VersionControlSystem vcs = systems [repCombo.Active];
			repo = vcs.CreateRepositoryInstance ();
			currentEditor = vcs.CreateRepositoryEditor (repo);
			repoContainer.Add (currentEditor.Widget);
			currentEditor.Show ();
			UrlBasedRepositoryEditor edit = currentEditor as UrlBasedRepositoryEditor;
			if (edit != null)
				edit.PathChanged += OnPathChanged;
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
				it = store.AppendValues (parent, r, r.Name, r.VersionControlSystem.Name, false, "vc-repository");
			else
				it = store.AppendValues (r, r.Name, r.VersionControlSystem.Name, false, "vc-repository");

			try {
				if (r.HasChildRepositories)
					store.AppendValues (it, null, "", "", true, null);
			}
			catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}

		protected virtual void OnButtonAddClicked(object sender, System.EventArgs e)
		{
			EditRepositoryDialog dlg = new EditRepositoryDialog (null);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok && dlg.Repository != null) {
					VersionControlService.AddRepository (dlg.Repository);
					VersionControlService.SaveConfiguration ();
					LoadRepositories (dlg.Repository, Gtk.TreeIter.Zero);
				}
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
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
			UpdateRepoDescription ();
		}

		protected virtual void OnButtonEditClicked(object sender, System.EventArgs e)
		{
			Repository rep = GetSelectedRepository ();
			if (rep != null) {
				Repository repCopy = rep.Clone ();
				EditRepositoryDialog dlg = new EditRepositoryDialog (repCopy);
				try {
					if (MessageService.RunCustomDialog (dlg, this) != (int) Gtk.ResponseType.Ok) {
						VersionControlService.ResetConfiguration ();
						return;
					}
					
					rep.CopyConfigurationFrom (repCopy);
					VersionControlService.SaveConfiguration ();

					TreeIter iter;
					TreeModel model;
					if (repoTree.Selection.GetSelected (out model, out iter)) {
						// Update values
						store.SetValue (iter, RepoNameCol, rep.Name);
						store.SetValue (iter, VcsName, rep.VersionControlSystem.Name);
						bool filled = (bool) store.GetValue (iter, FilledCol);
						if (filled && repoTree.GetRowExpanded (store.GetPath (iter))) {
							FullRepoNode (rep, iter);
							repoTree.ExpandRow (store.GetPath (iter), false);
						} else if (filled) {
							store.SetValue (iter, FilledCol, false);
							store.AppendValues (iter, null, "", "", true, "vc-repository");
						}
					}
					UpdateRepoDescription ();
				} finally {
					dlg.Destroy ();
					dlg.Dispose ();
				}
			}
		}

		Repository GetSelectedRepository ()
		{
			TreeIter iter;
			TreeModel model;
			if (repoTree.Selection.GetSelected (out model, out iter))
				return (Repository) store.GetValue (iter, RepositoryCol);
			return null;
		}

		private void OnTestExpandRow (object sender, Gtk.TestExpandRowArgs args)
		{
			bool filled = (bool) store.GetValue (args.Iter, FilledCol);
			Repository parent = (Repository) store.GetValue (args.Iter, RepositoryCol);
			if (!filled) {
				store.SetValue (args.Iter, FilledCol, true);
				FullRepoNode (parent, args.Iter);
			} else
				args.RetVal = false;
		}
		
		void FullRepoNode (Repository parent, TreeIter repoIter)
		{
			// Remove the dummy child
			TreeIter citer;
			store.IterChildren (out citer, repoIter);

			store.SetValue (citer, RepoNameCol, GettextCatalog.GetString ("Loading..."));

			loadingRepos.Add (FindRootRepo (repoIter));
			UpdateControls ();
			
			ThreadPool.QueueUserWorkItem (delegate {
				LoadRepoInfo (parent, repoIter, citer);
			});
		}
			
		Repository FindRootRepo (TreeIter iter)
		{
			TreeIter piter;
			while (store.IterParent (out piter, iter)) {
				iter = piter;
			}
			return (Repository) store.GetValue (iter, RepositoryCol);
		}
			
		void LoadRepoInfo (Repository parent, TreeIter piter, TreeIter citer)
		{
			IEnumerable repos = null;
			Exception ex = null;
			try {
				repos = parent.ChildRepositories;
			} catch (Exception e) {
				ex = e;
			}
				
			Gtk.Application.Invoke ((o, args) => {
				if (ex != null) {
					LoggingService.LogError ("Failed to load connected repositories.", ex.ToString ());
				}
				else {
					foreach (Repository rep in repos)
						LoadRepositories (rep, piter);
				}
				store.Remove (ref citer);
				loadingRepos.Remove (FindRootRepo (piter));
				UpdateControls ();
			});
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
				TreeIter piter;
				if (!store.IterParent (out piter, iter)) {
					Repository repo = FindRootRepo (iter);
					if (!loadingRepos.Contains (repo)) {
						buttonRemove.Sensitive = true;
						buttonEdit.Sensitive = true;
						return;
					}
				}
			}
			buttonRemove.Sensitive = false;
			buttonEdit.Sensitive = false;
		}

		protected virtual void OnButtonBrowseClicked(object sender, System.EventArgs e)
		{
			var dlg = new MonoDevelop.Components.SelectFolderDialog (GettextCatalog.GetString ("Select target directory"));
			if (dlg.Run ()) {
				defaultPath = dlg.SelectedFile;
				VersionControlDefaultPath.Value = defaultPath;
				AppendRelativePath ();
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
		
		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			if (notebook.Page == 0 && Repository != null) {
				if (!currentEditor.Validate ())
					return;
			}
			Respond (ResponseType.Ok);
		}

		protected virtual void OnPathChanged (object sender, EventArgs e)
		{
			AppendRelativePath ();
		}

		void AppendRelativePath ()
		{
			UrlBasedRepositoryEditor edit = currentEditor as UrlBasedRepositoryEditor;
			if (edit == null)
				return;

			// RelativePath always is at least '/'.
			if (edit.RelativePath == "/") {
				entryFolder.Text = defaultPath;
				return;
			}

			entryFolder.Text = defaultPath + edit.RelativePath.Replace ('/', System.IO.Path.DirectorySeparatorChar);
		}
	}
}
