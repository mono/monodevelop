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
using System.Collections.Generic;
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
			
			buttonExport.Clicked += delegate {
				Gtk.FileChooserDialog dialog = new Gtk.FileChooserDialog (GettextCatalog.GetString ("Export Profile"),
				                                                          this,
				                                                          FileChooserAction.Save,
				                                                          Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, Gtk.Stock.Save, Gtk.ResponseType.Ok);
				dialog.Filter = new FileFilter();
				dialog.Filter.AddPattern ("*.xml");
				if (ResponseType.Ok == (ResponseType)dialog.Run ()) {
//					System.Console.WriteLine("fn:" + dialog.Filename);
					description.ExportSettings (settings, dialog.Filename);
				}
				dialog.Destroy ();
			};
			checkbuttonWhiteSpaces.Toggled += delegate {
				options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = checkbuttonWhiteSpaces.Active;
				this.texteditor1.QueueDraw ();
			};
			comboboxValue.Clear ();
			Gtk.CellRendererText ctx = new Gtk.CellRendererText ();
			comboboxValue.PackStart (ctx, true);
			comboboxValue.AddAttribute (ctx, "text", 1);
			
			comboBoxStore = new ListStore (typeof (string), typeof (string));
			comboboxValue.Model = comboBoxStore;
			
			comboboxValue.Changed += HandleChanged;
			scrolledwindow2.Child = texteditor1;
			scrolledwindow2.ShowAll ();
		}
		Mono.TextEditor.TextEditor texteditor1 = new Mono.TextEditor.TextEditor ();
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
			IPrettyPrinter printer = TextFileService.GetPrettyPrinter (description.MimeType);
			if (printer == null)
				return;
			DotNetProject parent = new DotNetProject ();
			parent.Policies.Set <CodeFormattingPolicy> (new CustomFormattingPolicy (settings));
			texteditor1.Document.Text  = printer.FormatText (parent, texteditor1.Document.Text);
		}
		
		void HandleChanged(object sender, EventArgs e)
		{
			CodeFormatType type = description.GetCodeFormatType (option.Type);
			int a = comboboxValue.Active;
			if (type == null || a < 0 || a >= type.Values.Count)
				return;
			KeyValuePair<string, string> val = type.Values[a];
			settings.SetValue (option, val.Key);
			store.SetValue (iter, valueDisplayTextColumn, GettextCatalog.GetString (val.Value));
			UpdateExample ();
		}
		Mono.TextEditor.TextEditorOptions options = new Mono.TextEditor.TextEditorOptions ();
		public void SetFormat (CodeFormatDescription description, CodeFormatSettings settings)
		{
			this.description = description;
			this.settings    = settings;
			this.entryName.Text = settings.Name;
			this.Title = string.Format (GettextCatalog.GetString ("Edit Profile '{0}'"), settings.Name);
			options.ShowLineNumberMargin = false;
			options.ShowFoldMargin = false;
			options.ShowIconMargin = false;
			options.ShowInvalidLines = false;
			options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = false;
			options.ColorScheme = PropertyService.Get ("ColorScheme", "Default");
			texteditor1.Options = options;
			texteditor1.Document.ReadOnly = true;
			texteditor1.Document.MimeType = description.MimeType;
			while (notebookCategories.NPages > 0) {
				notebookCategories.RemovePage (0);
			}
			
			if (description != null) {
				Gtk.Label label = new Gtk.Label (GettextCatalog.GetString ("Text Style"));
				notebookCategories.AppendPage (new OverrideTextSettingsPolicyWidget (), label);

				foreach (CodeFormatCategory category in description.SubCategories) {
					AddCategoryPage (category);
				}
			}
			notebookCategories.ShowAll ();
		}
		const int keyColumn   = 0;
		const int valueColumn = 1;
		const int valueDisplayTextColumn = 2;
		const int objectColumn = 3;

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
					KeyValuePair<string, string> val = settings.GetValue (description, option);
					store.AppendValues (categoryIter, 
					                    GettextCatalog.GetString (option.DisplayName), 
					                    val.Key,
					                    GettextCatalog.GetString (val.Value), 
					                    option);
				}
				foreach (CodeFormatCategory s in subCategory.SubCategories) {
					AppendCategory (store, categoryIter, s);
				}
			}
			
		}
		void AddCategoryPage (CodeFormatCategory category)
		{
			Gtk.Label label = new Gtk.Label (GettextCatalog.GetString (category.DisplayName));
			Gtk.TreeStore store = new Gtk.TreeStore (typeof(string), typeof (string), typeof (string), typeof (object));
			AppendCategory (store, TreeIter.Zero, category);
			Gtk.TreeView tree = new Gtk.TreeView (store);
			tree.AppendColumn (GettextCatalog.GetString ("Key"), new CellRendererText (), "text", keyColumn);
			tree.AppendColumn (GettextCatalog.GetString ("Value"), new CellRendererText (), "text", valueDisplayTextColumn);
			tree.Selection.Changed += delegate {
				if (tree.Selection.GetSelected (out model, out iter)) {

					option =  model.GetValue (iter, objectColumn) as CodeFormatOption;
					this.store = store;
					if (option == null) {
						texteditor1.Document.Text = "";
	//					comboboxentryValue.Sensitive = false;
						return;
					}
					comboboxValue.Changed -= HandleChanged;
	//				comboboxentryValue.Sensitive = true;
					CodeFormatType type = description.GetCodeFormatType (option.Type);
					texteditor1.Document.Text = option.Example;
					
					comboBoxStore.Clear ();
					int active = 0, i = 0;
					KeyValuePair<string, string> curValue = settings.GetValue (description, option);
					foreach (KeyValuePair<string, string> v in type.Values) {
						if (v.Key == curValue.Key)
							active = i;
					 	comboBoxStore.AppendValues (v.Key, GettextCatalog.GetString (v.Value));
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
