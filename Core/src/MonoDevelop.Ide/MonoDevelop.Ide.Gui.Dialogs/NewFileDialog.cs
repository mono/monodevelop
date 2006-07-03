// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
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
	internal class NewFileDialog : Dialog
	{
		ArrayList alltemplates = new ArrayList ();
		ArrayList categories   = new ArrayList ();
		Hashtable icons        = new Hashtable ();
		ArrayList projectLangs = new ArrayList ();

		PixbufList cat_imglist;

		TreeStore catStore;
		TreeStore templateStore;
		Gtk.TreeView catView;
		Gtk.TreeView templateView;
		IconView TemplateView;
		Button okButton;
		Button cancelButton;
		Label infoLabel;
		Entry nameEntry;
		
		Project parentProject;
		string basePath;
		
		string currentProjectType = string.Empty;

		public NewFileDialog (Project parentProject, string basePath) : base ()
		{
			this.parentProject = parentProject;
			this.basePath = basePath;
			
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.BorderWidth = 6;
			this.HasSeparator = false;
			
			//if there's a parent project, check whether it wants to filter languages, else use defaults
			if ((parentProject != null) && (parentProject.SupportedLanguages != null) && (parentProject.SupportedLanguages.Length > 0)) {
				projectLangs.AddRange (parentProject.SupportedLanguages);
			} else {
				projectLangs.Add ("");	//match all non-filtered templates
				projectLangs.Add ("*");	//match all .NET langs with CodeDom
			}
			ExpandLanguageWildcards (projectLangs);
			
			InitializeTemplates ();
			InitializeComponents ();
			nameEntry.GrabFocus ();
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
					Runtime.LoggingService.ErrorFormat(GettextCatalog.GetString ("Can't load bitmap {0} using default"), entry.Key.ToString ());
				}
			}
			
			icons = tmp;
			
			InsertCategories(TreeIter.Zero, categories);
			//PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
			/*for (int j = 0; j < categories.Count; ++j) {
				if (((Category)categories[j]).Name == propertyService.GetProperty("Dialogs.NewFileDialog.LastSelectedCategory", "C#")) {
					((TreeView)ControlDictionary["categoryTreeView"]).SelectedNode = (TreeNode)((TreeView)ControlDictionary["categoryTreeView"]).Nodes[j];
					break;
				}
			}*/
			ShowAll ();
		}
		
		void InsertCategories(TreeIter node, ArrayList catarray)
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
				foreach (TemplateItem item in (ArrayList)(catStore.GetValue (iter, 2))) {
					if (item.Template.Id == id) {
						catView.Selection.SelectIter (iter);
						TemplateView.CurrentlySelected = item;
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
		
		Category GetCategory (ArrayList catList, string categoryname)
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
			foreach (FileTemplate template in FileTemplate.GetFileTemplates (parentProject)) {
			
				if (template.Icon != null) {
					icons[template.Icon] = 0; // "create template icon"
				}
				
				// Ignore templates not supported by this project
				if (template.ProjectType != "" && template.ProjectType != currentProjectType)
					continue;
				
				//Find the languages that the template supports
				ArrayList templateLangs = new ArrayList ();
				foreach (string s in template.LanguageName.Split (','))
					templateLangs.Add (s.Trim ());
				ExpandLanguageWildcards (templateLangs);
				
				//Find all matches between the language strings of project and template
				ArrayList langMatches = new ArrayList ();
				
				foreach (string templLang in templateLangs)
					foreach (string projLang in projectLangs)
						if (templLang == projLang)
							langMatches.Add (projLang);
				
				//Eliminate duplicates
				int pos = 0;
				while (pos < langMatches.Count) {
					int next = langMatches.IndexOf (langMatches [pos], pos +1);
					if (next != -1)
						langMatches.RemoveAt (next);
					else
						pos++;
				}
				
				//Add all the possible templates
				foreach (string match in langMatches) {
					AddTemplate (new TemplateItem (template, match), match);	
				}
			}
		}
		
		void ExpandLanguageWildcards (ArrayList list)
		{
			//Template can match all CodeDom .NET languages with a "*"
			if (list.Contains ("*")) {
				ILanguageBinding [] bindings = MonoDevelop.Projects.Services.Languages.GetLanguageBindings ();
				foreach (ILanguageBinding lb in bindings) {
					IDotNetLanguageBinding dnlang = lb as IDotNetLanguageBinding;
					if (dnlang != null && dnlang.GetCodeDomProvider () != null)
						list.Add (dnlang.Language);
				list.Remove ("*");
				}
			}
		}
		
		void AddTemplate (TemplateItem titem, string templateLanguage)
		{
			Category cat = null;
			
			if (parentProject != null) {
				if ((templateLanguage != "") && (projectLangs.Count != 2) ) {
					// The template requires a language, but the project does not have a single fixed
					// language type (plus empty match), so create a language category
					cat = GetCategory (templateLanguage);
					cat = GetCategory (cat.Categories, titem.Template.Category);
				} else {
					cat = GetCategory (titem.Template.Category);
				}
			} else {
				if (templateLanguage != "") {
					// The template requires a language, but there is no current language set, so create a category for it
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
			TreeIter  iter;
			if (catView.Selection.GetSelected (out mdl, out iter)) {
				FillCategoryTemplates (iter);
				okButton.Sensitive = false;
			}
		}
		
		void FillCategoryTemplates (TreeIter iter)
		{
			TemplateView.Clear ();
			foreach (TemplateItem item in (ArrayList)(catStore.GetValue (iter, 2))) {
				TemplateView.AddIcon (new Gtk.Image (Services.Resources.GetBitmap (item.Template.Icon, Gtk.IconSize.Dnd)), item.Name, item);
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
			TemplateItem sel = (TemplateItem) TemplateView.CurrentlySelected;
			if (sel == null)
				return;
			
			FileTemplate item = sel.Template;
			

			if (item != null) {
				infoLabel.Text = item.Description;
				okButton.Sensitive = item.IsValidName (nameEntry.Text, sel.Language);
			} else {
				okButton.Sensitive = false;
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
			//FIXME: we need to set this up
			//PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
			//propertyService.SetProperty("Dialogs.NewProjectDialog.LargeImages", ((RadioButton)ControlDictionary["largeIconsRadioButton"]).Checked);
			//propertyService.SetProperty("Dialogs.NewFileDialog.LastSelectedCategory", ((TreeView)ControlDictionary["categoryTreeView"]).SelectedNode.Text);
			
			//if (templateView.Selection.GetSelected (out mdl, out iter)) {
			if (TemplateView.CurrentlySelected != null && nameEntry.Text.Length > 0) {
				TemplateItem titem = (TemplateItem) TemplateView.CurrentlySelected;
				FileTemplate item = titem.Template;
				
				try {
					if (!item.Create (parentProject, basePath, titem.Language, nameEntry.Text))
						return;
				} catch (Exception ex) {
					Services.MessageService.ShowError (ex);
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
		internal class Category
		{
			ArrayList categories = new ArrayList();
			ArrayList templates  = new ArrayList();
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
			public ArrayList Categories {
				get {
					return categories;
				}
			}
			public ArrayList Templates {
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

		void InitializeComponents()
		{
			
			catStore = new Gtk.TreeStore (typeof(string), typeof(ArrayList), typeof(ArrayList), typeof(Gdk.Pixbuf));
			catStore.SetSortColumnId (0, SortType.Ascending);
			
			templateStore = new Gtk.TreeStore (typeof(string), typeof(FileTemplate));

			ScrolledWindow swindow1 = new ScrolledWindow();
			swindow1.VscrollbarPolicy = PolicyType.Automatic;
			swindow1.HscrollbarPolicy = PolicyType.Automatic;
			swindow1.ShadowType = ShadowType.In;
			catView = new Gtk.TreeView (catStore);
			catView.WidthRequest = 160;
			catView.HeadersVisible = false;
			templateView = new Gtk.TreeView (templateStore);
			TemplateView = new IconView();

			TreeViewColumn catColumn = new TreeViewColumn ();
			catColumn.Title = "categories";
			
			CellRendererText cat_text_render = new CellRendererText ();
			catColumn.PackStart (cat_text_render, true);
			catColumn.AddAttribute (cat_text_render, "text", 0);

			catView.AppendColumn (catColumn);

			TreeViewColumn templateColumn = new TreeViewColumn ();
			templateColumn.Title = "template";
			CellRendererText tmpl_text_render = new CellRendererText ();
			templateColumn.PackStart (tmpl_text_render, true);
			templateColumn.AddAttribute (tmpl_text_render, "text", 0);
			templateView.AppendColumn (templateColumn);

			okButton = new Button (Gtk.Stock.New);
			okButton.Clicked += new EventHandler (OpenEvent);

			cancelButton = new Button (Gtk.Stock.Close);
			cancelButton.Clicked += new EventHandler (cancelClicked);

			infoLabel = new Label ("");
			Frame infoLabelFrame = new Frame();
			infoLabelFrame.Add(infoLabel);

			HBox viewbox = new HBox (false, 6);
			swindow1.Add(catView);
			viewbox.PackStart (swindow1,false,true,0);
			viewbox.PackStart(TemplateView, true, true,0);

			this.AddActionWidget (cancelButton, (int)Gtk.ResponseType.Cancel);
			this.AddActionWidget (okButton, (int)Gtk.ResponseType.Ok);

			this.VBox.PackStart (viewbox);
			this.VBox.PackStart (infoLabelFrame, false, false, 6);
			
			HBox nameBox = new HBox ();
			nameBox.PackStart (new Label (GettextCatalog.GetString ("Name:")), false, false, 0);
			nameEntry = new Entry ();
			nameBox.PackStart (nameEntry, true, true, 6);
			nameEntry.Changed += new EventHandler (NameChanged);
			this.VBox.PackStart (nameBox, false, false, 6);

			cat_imglist = new PixbufList();
			cat_imglist.Add(Services.Resources.GetBitmap("md-open-folder"));
			cat_imglist.Add(Services.Resources.GetBitmap("md-closed-folder"));
			catView.Selection.Changed += new EventHandler (CategoryChange);
			catView.RowActivated += new RowActivatedHandler (CategoryActivated);
			TemplateView.IconSelected += new EventHandler(SelectedIndexChange);
			TemplateView.IconDoubleClicked += new EventHandler(OpenEvent);
			InitializeView ();
		}
	}
}
