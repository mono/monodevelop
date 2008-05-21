
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment.Gui
{
	internal partial class DeployDialog : Gtk.Dialog
	{
		ListStore store;
		List<PackageBuilder> builders = new List<PackageBuilder> ();
		PackageBuilder currentBuilder;
		Gtk.Widget currentEditor;
		ReadOnlyCollection<SolutionFolder> combineList;
		ReadOnlyCollection<PackagingProject> projectsList;
		SolutionItem defaultEntry;
		
		public DeployDialog (SolutionItem defaultEntry, bool createBuilderOnly)
		{
			this.Build();
			notebook.ShowTabs = false;
			this.defaultEntry = defaultEntry;
			
			if (createBuilderOnly) {
				vboxSaveProject.Hide ();
				checkSave.Active = true;
				checkSave.Hide ();
				saveSeparator.Hide ();
			}
			else {
				pageSave.Hide ();
				FillProjectSelectors ();
			}
			
			store = new ListStore (typeof(Gdk.Pixbuf), typeof(string), typeof(object));
			targetsTree.Model = store;
			
			targetsTree.HeadersVisible = false;
			Gtk.CellRendererPixbuf cr = new Gtk.CellRendererPixbuf();
			cr.Yalign = 0;
			targetsTree.AppendColumn ("", cr, "pixbuf", 0);
			targetsTree.AppendColumn ("", new Gtk.CellRendererText(), "markup", 1);
			
			targetsTree.Selection.Changed += delegate (object s, EventArgs a) {
				UpdateButtons ();
			};
			
			FillBuilders ();
			
			UpdateButtons ();
		}
		
		public PackageBuilder PackageBuilder {
			get { return currentBuilder; }
		}
		
		public bool SaveToProject {
			get { return checkSave.Active; }
		}
		
		public bool CreateNewProject {
			get { return radioCreateProject.Active; }
		}
		
		public SolutionFolder NewProjectSolution {
			get { return CreateNewProject ? combineList [comboCreateProject.Active] as SolutionFolder : null; }
		}
		
		public string NewProjectName {
			get { return entryProjectName.Text; }
		}
		
		public PackagingProject ExistingPackagingProject {
			get { return CreateNewProject ? null : projectsList [comboSelProject.Active] as PackagingProject; }
		}
		
		public string NewPackageName {
			get { return entrySaveName.Text; }
		}
		
		void FillBuilders ()
		{
			builders.Clear ();
			foreach (PackageBuilder builder in DeployService.GetPackageBuilders ()) {
				builders.Add (builder);
			}
			
			store.Clear ();
			foreach (PackageBuilder builder in builders) {
				Gdk.Pixbuf pix = MonoDevelop.Core.Gui.Services.Resources.GetIcon (builder.Icon, Gtk.IconSize.LargeToolbar);
				store.AppendValues (pix, builder.Description, builder);
			}
			
			if (builders.Count > 0)
				SelectBuilder (builders[0]);
		}

		void FillProjectSelectors ()
		{
			// Fill the combine list
			int n=0, sel=-1;
			combineList = IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllSolutionItems<SolutionFolder> ();
			foreach (SolutionFolder c in combineList) {
				string name = c.Name;
				SolutionFolder co = c;
				while (co.ParentFolder != null) {
					co = co.ParentFolder;
					name = co.Name + " / " + name;
				}
				comboCreateProject.AppendText (name);
				n++;
			}
			if (sel != -1)
				comboCreateProject.Active = sel;
			else
				comboCreateProject.Active = 0;
			
			// Fill the packaging project list
			projectsList = IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllSolutionItems<PackagingProject> ();
			if (projectsList.Count == 0) {
				radioAddProject.Sensitive = false;
			}
			else {
				foreach (PackagingProject p in projectsList) {
					string name = p.Name;
					SolutionFolder c = p.ParentFolder;
					while (c != null) {
						name = c.Name + " / " + name;
						c = c.ParentFolder;
					}
					comboSelProject.AppendText (name);
				}
				comboSelProject.Active = 0;
			}
		}
		
		void SelectBuilder (PackageBuilder builder)
		{
			if (builder == null)
				return;
			Gtk.TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					PackageBuilder t = (PackageBuilder) store.GetValue (iter, 2);
					if (t == builder) {
						targetsTree.Selection.SelectIter (iter);
						return;
					}
				} while (store.IterNext (ref iter));
			}
		}
		
		PackageBuilder GetBuilderSelection ()
		{
			Gtk.TreeModel model;
			Gtk.TreeIter iter;
			
			if (targetsTree.Selection.GetSelected (out model, out iter)) {
				return (PackageBuilder) store.GetValue (iter, 2);
			} else
				return null;
		}
		
		void UpdateBuilderEditor ()
		{
			if (currentEditor != null) {
				editorBox.Remove (currentEditor);
				currentEditor.Destroy ();
			}
			
			currentEditor = new PackageBuilderEditor (currentBuilder);
			editorBox.Child = currentEditor;
			editorBox.ShowAll ();
		}
		
		bool ValidatePage ()
		{
			string msg = null;
			switch (notebook.Page) {
			case 0:
				if (GetBuilderSelection () == null)
					msg = GettextCatalog.GetString ("Please select a package type.");
				else if (radioCreateProject.Active && entryProjectName.Text.Length == 0)
					msg = GettextCatalog.GetString ("Project name not provided.");
				else {
					currentBuilder = GetBuilderSelection ();
					entryTree.Fill (currentBuilder, defaultEntry);
					defaultEntry = null;
				}
				break;
			case 1:
				if (entryTree.GetSelectedEntry () == null)
					msg = GettextCatalog.GetString ("Please select a project or solution.");
				else {
					currentBuilder.SetSolutionItem (entryTree.GetSelectedEntry (), entryTree.GetSelectedChildren ());
					UpdateBuilderEditor ();
				}
				break;
			case 2:
				msg = currentBuilder.Validate ();
				if (msg == null)
					entrySaveName.Text = currentBuilder.DefaultName;
				break;
			case 3:
				if (entrySaveName.Text.Length == 0)
					msg = GettextCatalog.GetString ("Package name not provided.");
				if (vboxSaveProject.Visible) {
					if (radioCreateProject.Active) {
						if (entryProjectName.Text.Length == 0)
							msg = GettextCatalog.GetString ("Project name not provided.");
						else if (comboCreateProject.Active == -1)
							msg = GettextCatalog.GetString ("Solution where to create the project not selected.");
					}
					else if (comboSelProject.Active == -1) {
						msg = GettextCatalog.GetString ("Packaging project not selected.");
					}
				}
				break;
			}
			if (msg != null) {
				MonoDevelop.Core.Gui.MessageService.ShowError (this, msg);
				return false;
			}
			else
				return true;
		}

		protected virtual void OnButtonBackClicked(object sender, System.EventArgs e)
		{
			if (notebook.Page > 0)
				notebook.Page--;
			UpdateButtons ();
		}

		protected virtual void OnButtonNextClicked(object sender, System.EventArgs e)
		{
			if (!ValidatePage ())
				return;
			
			if (buttonNext.Label != Stock.GoForward) {
				this.Respond (ResponseType.Ok);
				return;
			}
			
			if (notebook.Page < notebook.NPages - 1)
				notebook.Page++;
			UpdateButtons ();
		}

		protected virtual void OnNotebookSelectPage(object o, Gtk.SelectPageArgs args)
		{
			UpdateButtons ();
		}
		
		void UpdateButtons ()
		{
			buttonBack.Sensitive = notebook.Page > 0 && notebook.GetNthPage (notebook.Page - 1).Visible;
			
			if (notebook.Page < notebook.NPages - 1 && notebook.GetNthPage (notebook.Page + 1).Visible) {
				buttonNext.Label = Stock.GoForward;
				buttonNext.UseStock = true;
			} else {
				buttonNext.Label = GettextCatalog.GetString ("Create");
				buttonNext.UseStock = false;
			}
			
			if (vboxSaveProject.Visible) {
				tableNewProject.Sensitive = radioCreateProject.Active;
				boxAddProject.Sensitive = radioAddProject.Active;
			}
		}

		protected virtual void OnCheckSaveClicked(object sender, System.EventArgs e)
		{
			pageSave.Visible = checkSave.Active;
			UpdateButtons ();
		}

		protected virtual void OnButtonPublishClicked(object sender, System.EventArgs e)
		{
			if (ValidatePage ())
				this.Respond (ResponseType.Ok);
		}

		protected virtual void OnRadioCreateProjectClicked(object sender, System.EventArgs e)
		{
			UpdateButtons ();
		}

		protected virtual void OnRadioAddProjectClicked(object sender, System.EventArgs e)
		{
			UpdateButtons ();
		}
	}
}
