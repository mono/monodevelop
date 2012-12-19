//  NewProjectDialog.cs
//
//   Todd Berman  <tberman@off.net>
//   Lluis Sanchez Gual <lluis@novell.com>
//   Viktoria Dudka  <viktoriad@remobjects.com>
//
// Copyright (c) 2004 Todd Berman
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2009 RemObjects Software
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Collections;
using System.IO;
using System.Text;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;

using MonoDevelop.Components;
using IconView = MonoDevelop.Components.IconView;
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Components;
using System.Reflection;

namespace MonoDevelop.Ide.Projects {
	/// <summary>
	/// This class displays a new project dialog and sets up and creates a a new project,
	/// the project types are described in an XML options file
	/// </summary>
	public partial class NewProjectDialog: Gtk.Dialog
	{
		ArrayList alltemplates = new ArrayList();
		List<Category> categories = new List<Category> ();
		
		TemplateView templateView;
		TreeStore catStore;
		
		bool openSolution;
		string basePath;
		bool newSolution;
		string lastName = "";
		ProjectTemplate selectedItem;
		SolutionItem currentEntry;
		SolutionFolder parentFolder;
		CombineEntryFeatureSelector featureList;
		IWorkspaceFileObject newItem;
		Category recentCategory;
		List<string> recentIds = new List<string> ();
			
		public NewProjectDialog (SolutionFolder parentFolder, bool openCombine, string basePath)
		{
			Build ();
			featureList = new CombineEntryFeatureSelector ();
			vbox5.PackEnd (featureList, true, true, 0);
			vbox5.ShowAll ();
			notebook.Page = 0;
			notebook.ShowTabs = false;
			
			this.parentFolder = parentFolder;
			this.basePath = basePath;
			this.newSolution = parentFolder == null;
			this.openSolution = openCombine;
			TransientFor = IdeApp.Workbench.RootWindow;
			Title = newSolution ? GettextCatalog.GetString ("New Solution") : GettextCatalog.GetString ("New Project");
			
			InitializeTemplates ();
			
			if (!newSolution) {
				txt_subdirectory.Hide ();
				chk_combine_directory.Active = false;
				chk_combine_directory.Hide ();
				lbl_subdirectory.Hide ();
			}

			TreeIter iter;
			ExpandCategory ("C#", out iter);
		}
		
		public void SelectTemplate (string id)
		{
			TreeIter iter;
			catStore.GetIterFirst (out iter);
			SelectTemplate (iter, id);
		}
		
		ProjectTemplate GetTemplate (string id)
		{
			foreach (ProjectTemplate template in ProjectTemplate.ProjectTemplates) {
				if (template.Id == id)
					return template;
			}
			return null;
		}
		
