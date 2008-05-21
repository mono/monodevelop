//  NewFileDialog.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using Gtk;
using MonoDevelop.Components;
using IconView = MonoDevelop.Components.IconView;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	/// <summary>
	///  This class is for creating a new "empty" file
	/// </summary>
	internal partial class NewFileDialog : Dialog
	{
		List<TemplateItem> alltemplates      = new List<TemplateItem> ();
		List<Category> categories            = new List<Category> ();
		Hashtable icons                      = new Hashtable ();
		Dictionary<string, bool> activeLangs = new Dictionary<string, bool> ();

		PixbufList cat_imglist;

		TreeStore catStore;
		
		// Add To Project widgets
		WorkspaceItem solution;
		string[] projectNames;
		Project[] projectRefs;
		
		Project parentProject;
		string basePath;
		
		string userEditedEntryText = null;
		string previousDefaultEntryText = null;
		
		public NewFileDialog (Project parentProject, string basePath) : base ()
		{
			Build ();
			this.parentProject = parentProject;
			this.basePath = basePath;
			
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.BorderWidth = 6;
			this.HasSeparator = false;
			
			InitializeComponents ();
			
			nameEntry.GrabFocus ();
		}
		
		void InitializeDialog (bool update)
		{
			if (update) {
				alltemplates.Clear ();
				categories.Clear ();
				catStore.Clear ();
				icons.Clear ();
				activeLangs.Clear ();
			}
			
			InitializeTemplates ();
			
			if (update) {
				iconView.Clear ();
				InitializeView ();
			}
		}
		
		public override void Dispose ()
		{
			Destroy ();
			base.Dispose ();
		}
		
		void InitializeView()
		{
			PixbufList smalllist  = new PixbufList();
			PixbufList imglist    = new PixbufList();
			
			smalllist.Add(Services.Resources.GetBitmap("md-empty-file-icon"));
			imglist.Add(Services.Resources.GetBitmap("md-empty-file-icon"));
			
			int i = 0;
			Hashtable tmp = new Hashtable(icons);
			foreach (DictionaryEntry entry in icons) {
				Gdk.Pixbuf bitmap = Services.Resources.GetBitmap(entry.Key.ToString(), Gtk.IconSize.LargeToolbar);
				if (bitmap != null) {
					smalllist.Add(bitmap);
					imglist.Add(bitmap);
					tmp[entry.Key] = ++i;
				} else {
					LoggingService.LogError (GettextCatalog.GetString ("Can't load bitmap {0} using default", entry.Key.ToString ()));
				}
			}
			
			icons = tmp;
			
			InsertCategories(TreeIter.Zero, categories);
			
			//select the most recently selected category (with a few fallbacks)
			string lastSelected = PropertyService.Get<string> (GetCategoryPropertyKey (parentProject), "General");
			TreeIter iterToSelect;
			if (FindCatIter (lastSelected, out iterToSelect)
			    || FindCatIter ("Misc", out iterToSelect)
			    || catStore.GetIterFirst (out iterToSelect)) {
				catView.Selection.SelectIter (iterToSelect);
			}
		}
		
		static string GetCategoryPropertyKey (Project proj)
		{
			string key = "Dialogs.NewFileDialog.LastSelectedCategory";
			if (proj != null) {
				key += "." + proj.ProjectType;
				if (proj is DotNetProject)
					key += "." + ((DotNetProject)proj).LanguageName;
			}
			return key;
		}
		
		bool FindCatIter (string catPath, out TreeIter iter)
		{
			string[] cats = catPath.Split ('/');
			iter = TreeIter.Zero;
			
			TreeIter nextIter;
			if (!catStore.GetIterFirst (out nextIter))
				return false;
			
			for (int i = 0; i < cats.Length; i++) {
				if (FindCategoryAtCurrentLevel (cats[i], ref nextIter)) {
					iter = nextIter;
					if (i >= cats.Length - 1 || !catStore.IterChildren (out nextIter, nextIter))
						return true;
				}
			}
			return false;
		}
		
		bool FindCategoryAtCurrentLevel (string category, ref TreeIter iter)
		{
			TreeIter trial = iter;
			do {
				string val = (string)catStore.GetValue (trial, 0);
				if (val == category) {
					iter = trial;
					return true;
				}
			} while (catStore.IterNext (ref trial));
			return false;
		}
		
		string GetCatPath (TreeIter iter)
		{
			TreeIter currentIter = iter;
			string path = (string) catStore.GetValue (currentIter, 0);
			while (catStore.IterParent (out currentIter, currentIter)) {
				path = ((string) catStore.GetValue (currentIter, 0)) + "/" + path;
			}
			return path;
		}
		
		void InsertCategories(TreeIter node, List<Category> catarray)
		{
			foreach (Category cat in catarray) {
				TreeIter cnode;
				if (node.Equals(Gtk.TreeIter.Zero)) {
					cnode = catStore.AppendValues (cat.Name, cat.Categories, cat.Templates, cat_imglist[1]);
				} else {
					cnode = catStore.AppendValues (node, cat.Name, cat.Categories, cat.Templates, cat_imglist[1]);
				}
				if (cat.Categories.Count > 0)
					InsertCategories (cnode, cat.Categories);
			}
		}
		
		public void SelectTemplate (string id)
		{
			TreeIter iter;
			catStore.GetIterFirst (out iter);
			SelectTemplate (iter, id);
		}
		
		public bool SelectTemplate (TreeIter iter, string id)
		{
			do {
				foreach (TemplateItem item in (List<TemplateItem>)(catStore.GetValue (iter, 2))) {
					if (item.Template.Id == id) {
						catView.ExpandToPath (catStore.GetPath (iter));
						catView.Selection.SelectIter (iter);
						CategoryChange (null,null);
						iconView.CurrentlySelected = item;
						return true;
					}
				}
				
				TreeIter citer;
				if (catStore.IterChildren (out citer, iter)) {
					do {
						if (SelectTemplate (citer, id))
							return true;
					} while (catStore.IterNext (ref citer));
				}
				
			} while (catStore.IterNext (ref iter));
			return false;
		}
		
		Category GetCategory (string categoryname)
		{
			return GetCategory (categories, categoryname);
		}
		
		Category GetCategory (List<Category> catList, string categoryname)
		{
			foreach (Category category in catList) {
				if (category.Name == categoryname) {
					return category;
				}
			}
			Category newcategory = new Category(categoryname);
			catList.Add(newcategory);
			return newcategory;
		}
		
		void InitializeTemplates()
		{
			Project project = null;
			
			if (!boxProject.Visible || projectAddCheckbox.Active)
				project = parentProject;
			
			foreach (FileTemplate template in FileTemplate.GetFileTemplates (project, basePath)) {
				if (template.Icon != null) {
					icons[template.Icon] = 0; // "create template icon"
				}
				List<string> langs = template.GetCompatibleLanguages (project, basePath);
				if (langs != null) {
					foreach (string language in langs) {					
						AddTemplate (new TemplateItem (template, language), language);
						//count the number of active languages
						activeLangs[language] = true; 
					}
				}
				
			}
		}
		
		void AddTemplate (TemplateItem titem, string templateLanguage)
		{
			Project project = null;
			Category cat = null;
			
			if (!boxProject.Visible || projectAddCheckbox.Active)
				project = parentProject;
			
			if (project != null) {
				if ((templateLanguage != "") && (activeLangs.Count > 2) ) {
					// The template requires a language, but the project does not have a single fixed
					// language type (plus empty match), so create a language category
					cat = GetCategory (templateLanguage);
					cat = GetCategory (cat.Categories, titem.Template.Category);
				} else {
					cat = GetCategory (titem.Template.Category);
				}
			} else {
				if (templateLanguage != "") {
					// The template requires a language, but there is no current language set, so
					// create a category for it
					cat = GetCategory (templateLanguage);
					cat = GetCategory (cat.Categories, titem.Template.Category);
				} else {
					cat = GetCategory (titem.Template.Category);
				}
			}

			cat.Templates.Add (titem); 
			
			if (cat.Selected == false && titem.Template.WizardPath == null) {
				cat.Selected = true;
			}
			
			if (!cat.HasSelectedTemplate && titem.Template.Files.Count == 1) {
				if (((FileDescriptionTemplate)titem.Template.Files[0]).Name.StartsWith("Empty")) {
					//titem.Selected = true;
					cat.HasSelectedTemplate = true;
				}
			}
			
			alltemplates.Add(titem);
		}

		//tree view event handler for double-click
		//toggle the expand collapse methods.
		void CategoryActivated(object sender,RowActivatedArgs args)
		{
			if (!catView.GetRowExpanded(args.Path)) {
				catView.ExpandRow(args.Path,false);
			} else {
				catView.CollapseRow(args.Path);
			}
		}
		
		// tree view event handlers
		void CategoryChange(object sender, EventArgs e)
		{
			TreeModel mdl;
			TreeIter iter;
			if (catView.Selection.GetSelected (out mdl, out iter)) {
				FillCategoryTemplates (iter);
				okButton.Sensitive = false;
			}
		}
		
		void FillCategoryTemplates (TreeIter iter)
		{
			iconView.Clear ();
			foreach (TemplateItem item in (List<TemplateItem>)(catStore.GetValue (iter, 2))) {
				iconView.AddIcon (new Gtk.Image (Services.Resources.GetBitmap (item.Template.Icon, Gtk.IconSize.Dnd)), item.Name, item);
			}
		}
		
		// list view event handlers
		void SelectedIndexChange (object sender, EventArgs e)
		{
			UpdateOkStatus ();
		}
		
		void NameChanged (object sender, EventArgs e)
		{
			UpdateOkStatus ();
		}
		
		void UpdateOkStatus ()
		{
			try {
				TemplateItem sel = (TemplateItem) iconView.CurrentlySelected;
				if (sel == null) {
					okButton.Sensitive = false;
					return;
				}
				
				FileTemplate item = sel.Template;

				if (item != null) {
					infoLabel.Text = item.Description;
					
					//desensitise the text entry if the name is fixed
					//careful to store user-entered text so we can replace it if they change their selection
					if (item.IsFixedFilename) {
						if (userEditedEntryText == null)
							userEditedEntryText = nameEntry.Text;
						nameEntry.Text = item.DefaultFilename;
						nameEntry.Sensitive = false;
					} else {
						if (userEditedEntryText != null) {
							nameEntry.Text = userEditedEntryText;
							userEditedEntryText = null;
						}
						nameEntry.Sensitive = true;
					}
					
					//fill in a default name if text entry is empty or contains a default name
					if ((string.IsNullOrEmpty (nameEntry.Text) || (previousDefaultEntryText == nameEntry.Text))
					    && !string.IsNullOrEmpty (item.DefaultFilename) ) {
						nameEntry.Text = item.DefaultFilename;
						previousDefaultEntryText = item.DefaultFilename;
					}
					
					okButton.Sensitive = item.IsValidName (nameEntry.Text, sel.Language);
				} else {				
					nameEntry.Sensitive = true;
					okButton.Sensitive = false;
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}
		
		// button events
		
		protected void CheckedChange(object sender, EventArgs e)
		{
			//((ListView)ControlDictionary["templateListView"]).View = ((RadioButton)ControlDictionary["smallIconsRadioButton"]).Checked ? View.List : View.LargeIcon;
		}
		
		public event EventHandler OnOked;	
		
		void OpenEvent(object sender, EventArgs e)
		{
			if (!okButton.Sensitive)
				return;
			
			//FIXME: we need to set this up
			//PropertyService.Set("Dialogs.NewProjectDialog.LargeImages", ((RadioButton)ControlDictionary["largeIconsRadioButton"]).Checked);
			TreeIter selectedIter;
			if (catView.Selection.GetSelected (out selectedIter))
				PropertyService.Set (GetCategoryPropertyKey (parentProject), GetCatPath (selectedIter));
			
			if (iconView.CurrentlySelected != null && nameEntry.Text.Length > 0) {
				TemplateItem titem = (TemplateItem) iconView.CurrentlySelected;
				FileTemplate item = titem.Template;
				Project project = null;
				string path = null;
				
				if (!boxProject.Visible || projectAddCheckbox.Active) {
					project = parentProject;
					path = basePath;
				}
				
				try {
					if (!item.Create (project, path, titem.Language, nameEntry.Text))
						return;
				} catch (Exception ex) {
					MessageService.ShowException (ex);
					return;
				}

				if (OnOked != null)
					OnOked (null, null);
				Respond (Gtk.ResponseType.Ok);
				Destroy ();
			}
		}
		
		/// <summary>
		///  Represents a category
		/// </summary>
		class Category
		{
			List<Category> categories = new List<Category>();
			List<TemplateItem> templates  = new List<TemplateItem>();
			string name;
			
			public bool Selected = false;
			public bool HasSelectedTemplate = false;
			
			public Category(string name)
			{
				this.name = name;
				//ImageIndex = 1;
			}
			
			public string Name {
				get {
					return name;
				}
			}
			
			public List<Category> Categories {
				get {
					return categories;
				}
			}
			
			public List<TemplateItem> Templates {
				get {
					return templates;
				}
			}
		}
		
		/// <summary>
		///  Represents a new file template
		/// </summary>
		class TemplateItem
		{
			FileTemplate template;
			string name;
			string language;
			
			public TemplateItem (FileTemplate template, string language)
			{
				this.template = template;
				this.language =  language;
				this.name = template.Name;
			}

			public string Name {
				get {
					return name;
				}
			}
			
			public FileTemplate Template {
				get {
					return template;
				}
			}
			
			public string Language {
				get { return language; }
			}
		}

		void cancelClicked (object o, EventArgs e) {
			Destroy ();
		}
		
		void AddToProjectToggled (object o, EventArgs e)
		{
			projectAddCombo.Sensitive = projectAddCheckbox.Active;
			projectPathLabel.Sensitive = projectAddCheckbox.Active;
			projectFolderEntry.Sensitive = projectAddCheckbox.Active;
			
			TemplateItem titem = (TemplateItem) iconView.CurrentlySelected;
			
			InitializeDialog (true);
			
			if (titem != null)
				SelectTemplate (titem.Template.Id);
		}
		
		void AddToProjectComboChanged (object o, EventArgs e)
		{
			int which = projectAddCombo.Active;
			Project project = projectRefs [which];
			
			if (project != null) {
				if (basePath == null || basePath == String.Empty ||
				    (parentProject != null && basePath == parentProject.BaseDirectory)) {
					basePath = project.BaseDirectory;
					projectFolderEntry.Path = basePath;
				}
				
				parentProject = project;
				
				InitializeDialog (true);
			}
		}
		
		void AddToProjectPathChanged (object o, EventArgs e)
		{
			basePath = projectFolderEntry.Path;
		}
		
		void InitializeComponents()
		{
			catStore = new Gtk.TreeStore (typeof(string), typeof(List<Category>), typeof(List<TemplateItem>), typeof(Gdk.Pixbuf));
			catStore.SetSortColumnId (0, SortType.Ascending);
			
			catView.Model = catStore;

			TreeViewColumn catColumn = new TreeViewColumn ();
			catColumn.Title = "categories";
			
			CellRendererText cat_text_render = new CellRendererText ();
			catColumn.PackStart (cat_text_render, true);
			catColumn.AddAttribute (cat_text_render, "text", 0);

			catView.AppendColumn (catColumn);

			okButton.Clicked += new EventHandler (OpenEvent);
			cancelButton.Clicked += new EventHandler (cancelClicked);

			nameEntry.Changed += new EventHandler (NameChanged);
			nameEntry.Activated += new EventHandler (OpenEvent);
			
			ReadOnlyCollection<Project> projects = null;
			if (parentProject == null)
				projects = IdeApp.Workspace.GetAllProjects ();
			
			if (projects != null) {
				Project curProject = IdeApp.ProjectOperations.CurrentSelectedProject;
				
				boxProject.Visible = true;
				projectAddCheckbox.Active = curProject != null;
				projectAddCheckbox.Toggled += new EventHandler (AddToProjectToggled);
				
				projectNames = new string [projects.Count];
				projectRefs = new Project [projects.Count];
				int i = 0;
				
				bool singleSolution = IdeApp.Workspace.Items.Count == 1 && IdeApp.Workspace.Items [0] is Solution;
				
				foreach (Project project in projects) {
					projectRefs[i] = project;
					if (singleSolution)
						projectNames[i++] = project.Name;
					else
						projectNames[i++] = project.ParentSolution.Name + "/" + project.Name;
				}
				
				Array.Sort (projectNames, projectRefs);
				i = Array.IndexOf (projectRefs, curProject);
				
				foreach (string pn in projectNames)
					projectAddCombo.AppendText (pn);
				
				projectAddCombo.Active = i != -1 ? i : 0;
				projectAddCombo.Sensitive = projectAddCheckbox.Active;
				projectAddCombo.Changed += new EventHandler (AddToProjectComboChanged);
				
				projectPathLabel.Sensitive = projectAddCheckbox.Active;
				projectFolderEntry.Sensitive = projectAddCheckbox.Active;
				if (curProject != null)
					projectFolderEntry.Path = curProject.BaseDirectory;
				projectFolderEntry.PathChanged += new EventHandler (AddToProjectPathChanged);
				
				if (curProject != null) {
					basePath = curProject.BaseDirectory;
					parentProject = curProject;
				}
			}
			else {
				boxProject.Visible = false;
			}
			
			cat_imglist = new PixbufList();
			cat_imglist.Add(Services.Resources.GetBitmap("md-open-folder"));
			cat_imglist.Add(Services.Resources.GetBitmap("md-closed-folder"));
			catView.Selection.Changed += new EventHandler (CategoryChange);
			catView.RowActivated += new RowActivatedHandler (CategoryActivated);
			iconView.IconSelected += new EventHandler(SelectedIndexChange);
			iconView.IconDoubleClicked += new EventHandler(OpenEvent);
			InitializeDialog (false);
			InitializeView ();
			UpdateOkStatus ();
		}
	}
}
