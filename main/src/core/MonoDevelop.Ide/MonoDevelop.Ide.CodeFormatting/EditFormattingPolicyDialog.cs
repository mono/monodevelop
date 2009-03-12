// 
// EditFormattingPolicyDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Mike Krüger
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.CodeFormatting
{
	public partial class EditFormattingPolicyDialog : Gtk.Dialog
	{
		CodeFormatSettings    settings;
		CodeFormatDescription description;
		ListStore comboBoxStore;
		
		Gtk.TreeStore store;
		TreeModel model;
		TreeIter iter;
		CodeFormatOption option;
		
		public EditFormattingPolicyDialog()
		{
			this.Build();
			buttonOk.Clicked += delegate {
				if (description == null)
					return;
				settings.Name = this.entryName.Text;
			};
			
/*			Gtk.CellRendererText ctx = new Gtk.CellRendererText ();
			comboboxValue.PackStart (ctx, true);
			comboboxValue.AddAttribute (ctx, "text", 0);*/
			
			comboBoxStore = new ListStore (typeof (string));
			comboboxValue.Model = comboBoxStore;
			
			comboboxValue.Changed += HandleChanged;
		}
		
		class CustomFormattingPolicy : CodeFormattingPolicy
		{
			CodeFormatSettings settings;
			
			public CustomFormattingPolicy (CodeFormatSettings settings)
			{
				this.settings = settings;
			}
			
			public override CodeFormatSettings GetSettings ()
			{
				return settings;
			}
		}
		
		void UpdateExample ()
		{
			IFormatter formatter = TextFileService.GetFormatter (description.MimeType);
			DotNetProject parent = new DotNetProject ();
			parent.Policies.Set <CodeFormattingPolicy> (new CustomFormattingPolicy (settings));
			textviewPreview.Buffer.Text = formatter.FormatText (parent, textviewPreview.Buffer.Text);
		}
		
		void HandleChanged(object sender, EventArgs e)
		{
			settings.SetValue (option, comboboxValue.ActiveText);
			store.SetValue (iter, 1, comboboxValue.ActiveText);
			UpdateExample ();
		}
		
		public void SetFormat (CodeFormatDescription description, CodeFormatSettings settings)
		{
			this.description = description;
			this.settings    = settings;
			this.entryName.Text = settings.Name;
			while (notebookCategories.NPages > 0) {
				notebookCategories.RemovePage (0);
			}
			if (description != null) {
				foreach (CodeFormatCategory category in description.SubCategories) {
					AddCategoryPage (category);
				}
			}
			notebookCategories.ShowAll ();
		}
		const int keyColumn   = 0;
		const int valueColumn = 1;
		const int objectColumn = 2;

		public CodeFormatSettings Settings {
			get {
				return settings;
			}
		}
		
		void AppendCategory (Gtk.TreeStore store, TreeIter iter, CodeFormatCategory category)
		{
			
			foreach (CodeFormatCategory subCategory in category.SubCategories) {
				TreeIter categoryIter = iter.Equals (TreeIter.Zero) ? store.AppendValues (GettextCatalog.GetString (subCategory.DisplayName), null, subCategory) : store.AppendValues (iter, GettextCatalog.GetString (subCategory.DisplayName), null, subCategory);
				foreach (CodeFormatOption option in subCategory.Options) {
					store.AppendValues (categoryIter, 
					                    GettextCatalog.GetString (option.DisplayName), 
					                    GettextCatalog.GetString (settings.GetValue (description, option)), option);
				}
				foreach (CodeFormatCategory s in subCategory.SubCategories) {
					AppendCategory (store, categoryIter, s);
				}
			}
			
		}
		void AddCategoryPage (CodeFormatCategory category)
		{
			Gtk.Label label = new Gtk.Label (GettextCatalog.GetString (category.DisplayName));
			Gtk.TreeStore store = new Gtk.TreeStore (typeof(string), typeof (string), typeof (object));
			AppendCategory (store, TreeIter.Zero, category);
			Gtk.TreeView tree = new Gtk.TreeView (store);
			tree.AppendColumn (GettextCatalog.GetString ("Key"), new CellRendererText (), "text", keyColumn);
			tree.AppendColumn (GettextCatalog.GetString ("Value"), new CellRendererText (), "text", valueColumn);
			tree.Selection.Changed += delegate {
				if (tree.Selection.GetSelected (out model, out iter)) {

					option =  model.GetValue (iter, objectColumn) as CodeFormatOption;
					this.store = store;
					textviewPreview.Buffer.Text = "";
					if (option == null) {
	//					comboboxentryValue.Sensitive = false;
						return;
					}
					comboboxValue.Changed -= HandleChanged;
	//				comboboxentryValue.Sensitive = true;
					CodeFormatType type = description.GetCodeFormatType (option.Type);
					textviewPreview.Buffer.Text = option.Example;
					comboBoxStore.Clear ();
					int active = 0, i = 0;
					string curValue = settings.GetValue (description, option);
					foreach (string v in type.Values) {
						if (v == curValue)
							active = i;
					 	comboBoxStore.AppendValues (v);
						i++;
					}
					comboboxValue.Active = active;
					comboboxValue.Changed += HandleChanged;
					UpdateExample ();
				}
			};
			ScrolledWindow sw = new ScrolledWindow ();
			sw.Child = tree;
			notebookCategories.AppendPage (sw, label);
		}
	}
}