		bool SelectTemplate (TreeIter iter, string id)
		{
			do {
				foreach (TemplateItem item in ((Category)catStore.GetValue (iter, 1)).Templates) {
					if (item.Template.Id == id) {
						lst_template_types.Selection.SelectIter (iter);
						templateView.CurrentlySelected = item.Template;
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

		bool ExpandCategory (string category, out TreeIter result)
		{
			string[] cats = category.Split ('/');
			
			TreeIter iter;
			if (!catStore.GetIterFirst (out iter)) {
				result = TreeIter.Zero;
				return false;
			}
			
			TreeIter nextIter = iter;
			for (int i = 0; i < cats.Length; i++) {
				if (FindCategoryAtCurrentLevel (cats[i], ref nextIter)) {
					iter = nextIter;
					if (i >= cats.Length - 1 || !catStore.IterChildren (out nextIter, nextIter))
						break;
				} else if (i == 0) {
					FindCategoryAtCurrentLevel ("C#", ref iter);
					break;
				}
			}
			
			lst_template_types.ExpandToPath (catStore.GetPath (iter));
			result = iter;
			return true;
		}
		
		void SelectCategory (string category)
		{
			TreeIter iter;
			if (ExpandCategory (category, out iter))
				lst_template_types.Selection.SelectIter (iter);
		}
		
		void InitializeView()
		{
			InsertCategories (TreeIter.Zero, categories);
			if (recentCategory.Templates.Count == 0)
				SelectCategory (PropertyService.Get<string> ("Dialogs.NewProjectDialog.LastSelectedCategory", "C#"));
			else
				SelectTemplate (recentCategory.Templates [0].Template.Id);
			ShowAll ();
		}
		
		protected override void OnDestroyed ()
		{
			if (catStore != null) {
				catStore.Dispose ();
				catStore = null;
			}
			
			if (catColumn != null) {
				catColumn.Destroy ();
				catColumn = null;
			}
			
			if (cat_text_render != null) {
				cat_text_render.Destroy ();
				cat_text_render = null;
			}
			base.OnDestroyed ();
		}
		
		
		
		Category GetCategory (string categoryname)
		{
			return GetCategory (categories, categoryname);
		}
		
		Category GetCategory (List<Category> catList, string categoryname)
		{
			int i = categoryname.IndexOf ('/');
			if (i != -1) {
				string cn = categoryname.Substring (0, i).Trim ();
				Category rootCat = GetCategory (catList, cn);
				return GetCategory (rootCat.Categories, categoryname.Substring (i+1));
			}
			
			foreach (Category category in catList) {
				if (category.Name == categoryname)
					return category;
			}
			Category newcategory = new Category (categoryname);
			catList.Add(newcategory);
			return newcategory;
		}
		
		
		
		
		
		string GetValidDir (string name)
		{
			name = name.Trim ();
			StringBuilder sb = new StringBuilder ();
			for (int n=0; n<name.Length; n++) {
				char c = name [n];
				if (Array.IndexOf (System.IO.Path.GetInvalidPathChars(), c) != -1)
					continue;
				if (c == System.IO.Path.DirectorySeparatorChar || c == System.IO.Path.AltDirectorySeparatorChar || c == System.IO.Path.VolumeSeparatorChar)
					continue;
				sb.Append (c);
			}
			return sb.ToString ();
		}
		
		bool CreateSolutionDirectory {
			get { return chk_combine_directory.Active && chk_combine_directory.Sensitive; }
		}

		string SolutionLocation {
			get {
				if (CreateSolutionDirectory)
					return System.IO.Path.Combine (entry_location.Path, GetValidDir (txt_subdirectory.Text));
				else
					return System.IO.Path.Combine (entry_location.Path, GetValidDir (txt_name.Text));
			}
		}
		
		string ProjectLocation {
			get {
				string path = entry_location.Path;
				if (CreateSolutionDirectory)
					path = System.IO.Path.Combine (path, GetValidDir (txt_subdirectory.Text));
				
				return System.IO.Path.Combine (path, GetValidDir (txt_name.Text));
			}
		}

		public IWorkspaceObject NewItem {
			get {
				return newItem;
			}
		}
		
		protected void SolutionCheckChanged (object sender, EventArgs e)
		{
			if (CreateSolutionDirectory && txt_subdirectory.Text == "")
				txt_subdirectory.Text = txt_name.Text;

			PathChanged (null, null);
		}
		
		protected void NameChanged (object sender, EventArgs e)
		{
			if (CreateSolutionDirectory && txt_subdirectory.Text == lastName)
				txt_subdirectory.Text = txt_name.Text;
				
			lastName = txt_name.Text;
			PathChanged (null, null);
		}
		
		void PathChanged (object sender, EventArgs e)
		{
			ActivateIfReady ();
			lbl_will_save_in.Text = GettextCatalog.GetString("Project will be saved at") + " " + ProjectLocation;
		}
		
		void OpenEvent (object sender, EventArgs e)
		{
			if (!btn_new.Sensitive)
				return;
			
			if (notebook.Page == 0) {
				if (!CreateProject ())
					return;
				
				Solution parentSolution = null;
				
				if (parentFolder == null) {
					WorkspaceItem item = (WorkspaceItem) newItem;
					parentSolution = item as Solution;
					if (parentSolution != null) {
						if (parentSolution.RootFolder.Items.Count > 0)
							currentEntry = parentSolution.RootFolder.Items [0] as SolutionItem;
						parentFolder = parentSolution.RootFolder;
					}
				} else {
					SolutionItem item = (SolutionItem) newItem;
					parentSolution = parentFolder.ParentSolution;
					currentEntry = item;
				}
				
				if (btn_new.Label == Gtk.Stock.GoForward) {
					// There are features to show. Go to the next page
					if (currentEntry != null) {
						try {
							featureList.Fill (parentFolder, currentEntry, SolutionItemFeatures.GetFeatures (parentFolder, currentEntry));
						}
						catch (Exception ex) {
							LoggingService.LogError (ex.ToString ());
						}
					}
					notebook.Page++;
					btn_new.Label = Gtk.Stock.Ok;
					return;
				}
				
			} else {
				// Already in fetatures page
				if (!featureList.Validate ())
					return;
			}
			
			// New combines (not added to parent combines) already have the project as child.
			if (!newSolution) {
				// Make sure the new item is saved before adding. In this way the
				// version control add-in will be able to put it under version control.
				if (currentEntry is SolutionEntityItem) {
					// Inherit the file format from the solution
					SolutionEntityItem eitem = (SolutionEntityItem) currentEntry;
					eitem.FileFormat = parentFolder.ParentSolution.FileFormat;
					IdeApp.ProjectOperations.Save (eitem);
				}
				parentFolder.AddItem (currentEntry, true);
			}

			if (notebook.Page == 1)
				featureList.ApplyFeatures ();
			
			if (parentFolder != null)
				IdeApp.ProjectOperations.Save (parentFolder.ParentSolution);
			else
				IdeApp.ProjectOperations.Save (newItem);
			
			if (openSolution)
				selectedItem.OpenCreatedSolution();
			Respond (ResponseType.Ok);
		}
		
		bool CreateProject ()
		{
			if (templateView.CurrentlySelected != null) {
				PropertyService.Set ("Dialogs.NewProjectDialog.LastSelectedCategory",  ((ProjectTemplate)templateView.CurrentlySelected).Category);
				recentIds.Remove (templateView.CurrentlySelected.Id);
				recentIds.Insert (0, templateView.CurrentlySelected.Id);
				if (recentIds.Count > 15)
					recentIds.RemoveAt (recentIds.Count - 1);
				string strRecent = string.Join (",", recentIds.ToArray ());
				PropertyService.Set ("Dialogs.NewProjectDialog.RecentTemplates", strRecent);
				PropertyService.SaveProperties ();
				//PropertyService.Set("Dialogs.NewProjectDialog.LargeImages", ((RadioButton)ControlDictionary["largeIconsRadioButton"]).Checked);
			}
			
			string solution = txt_subdirectory.Text;
			string name     = txt_name.Text;
			string location = ProjectLocation;

			if(solution.Equals("")) solution = name; //This was empty when adding after first combine
			
			if (
				!FileService.IsValidPath (solution) || 
			    !FileService.IsValidFileName(name) ||
				name.IndexOf (' ') >= 0 ||
				!FileService.IsValidPath(location))
			{
				MessageService.ShowError (GettextCatalog.GetString ("Illegal project name.\nOnly use letters, digits, '.' or '_'."));
				return false;
			}

			if (parentFolder != null && parentFolder.ParentSolution.FindProjectByName (name) != null) {
				MessageService.ShowError (GettextCatalog.GetString ("A Project with that name is already in your Project Space"));
				return false;
			}
			
			PropertyService.Set (
				"MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.AutoCreateProjectSubdir",
				CreateSolutionDirectory);
			
			if (templateView.CurrentlySelected == null || name.Length == 0)
				return false;
				
			ProjectTemplate item = (ProjectTemplate) templateView.CurrentlySelected;
			
			try {
				System.IO.Directory.CreateDirectory (location);
			} catch (IOException) {
				MessageService.ShowError (GettextCatalog.GetString ("Could not create directory {0}. File already exists.", location));
				return false;
			} catch (UnauthorizedAccessException) {
				MessageService.ShowError (GettextCatalog.GetString ("You do not have permission to create to {0}", location));
				return false;
			}
			
			
			try {
				ProjectCreateInformation cinfo = CreateProjectCreateInformation ();
				if (newSolution)
					newItem = item.CreateWorkspaceItem (cinfo);
				else
					newItem = item.CreateProject (parentFolder, cinfo);
			} catch (UserException ex) {
				MessageService.ShowError (ex.Message, ex.Details);
				return false;
			} catch (Exception ex) {
				MessageService.ShowException (ex, GettextCatalog.GetString ("The project could not be created"));
				return false;
			}
			selectedItem = item;
			return true;
		}
		
		ProjectCreateInformation CreateProjectCreateInformation ()
		{
			ProjectCreateInformation cinfo = new ProjectCreateInformation ();
			cinfo.SolutionPath = FileService.ResolveFullPath (SolutionLocation);
			cinfo.ProjectBasePath = FileService.ResolveFullPath (ProjectLocation);
			cinfo.ProjectName = txt_name.Text;
			cinfo.SolutionName = CreateSolutionDirectory ? txt_subdirectory.Text : txt_name.Text;
			cinfo.ParentFolder = parentFolder;
			cinfo.ActiveConfiguration = IdeApp.Workspace.ActiveConfiguration;
			return cinfo;
		}

		// icon view event handlers
		void SelectedIndexChange(object sender, EventArgs e)
		{
			try {
				btn_new.Sensitive = true;
				txt_name.Sensitive = true;
				txt_subdirectory.Sensitive = true;
				chk_combine_directory.Sensitive = true;
				entry_location.Sensitive = true;
				
				if (templateView.CurrentlySelected != null) {
					ProjectTemplate ptemplate = (ProjectTemplate) templateView.CurrentlySelected;
					lbl_template_descr.Text = StringParserService.Parse (ptemplate.Description);
					labelTemplateTitle.Markup = "<b>" + GLib.Markup.EscapeText (ptemplate.Name) + "</b>";
					
					if (ptemplate.SolutionDescriptor.EntryDescriptors.Length == 0) {
						txt_subdirectory.Sensitive = false;
						chk_combine_directory.Sensitive = false;
						lbl_subdirectory.Sensitive = false;
						btn_new.Label = Gtk.Stock.Ok;
					} else {
						lbl_subdirectory.Sensitive = true;
						txt_subdirectory.Text = txt_name.Text;
						
						ProjectCreateInformation cinfo = CreateProjectCreateInformation ();
						if (ptemplate.HasItemFeatures (parentFolder, cinfo))
							btn_new.Label = Gtk.Stock.GoForward;
						else
							btn_new.Label = Gtk.Stock.Ok;
					}
				}
				else {
					lbl_template_descr.Text = String.Empty;
					labelTemplateTitle.Text = "";
				}
				
				PathChanged (null, null);
				
				btn_new.GrabDefault ();
			} catch (Exception ex) {
				txt_name.Sensitive = false;
				btn_new.Sensitive = false;
				txt_subdirectory.Sensitive = false;
				chk_combine_directory.Sensitive = false;
				entry_location.Sensitive = false;
				
				while (ex is TargetInvocationException)
						ex = ((TargetInvocationException) ex).InnerException;
				
				if (ex is UserException) {
					var user = (UserException) ex;
					MessageService.ShowError (user.Message, user.Details);
				} else {
					MessageService.ShowException (ex);
				};
			}
		}
		
		protected void cancelClicked (object o, EventArgs e)
		{
			Respond (ResponseType.Cancel);
		}
		
		void ActivateIfReady ()
		{
			if (templateView.CurrentlySelected == null || !txt_name.Sensitive || txt_name.Text.Trim () == "" || (txt_subdirectory.Sensitive && chk_combine_directory.Active && txt_subdirectory.Text.Trim ().Length == 0))
				btn_new.Sensitive = false;
			else
				btn_new.Sensitive = true;

			txt_subdirectory.Sensitive = CreateSolutionDirectory;
		}
		
		TreeViewColumn catColumn;
		CellRendererText cat_text_render;
		void InitializeComponents()
		{	
			catStore = new Gtk.TreeStore (typeof (string), typeof (Category));
			lst_template_types.Model = catStore;
			lst_template_types.WidthRequest = 160;
			
			lst_template_types.Selection.Changed += new EventHandler (CategoryChange);
			
			catColumn = new TreeViewColumn ();
			catColumn.Title = "categories";
			cat_text_render = new CellRendererText ();
			catColumn.PackStart (cat_text_render, true);
			catColumn.AddAttribute (cat_text_render, "text", 0);

			lst_template_types.AppendColumn (catColumn);

			templateView = new TemplateView ();
			
			boxTemplates.Add (templateView);

			if (basePath == null)
				basePath = IdeApp.ProjectOperations.ProjectsDefaultPath;
				
			entry_location.Path = FileService.ResolveFullPath (basePath);
			
			PathChanged (null, null);
			
			templateView.SelectionChanged += SelectedIndexChange;
			templateView.DoubleClicked += OpenEvent;
			entry_location.PathChanged += PathChanged;
			InitializeView ();
		}

		
		/// <summary>
		/// Holds a new file template
		/// </summary>
		internal class TemplateItem
		{
			ProjectTemplate template;
			string name;
			
			public TemplateItem (ProjectTemplate template)
			{
				name = StringParserService.Parse(template.Name);
				this.template = template;
			}
			
			public string Name {
				get { return name; }
			}
			
			public ProjectTemplate Template {
				get {
					return template;
				}
			}
			
			public bool DisplayCategory {
				get; set;
			}
		}

		private void InitializeTemplates ()
		{
			foreach (ProjectTemplate projectTemplate in ProjectTemplate.ProjectTemplates) {
				if (!newSolution && projectTemplate.SolutionDescriptor.EntryDescriptors.Length == 0)
					continue;
				TemplateItem templateItem = new TemplateItem (projectTemplate);
				
				Category category = GetCategory(templateItem.Template.Category);
				if (category != null )
					category.Templates.Add (templateItem);
				
				alltemplates.Add(templateItem);
			}
			
			recentCategory = new Category (GettextCatalog.GetString ("Recent"));
			string strRecent = PropertyService.Get<string> ("Dialogs.NewProjectDialog.RecentTemplates", "");
			recentIds = new List<string> (strRecent.Split (new char[] {','}, StringSplitOptions.RemoveEmptyEntries));

			foreach (string id in recentIds) {
				ProjectTemplate pt = GetTemplate (id);
				if (pt != null)
					recentCategory.Templates.Add (new TemplateItem (pt) { DisplayCategory = true });
			}
			
			InitializeComponents ();
		}
		
		private void InsertCategories (TreeIter node, List<Category> listCategories)
		{
			listCategories.Sort ();
			if (TreeIter.Zero.Equals (node))
				listCategories.Insert (0, recentCategory);
			foreach (Category category in listCategories) {
				if (TreeIter.Zero.Equals (node))
					InsertCategories (catStore.AppendValues (category.Name, category), category.Categories);
	            else {
					InsertCategories (catStore.AppendValues (node, category.Name, category), category.Categories);
				}
			}
		}
		
		private void CategoryChange (System.Object o, EventArgs e)
		{
			TreeModel treeModel;
			TreeIter treeIter;
			
			if (lst_template_types.Selection.GetSelected(out treeModel, out treeIter)) {
				templateView.Clear();
				
				foreach ( TemplateItem templateItem in  (catStore.GetValue(treeIter, 1) as Category).Templates) {
					templateView.Add (templateItem);
				}
				
				btn_new.Sensitive = false;
			}
			
		}
		
		protected virtual void OnBoxInfoSizeAllocated (object o, Gtk.SizeAllocatedArgs args)
		{
		}
		
		protected virtual void OnScrolledInfoSizeAllocated (object o, Gtk.SizeAllocatedArgs args)
		{
			if (labelTemplateTitle.WidthRequest != scrolledInfo.Allocation.Width) {
				labelTemplateTitle.WidthRequest = scrolledInfo.Allocation.Width;
				lbl_template_descr.WidthRequest = scrolledInfo.Allocation.Width;
			}
		}
		
		internal class Category: IComparable<Category>
		{
			private string name;
			public string Name
			{
				get { return name; }
			}
			
			public Category (string name)
			{
				this.name = name;
			}        
			
			private List<TemplateItem> templates = new List<TemplateItem>();
			public List<TemplateItem> Templates
			{
				get { return templates; }
			}
			
			private List<Category> categories = new List<Category>();
			public List<Category> Categories
			{
				get { return categories; }
			}

			public int CompareTo (Category other)
			{
				return name.CompareTo (other.name);
			}
		}
	
		class TemplateView: ScrolledWindow
		{
			TemplateTreeView tree;
			
			public TemplateView ()
			{
				tree = new TemplateTreeView ();
				tree.Selection.Changed += delegate {
					if (SelectionChanged != null)
						SelectionChanged (this, EventArgs.Empty);
				};
				tree.RowActivated += delegate {
					if (DoubleClicked != null)
						DoubleClicked (this, EventArgs.Empty);
				};
				Add (tree);
				HscrollbarPolicy = PolicyType.Automatic;
				VscrollbarPolicy = PolicyType.Automatic;
				ShadowType = ShadowType.In;
				ShowAll ();
			}
			
			public ProjectTemplate CurrentlySelected {
				get { return tree.CurrentlySelected; }
				set { tree.CurrentlySelected = value; }
			}
			
			public void Add (TemplateItem templateItem)
			{
				tree.Add (templateItem);
			}
			
			public void Clear ()
			{
				tree.Clear ();
			}
			
			public event EventHandler SelectionChanged;
			public event EventHandler DoubleClicked;
		}
			
		class TemplateTreeView: TreeView
		{
			Gtk.ListStore templateStore;
			
			public TemplateTreeView ()
			{
				HeadersVisible = false;
				templateStore = new ListStore (typeof(string), typeof(string), typeof(ProjectTemplate));
				Model = templateStore;
				
				TreeViewColumn col = new TreeViewColumn ();
				CellRendererIcon crp = new CellRendererIcon ();
				crp.StockSize = (uint) Gtk.IconSize.Dnd;
				crp.Ypad = 2;
				col.PackStart (crp, false);
				col.AddAttribute (crp, "stock-id", 0);
				
				CellRendererText crt = new CellRendererText ();
				col.PackStart (crt, false);
				col.AddAttribute (crt, "markup", 1);
				
				AppendColumn (col);
				ShowAll ();
			}
			
			public ProjectTemplate CurrentlySelected {
				get {
					Gtk.TreeIter iter;
					if (!Selection.GetSelected (out iter))
						return null;
					return (ProjectTemplate) templateStore.GetValue (iter, 2);
				}
				set {
					Gtk.TreeIter iter;
					if (templateStore.GetIterFirst (out iter)) {
						do {
							ProjectTemplate t = (ProjectTemplate) templateStore.GetValue (iter, 2);
							if (t == value) {
								Selection.SelectIter (iter);
								return;
							}
						} while (templateStore.IterNext (ref iter));
					}
				}
			}
			
			public void Add (TemplateItem templateItem)
			{
				string name = GLib.Markup.EscapeText (templateItem.Name);
				string desc = null;
				if (templateItem.DisplayCategory)
					desc = templateItem.Template.Category;
				else if (!string.IsNullOrEmpty (templateItem.Template.LanguageName))
					desc = templateItem.Template.LanguageName;
				
				if (desc != null)
					name += "\n<span foreground='darkgrey'><small>" + desc + "</small></span>";
				templateStore.AppendValues (templateItem.Template.Icon.IsNull ? "md-project" : templateItem.Template.Icon.ToString (), name, templateItem.Template);
			}
			
			public void Clear ()
			{
				templateStore.Clear ();
			}
		}
	}
}
