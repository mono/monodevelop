// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui.Components;
using MonoDevelop.Gui;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Internal.Templates;

using Gtk;
using MonoDevelop.Gui.Widgets;
using IconView = MonoDevelop.Gui.Widgets.IconView;

namespace MonoDevelop.Gui.Dialogs
{
	/// <summary>
	///  This class is for creating a new "empty" file
	/// </summary>
	internal class NewFileDialog : Dialog, INewFileCreator
	{
		ArrayList alltemplates = new ArrayList ();
		ArrayList categories   = new ArrayList ();
		Hashtable icons        = new Hashtable ();

		PixbufList cat_imglist;

		TreeStore catStore;
		TreeStore templateStore;
		Gtk.TreeView catView;
		Gtk.TreeView templateView;
		IconView TemplateView;
		Button okButton;
		Button cancelButton;
		Label infoLabel;

		public NewFileDialog () : base ()
		{
			this.TransientFor = (Window) WorkbenchSingleton.Workbench;
			this.BorderWidth = 6;
			this.HasSeparator = false;
			
			InitializeTemplates ();
		}
		
		void InitializeView()
		{
			PixbufList smalllist  = new PixbufList();
			PixbufList imglist    = new PixbufList();
			
			smalllist.Add(Runtime.Gui.Resources.GetBitmap("md-empty-file-icon"));
			imglist.Add(Runtime.Gui.Resources.GetBitmap("md-empty-file-icon"));
			
			int i = 0;
			Hashtable tmp = new Hashtable(icons);
			foreach (DictionaryEntry entry in icons) {
				Gdk.Pixbuf bitmap = Runtime.Gui.Resources.GetBitmap(entry.Key.ToString(), Gtk.IconSize.LargeToolbar);
				if (bitmap != null) {
					smalllist.Add(bitmap);
					imglist.Add(bitmap);
					tmp[entry.Key] = ++i;
				} else {
					Runtime.LoggingService.InfoFormat(GettextCatalog.GetString ("Can't load bitmap {0} using default"), entry.Key.ToString ());
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
				if (node.Equals(Gtk.TreeIter.Zero)) {
					node = catStore.AppendValues (cat.Name, cat.Categories, cat.Templates, cat_imglist[1]);
				} else {
					node = catStore.AppendValues (cat.Name, cat.Categories, cat.Templates, cat_imglist[1]);
				}
				InsertCategories(node, cat.Categories);
			}
		}
		
		// TODO : insert sub categories
		Category GetCategory(string categoryname)
		{
			foreach (Category category in categories) {
				if (category.Name == categoryname) {
					return category;
				}
			}
			Category newcategory = new Category(categoryname);
			categories.Add(newcategory);
			return newcategory;
		}
		
		void InitializeTemplates()
		{
			foreach (FileTemplate template in FileTemplate.FileTemplates) {
				TemplateItem titem = new TemplateItem(template);
				if (titem.Template.Icon != null) {
					icons[titem.Template.Icon] = 0; // "create template icon"
				}
				Category cat = GetCategory(titem.Template.Category);
				cat.Templates.Add(titem); 
				
				if (cat.Selected == false && template.WizardPath == null) {
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
			InitializeComponents ();
		}
		
		// tree view event handlers
		void CategoryChange(object sender, EventArgs e)
		{
			TreeModel mdl;
			TreeIter  iter;
			if (catView.Selection.GetSelected (out mdl, out iter)) {
				//templateStore.Clear ();
                                TemplateView.Clear ();
				foreach (TemplateItem item in (ArrayList)((Gtk.TreeStore)mdl).GetValue (iter, 2)) {
					//templateStore.AppendValues (item.Name, item.Template);
					
					TemplateView.AddIcon(new Gtk.Image(Runtime.Gui.Resources.GetBitmap (item.Template.Icon, Gtk.IconSize.Dnd)), item.Name, item.Template);
				}
				okButton.Sensitive = false;
			}
		}
		
		// list view event handlers
		void SelectedIndexChange(object sender, EventArgs e)
		{
			FileTemplate item = (FileTemplate)TemplateView.CurrentlySelected;

			if (item != null)
			{
				infoLabel.Text = item.Description;
				okButton.Sensitive = true;
			}
		}
		
		// button events
		
		protected void CheckedChange(object sender, EventArgs e)
		{
			//((ListView)ControlDictionary["templateListView"]).View = ((RadioButton)ControlDictionary["smallIconsRadioButton"]).Checked ? View.List : View.LargeIcon;
		}
		
		public bool IsFilenameAvailable(string fileName)
		{
			return true;
		}
		
		public void SaveFile(string filename, string content, string languageName, bool showFile)
		{
			Runtime.FileService.NewFile(filename, languageName, content);
		}

		public event EventHandler OnOked;	
	
		void OpenEvent(object sender, EventArgs e)
		{
			//FIXME: we need to set this up
			//PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
			//propertyService.SetProperty("Dialogs.NewProjectDialog.LargeImages", ((RadioButton)ControlDictionary["largeIconsRadioButton"]).Checked);
			//propertyService.SetProperty("Dialogs.NewFileDialog.LastSelectedCategory", ((TreeView)ControlDictionary["categoryTreeView"]).SelectedNode.Text);
			
			//if (templateView.Selection.GetSelected (out mdl, out iter)) {
			if (TemplateView.CurrentlySelected != null) {
				FileTemplate item = (FileTemplate)TemplateView.CurrentlySelected;
				
				if (item.WizardPath != null) {
					//IProperties customizer = new DefaultProperties();
					//customizer.SetProperty("Template", item);
					//customizer.SetProperty("Creator",  this);
					//WizardDialog wizard = new WizardDialog("File Wizard", customizer, item.WizardPath);
					//if (wizard.ShowDialog() == DialogResult.OK) {
						//DialogResult = DialogResult.OK;
					//}
				} else {
					foreach (FileDescriptionTemplate newfile in item.Files) {
						SaveFile(newfile.Name, newfile.Content, item.LanguageName, true);
					}
				}
				
				if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null) {
					WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.SelectWindow();
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
			
			public TemplateItem(FileTemplate template)
			{
				this.template = template;
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

			cat_imglist = new PixbufList();
			cat_imglist.Add(Runtime.Gui.Resources.GetBitmap("md-open-folder"));
			cat_imglist.Add(Runtime.Gui.Resources.GetBitmap("md-closed-folder"));
			catView.Selection.Changed += new EventHandler (CategoryChange);
			TemplateView.IconSelected += new EventHandler(SelectedIndexChange);
			TemplateView.IconDoubleClicked += new EventHandler(OpenEvent);
			InitializeView ();
		}
	}
}
