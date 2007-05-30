//
// AddNewFilesToProjectDialog.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Gtk;

using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Ide.Gui.Pads.SolutionViewPad
{
	public partial class AddNewFilesToProjectDialog : Gtk.Dialog
	{
		TreeStore categoryTreeStore = new Gtk.TreeStore (typeof(string), typeof(Category));
		List<Category> categories   = new List<Category> ();
		string         language;
		string         path;
		string[]       createdFiles;
		
		public string[] CreatedFiles {
			get {
				return this.createdFiles;
			}
		}
		
		public AddNewFilesToProjectDialog (string language, string path)
		{
			this.language = language;
			this.path     = path;
			
			this.Build();

			LoadTemplates ();
			CreateCategoryItems ();
			
			this.labelName.Text = GettextCatalog.GetString ("Name:");
			this.Title          = GettextCatalog.GetString ("Add new File");
			
			this.buttonNew.Sensitive = false;
			this.buttonNew.Clicked += new EventHandler (FileCreationRequested);
			
			this.buttonCancel.Clicked += delegate {
				this.Respond (Gtk.ResponseType.Cancel);
				this.Destroy ();
			};
			
			this.treeviewCatalog.HeadersVisible = false;
			this.treeviewCatalog.Selection.Changed += delegate {
				TreeModel model;
				TreeIter iter;
				if (this.treeviewCatalog.Selection.GetSelected (out model, out iter)) {
					ShowCategory (this.categoryTreeStore.GetValue (iter, 1) as Category);
				}
			};
			this.entryName.Changed     += new EventHandler (DialogStateChanged);
			this.iconView.IconSelected += new EventHandler (DialogStateChanged);
			iconView.IconDoubleClicked += new EventHandler (FileCreationRequested);
		}
		
		void DialogStateChanged (object sender, EventArgs e)
		{
			FileTemplate currentTemplate = (FileTemplate) iconView.CurrentlySelected;
			if (currentTemplate == null) {
				this.buttonNew.Sensitive = false;
				return;
			}
			this.labelInfo.Text      = GettextCatalog.GetString (currentTemplate.Description);
			this.buttonNew.Sensitive = currentTemplate.IsValidName (this.entryName.Text, language);
		}
		
		void FileCreationRequested (object sender, EventArgs e)
		{
			FileTemplate currentTemplate = (FileTemplate) iconView.CurrentlySelected;
			if (currentTemplate == null)
				return;
			
			this.createdFiles = currentTemplate.Create (this.path, this.language, this.entryName.Text);
			
			this.Respond (Gtk.ResponseType.Ok);
			this.Destroy ();
		}
		
		void ShowCategory (Category category)
		{
			iconView.Clear ();
			foreach (FileTemplate template in category.Templates) {
				iconView.AddIcon (new Gtk.Image (Services.Resources.GetBitmap (template.Icon, Gtk.IconSize.Dnd)), template.Name, template);
			}
		}
		
		void CreateCategoryItems ()
		{
			categoryTreeStore.SetSortColumnId (0, SortType.Ascending);
			this.treeviewCatalog.Model = categoryTreeStore;
			
			if (categories.Count == 0)
				return;
			
			foreach (Category category in this.categories)
				categoryTreeStore.AppendValues (GettextCatalog.GetString (category.Name), category);
			
			this.treeviewCatalog.AppendColumn (null, new CellRendererText (), "text", 0);			
		}
		
		void LoadTemplates ()
		{
			foreach (FileTemplate fileTemplate in FileTemplate.FileTemplates) {
				if (fileTemplate.LanguageName != "*" && fileTemplate.LanguageName != this.language)
					continue;
				Category category = GetCategory (fileTemplate.Category);
				category.Templates.Add (fileTemplate);
			}
		}
		
		Category GetCategory (string name)
		{
			foreach (Category category in this.categories)
				if (category.Name == name)
					return category;
			Category result = new Category (name);
			this.categories.Add (result);
			return result;
		}
		
		class Category
		{
			string name;
			List<FileTemplate> templates = new List<FileTemplate> ();
			
			public string Name {
				get {
					return this.name;
				}
			}
			
			public List<FileTemplate> Templates {
				get {
					return templates;
				}
			}
			
			public Category (string name)
			{
				this.name = name;
			}
		}
	}
}
