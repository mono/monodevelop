// 
// TemplatePickerWidget.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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

using System;
using System.Collections.Generic;

using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.Templates;
using System.Linq;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Projects
{
	class TemplatePickerWidget : Bin
	{
		SectionList sectionList = new SectionList ();
		SectionList.Section recentSection, installedSection, onlineSection;
		
		CategoryTreeView recentTemplateCatView = new CategoryTreeView ();
		CategoryTreeView installedTemplateCatView = new CategoryTreeView ();
		CategoryTreeView onlineTemplateCatView = new CategoryTreeView ();
		
		List<TemplateItem> recentTemplates = new List<TemplateItem> ();
		List<TemplateItem> installedTemplates = new List<TemplateItem> ();
		List<TemplateItem> onlineTemplates = new List<TemplateItem> ();
		
		HPaned hsplit = new HPaned ();
		VPaned vsplit = new VPaned ();
		VBox rightVbox = new VBox ();
		HBox searchHbox = new HBox ();
		
		CompactScrolledWindow infoScrolledWindow = new CompactScrolledWindow ();
		VBox infoBox = new VBox ();
		Label infoHeaderLabel = new Label ();
		Label infoDecriptionLabel = new Label ();
		
		SearchEntry searchEntry = new SearchEntry ();
		
		TemplateView templateView = new TemplateView ();
		
		public TemplatePickerWidget ()
		{
			Stetic.BinContainer.Attach (this);
			
			infoBox.BorderWidth = 4;
			infoBox.Spacing = 4;
			infoHeaderLabel.Wrap = true;
			infoHeaderLabel.Xalign = 0;
			infoDecriptionLabel.Wrap = true;
			infoDecriptionLabel.Xalign = 0;
			infoDecriptionLabel.Yalign = 0;
			
			infoBox.SizeAllocated += delegate {
				var w = infoBox.Allocation.Width - 10;
				if (infoHeaderLabel.WidthRequest != w) {
					infoHeaderLabel.WidthRequest = w;
					infoDecriptionLabel.WidthRequest = w;
				}
			};
			
			recentSection = sectionList.AddSection (GettextCatalog.GetString ("Recent Templates"), recentTemplateCatView);
			installedSection = sectionList.AddSection (GettextCatalog.GetString ("Installed Templates"), installedTemplateCatView);
			onlineSection = sectionList.AddSection (GettextCatalog.GetString ("Online Templates"), onlineTemplateCatView);
			
			recentSection.Activated += delegate {
				LoadTemplatesIntoView (recentTemplates);
				templateView.SetCategoryFilter (recentTemplateCatView.GetSelection ());
			};
			
			installedSection.Activated += delegate {
				LoadTemplatesIntoView (installedTemplates);
				templateView.SetCategoryFilter (installedTemplateCatView.GetSelection ());
			};
			
			onlineSection.Activated += delegate {
				LoadTemplatesIntoView (onlineTemplates);
				templateView.SetCategoryFilter (onlineTemplateCatView.GetSelection ());
			};
			
			recentTemplateCatView.SelectionChanged += delegate {
				if (recentSection.IsActive)
					templateView.SetCategoryFilter (recentTemplateCatView.GetSelection ());
			};
			
			installedTemplateCatView.SelectionChanged += delegate {
				if (installedSection.IsActive)
					templateView.SetCategoryFilter (installedTemplateCatView.GetSelection ());
			};
			
			onlineTemplateCatView.SelectionChanged += delegate {
				if (onlineSection.IsActive)
					templateView.SetCategoryFilter (onlineTemplateCatView.GetSelection ());
			};
			
			searchEntry.WidthRequest = 150;
			searchEntry.EmptyMessage = GettextCatalog.GetString ("Search…");
			searchEntry.Changed += delegate {
				templateView.SetSearchFilter (searchEntry.Entry.Text);
			};
			searchEntry.Activated += delegate {
				templateView.Child.GrabFocus ();
			};
			searchEntry.Ready = true;
			searchEntry.Show ();
			
			installedTemplateCatView.SelectionChanged += delegate (object sender, EventArgs e) {
				var selection = installedTemplateCatView.GetSelection ();
				templateView.SetCategoryFilter (selection);
			};
			
			templateView.SelectionChanged += TemplateSelectionChanged;
			templateView.DoubleClicked += delegate {
				OnActivated ();
			};
			
			Add (hsplit);
			hsplit.Pack1 (sectionList, true, false);
			hsplit.Pack2 (rightVbox, true, false);
			rightVbox.PackStart (searchHbox, false, false, 0);
			rightVbox.Spacing = 6;
			searchHbox.PackStart (new Label (), true, true, 0);
			searchHbox.PackStart (searchEntry, false, false, 0);
			rightVbox.PackStart (vsplit, true, true, 0);
			vsplit.Pack1 (templateView, true, false);
			vsplit.Pack2 (infoScrolledWindow, true, false);
			infoScrolledWindow.ShowBorderLine = true;
			var vp = new Viewport ();
			vp.ShadowType = ShadowType.None;
			vp.Add (infoBox);
			infoScrolledWindow.Add (vp);
			infoBox.PackStart (infoHeaderLabel, false, false, 0);
			infoBox.PackStart (infoDecriptionLabel, true, true, 0);
			hsplit.ShowAll ();
			
			//sane proportions for the splitter children
			templateView.HeightRequest = 200;
			infoScrolledWindow.HeightRequest = 75;
			sectionList.WidthRequest = 150;
			rightVbox.WidthRequest = 300;
			
			sectionList.ActiveIndex = 1;
		}

		void TemplateSelectionChanged (object sender, EventArgs e)
		{
			var ti = templateView.CurrentlySelected;
			if (ti == null) {
				infoHeaderLabel.Markup = "";
				infoDecriptionLabel.Text = "";
			} else {
				infoHeaderLabel.Markup = "<b>" + GLib.Markup.EscapeText (ti.Name) + "</b>";
				infoDecriptionLabel.Text = StringParserService.Parse (ti.Template.Description);
			}
			OnSelectionChanged ();
		}
		
		void OnSelectionChanged ()
		{
			var evt = SelectionChanged;
			if (evt != null)
				evt (this, null);
		}
		
		void OnActivated ()
		{
			var evt = Activated;
			if (evt != null)
				evt (this, null);
		}
		
		public ProjectTemplate Selection {
			get {
				var sel = templateView.CurrentlySelected;
				if (sel == null)
					return null;
				return sel.Template;
			}
		}
		
		public void SelectTemplate (string id)
		{
			templateView.SelectTemplate (id);
		}
		
		public event EventHandler SelectionChanged;
		public event EventHandler Activated;
		
		void LoadTemplatesIntoView (List<TemplateItem> templates)
		{
			templateView.Clear ();
			foreach (var t in templates) {
				templateView.AddItem (t);
			}
		}
		
		public void LoadInstalledTemplates (IEnumerable<ProjectTemplate> templates)
		{
			foreach (var template in templates) {
				if (template == null)
					throw new ArgumentException ("Null template");
				installedTemplates.Add (new TemplateItem (template));
			}
			installedTemplates.Sort ((a,b) => String.Compare (a.Name, b.Name));
			installedTemplateCatView.Load (installedTemplates);
			
			
			if (installedTemplateCatView.GetSelection () == null)
				installedTemplateCatView.SetSelection (new string[0]);
			
			if (installedSection.IsActive)
				LoadTemplatesIntoView (installedTemplates);
		}
		
		public void LoadRecentTemplates (IEnumerable<ProjectTemplate> templates)
		{
			foreach (var template in templates) {
				if (template == null)
					throw new ArgumentException ("Null template");
				recentTemplates.Add (new TemplateItem (template));
			}
			//don't sort recent templates, they should already be in most->least recent
			recentTemplateCatView.Load (recentTemplates);
			
			if (recentTemplateCatView.GetSelection () == null)
				recentTemplateCatView.SetSelection (new string[0]);
			
			if (recentSection.IsActive)
				LoadTemplatesIntoView (installedTemplates);
		}
		
		public string InstalledTemplateSelectedCategory {
			get {
				return string.Join ("/", installedTemplateCatView.GetSelection ());
			}
			set {
				var cat = value.Split (new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
				installedTemplateCatView.SetSelection (cat);
			}
		}
		
		public string RecentTemplateSelectedCategory {
			get {
				return string.Join ("/", recentTemplateCatView.GetSelection ());
			}
			set {
				var cat = value.Split (new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
				recentTemplateCatView.SetSelection (cat);
			}
		}
		
		class CategoryTreeView : CompactScrolledWindow
		{
			TreeView view = new TreeView ();
			TreeStore store = new TreeStore (typeof (string));
			
			public CategoryTreeView ()
			{
				view.Model = store;
				view.HeadersVisible = false;
				var col = new TreeViewColumn ();
				var crt = new CellRendererText ();
				col.PackStart (crt, false);
				col.AddAttribute (crt, "markup", 0);
				view.AppendColumn (col);
				view.Selection.Changed += delegate {
					if (SelectionChanged != null)
						SelectionChanged (this, EventArgs.Empty);
				};
				this.Add (view);
			}
			
			public void Load (List<TemplateItem> templates)
			{
				store.Clear ();
				
				string general = GettextCatalog.GetString ("General");
				string all = GettextCatalog.GetString ("All");
				var categories = new Dictionary<string, object> ();
				
				foreach (var tp in templates) {
					string[] catPath;
					if (tp.Category == null || tp.Category.Length == 0)
						catPath = new string[] { general };
					else
						catPath = tp.Category;
					
					Dictionary<string, object> searchCats = categories;
					for (int i = 0; i < catPath.Length; i++) {
						string s = catPath[i];
						KeyValuePair<string,object>? found = null; 
						foreach (var searchCat in searchCats)
							if (searchCat.Key == s)
								found = searchCat;
						if (i < catPath.Length - 1) {
							if (found.HasValue && found.Value.Value != null) {
								searchCats = (Dictionary<string,object>) found.Value.Value;
							} else {
								var d = new Dictionary<string, object> ();
								searchCats [s] = d;
								searchCats = d;
							}
						} else if (!found.HasValue) {
							searchCats.Add (s, null);
						}	
					}
				}
				
				var iter = store.AppendValues (all);
				BuildTree (iter, categories);
				view.ExpandAll ();
			}
			
			void BuildTree (TreeIter parent, Dictionary<string, object> cats)
			{
				foreach (var cat in cats.OrderBy (kv => kv.Key)) {
					var iter = store.AppendValues (parent, cat.Key);
					if (cat.Value != null)
						BuildTree (iter, (Dictionary<string, object>) cat.Value);
				}
			}
			
			public IList<string> GetSelection ()
			{
				TreeIter selectedIter;
				if (!view.Selection.GetSelected (out selectedIter))
					return null;
				
				string[] selection = new string[store.IterDepth (selectedIter)];
				TreeIter iter;
				
				//root is the "all" category
				store.GetIterFirst (out iter);
				if (iter.Equals (selectedIter))
					return selection;
				store.IterChildren (out iter, iter);
				
				for (int i = 0; i < selection.Length; i++) {
					do {
						if (iter.Equals (selectedIter)) {
							selection[i] = (string) store.GetValue (iter, 0);
							return selection;
						} else if (store.IsAncestor (iter, selectedIter)) {
							selection[i] = (string) store.GetValue (iter, 0);
							store.IterChildren (out iter, iter);
							break;
						}
					} while (store.IterNext (ref iter));
				}
				return selection;
			}
			
			public void SetSelection (IList<string> value)
			{
				TreeIter parent;
				if (!store.GetIterFirst (out parent))
					return;
				
				//root is the "all" node, matched by zero-length selection array
				if (value.Count == 0) {
					view.Selection.SelectIter (parent);
					return;
				}
				
				TreeIter iter;
				store.IterChildren (out iter, parent);
				
				int i = 0;
				while (store.IterNext (ref iter)) {
					var s = (string) store.GetValue (iter, 0);
					if (s == value[i]) {
						i++;
						TreeIter child;
						if (i == value.Count || !store.IterChildren (out child, iter)) {
							view.Selection.SelectIter (iter);
							return;
						}
						parent = iter;
						iter = child;
					}
				}
				view.Selection.SelectIter (parent);
			}
			
			public event EventHandler SelectionChanged;
		}
		
		class TemplateView: CompactScrolledWindow
		{
			TemplateTreeView tree;
			IList<string> categoryFilter;
			string searchFilter;
			
			public TemplateView ()
			{
				ShowBorderLine = true;
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
				ShadowType = ShadowType.None;
				ShowAll ();
			}
			
			public TemplateItem CurrentlySelected {
				get { return tree.CurrentlySelected; }
			}
			
			public void SelectTemplate (string id)
			{
				tree.SelectItem (id);
			}
			
			public void AddItem (TemplateItem templateItem)
			{
				tree.AddItem (templateItem);
			}
			
			public void Clear ()
			{
				tree.Clear ();
			}
			
			public void SetCategoryFilter (IList<string> categoryFilter)
			{
				this.categoryFilter = categoryFilter;
				Refilter ();
			}
			
			public void SetSearchFilter (string search)
			{
				this.searchFilter = search;
				Refilter ();
			}
			
			bool MatchesCategory (TemplateItem item)
			{
				if (categoryFilter == null)
					return true;
				if (item.Category == null || item.Category.Length < categoryFilter.Count)
					return false;
				for (int i = 0; i < categoryFilter.Count; i++)
					if (item.Category[i] != categoryFilter[i])
						return false;
				return true;
			}
			
			bool MatchesSearch (TemplateItem item)
			{
				return string.IsNullOrWhiteSpace (searchFilter)
					|| item.Name.IndexOf (searchFilter, StringComparison.CurrentCultureIgnoreCase) >= 0
					|| item.Template.Description.IndexOf (searchFilter, StringComparison.CurrentCultureIgnoreCase) >= 0;
			}
			
			void Refilter ()
			{
				tree.Filter (item => MatchesSearch (item) && MatchesCategory (item));
			}
			
			public event EventHandler SelectionChanged;
			public event EventHandler DoubleClicked;
		}
		
		class TemplateTreeView: TreeView
		{
			Gtk.ListStore templateStore;
			TreeModelFilter filterModel;
			Func<TemplateItem,bool> filterFunc;
			
			public TemplateTreeView ()
			{
				HeadersVisible = false;
				templateStore = new ListStore (typeof(TemplateItem));
				Model = filterModel = new TreeModelFilter (templateStore, null);
				filterModel.VisibleFunc = FilterFuncWrapper;
				
				var col = new TreeViewColumn ();
				var crp = new CellRendererImage () {
					StockSize = Gtk.IconSize.Dnd,
					Ypad = 2,
				};
				col.PackStart (crp, false);
				col.SetCellDataFunc (crp, CellDataFuncIcon);
				
				var crt = new CellRendererText ();
				col.PackStart (crt, false);
				col.SetCellDataFunc (crt, CellDataFuncText);
				
				AppendColumn (col);
				ShowAll ();
			}

			bool FilterFuncWrapper (TreeModel model, TreeIter iter)
			{
				if (filterFunc == null)
					return true;
				var item = (TemplateItem) model.GetValue (iter, 0);
				
				//gets called while the rows are being inserted and don't yet have data
				if (item == null)
					return false;
				
				return filterFunc (item);
			}

			static void CellDataFuncText (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
			{
				var item = (TemplateItem) model.GetValue (iter, 0);
				string name = GLib.Markup.EscapeText (item.Name);
				if (!string.IsNullOrEmpty (item.Template.LanguageName))
					name += "\n<span foreground='darkgrey'><span font='11'>" + item.Template.LanguageName + "<span></span>";
				
				((CellRendererText)cell).Markup = name;
			}
			
			static void CellDataFuncIcon (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
			{
				var item = (TemplateItem) model.GetValue (iter, 0);
				var id = item.Template.Icon.IsNull ? "md-project" : item.Template.Icon.ToString ();
				((CellRendererImage)cell).StockId = id;
			}
			
			public void Filter (Func<TemplateItem,bool> filter)
			{
				this.filterFunc = filter;
				this.filterModel.Refilter ();
			}
			
			public TemplateItem CurrentlySelected {
				get {
					Gtk.TreeIter iter;
					if (!Selection.GetSelected (out iter))
						return null;
					return (TemplateItem) filterModel.GetValue (iter, 0);
				}
			}
			
			public void SelectItem (string id)
			{
				Gtk.TreeIter iter;
				if (filterModel.GetIterFirst (out iter)) {
					do {
						var t = (TemplateItem) filterModel.GetValue (iter, 0);
						if (t.Template.Id == id) {
							Selection.SelectIter (iter);
							return;
						}
					} while (filterModel.IterNext (ref iter));
				}
			}
			
			public void AddItem (TemplateItem templateItem)
			{
				templateStore.AppendValues (templateItem);
			}
			
			public void Clear ()
			{
				templateStore.Clear ();
			}
		}
		
		class TemplateItem
		{
			public TemplateItem (ProjectTemplate template)
			{
				Name = StringParserService.Parse (template.Name);
				this.Template = template;
				if (!string.IsNullOrEmpty (template.Category))
					this.Category = template.Category.Split (new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
				else
					this.Category = new string[0];
			}
			
			public string Name { get; private set; }
			public string[] Category { get; private set; }
			public ProjectTemplate Template { get; private set; }
		}
	}
}

