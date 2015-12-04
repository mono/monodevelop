//
// NUnitOptionsPanel.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections;

using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Dialogs;
using Gtk;

namespace MonoDevelop.NUnit
{
	public class NUnitOptionsPanel : OptionsPanel
	{
		NUnitOptionsWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			return widget = new NUnitOptionsWidget ((Properties) DataObject);
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ((Properties) DataObject);
		}
	}
	
	partial class NUnitOptionsWidget: Gtk.Bin
	{
		TreeStore store;
		TreeViewColumn textColumn;
		
		UnitTest test;
		string config;
		NUnitCategoryOptions options;
		NUnitCategoryOptions localOptions;

		public NUnitOptionsWidget (Properties customizationObject)
		{
			Build ();
			test =  ((Properties)customizationObject).Get<UnitTest> ("UnitTest");
			config =  ((Properties)customizationObject).Get<string> ("Config");
			options = localOptions = (NUnitCategoryOptions) test.GetOptions (typeof(NUnitCategoryOptions), config);
			
			store = new TreeStore (typeof(string));
			categoryTree.Model = store;
			categoryTree.HeadersVisible = false;
			
			CellRendererText tr = new CellRendererText ();
			tr.Editable = true;
			tr.Edited += new EditedHandler (OnCategoryEdited);
			textColumn = new TreeViewColumn ();
			textColumn.Title = GettextCatalog.GetString ("Category");
			textColumn.PackStart (tr, false);
			textColumn.AddAttribute (tr, "text", 0);
			textColumn.Expand = true;
			categoryTree.AppendColumn (textColumn);
			
			if (test.Parent != null)
				useParentCheck.Active = !test.HasOptions (typeof(NUnitCategoryOptions), config);
			else {
				useParentCheck.Active = false;
				useParentCheck.Sensitive = false;
			}
			
			if (!options.EnableFilter)
				noFilterRadio.Active = true;
			else if (options.Exclude)
				excludeRadio.Active = true;
			else
				includeRadio.Active = true;

			Fill ();
			
			noFilterRadio.Toggled += new EventHandler (OnFilterToggled);
			includeRadio.Toggled += new EventHandler (OnFilterToggled);
			excludeRadio.Toggled += new EventHandler (OnFilterToggled);
			useParentCheck.Toggled += new EventHandler (OnToggledUseParent);
			addButton.Clicked += new EventHandler (OnAddCategory);
			removeButton.Clicked += new EventHandler (OnRemoveCategory);
		}
		
		void Fill ()
		{
			noFilterRadio.Sensitive = !useParentCheck.Active;
			includeRadio.Sensitive = !useParentCheck.Active;
			excludeRadio.Sensitive = !useParentCheck.Active;
			categoryTree.Sensitive = !useParentCheck.Active && !noFilterRadio.Active;
			removeButton.Sensitive = !useParentCheck.Active && !noFilterRadio.Active;
			addButton.Sensitive = !useParentCheck.Active && !noFilterRadio.Active;
			
			store.Clear ();
			foreach (string cat in options.Categories)
				store.AppendValues (cat);
		}
		
		void OnToggledUseParent (object sender, EventArgs args)
		{
			if (useParentCheck.Active)
				options = (NUnitCategoryOptions) test.Parent.GetOptions (typeof(NUnitCategoryOptions), config);
			else
				options = localOptions;

			if (!options.EnableFilter)
				noFilterRadio.Active = true;
			else if (options.Exclude)
				excludeRadio.Active = true;
			else
				includeRadio.Active = true;

			Fill ();
		}
		
		void OnFilterToggled (object sender, EventArgs args)
		{
			options.EnableFilter = !noFilterRadio.Active;
			options.Exclude = excludeRadio.Active;
			Fill ();
		}

		void OnAddCategory (object sender, EventArgs args)
		{
			TreeIter it = store.AppendValues ("");
			categoryTree.SetCursor (store.GetPath (it), textColumn, true);
		}

		void OnRemoveCategory (object sender, EventArgs args)
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!categoryTree.Selection.GetSelected (out foo, out iter))
				return;
			string old = (string) store.GetValue (iter, 0);
			options.Categories.Remove (old);
			store.Remove (ref iter);
		}

		void OnCategoryEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (!store.GetIterFromString (out iter, args.Path))
				return;
			
			string old = (string) store.GetValue (iter, 0);
			if (args.NewText.Length == 0) {
				options.Categories.Remove (old);
				store.Remove (ref iter);
			} else {
				int i = options.Categories.IndexOf (old);
				if (i == -1)
					options.Categories.Add (args.NewText);
				else
					options.Categories [i] = args.NewText;
				store.SetValue (iter, 0, args.NewText);
			}
		}

		public void Store (Properties customizationObject)
		{
			if (useParentCheck.Active)
				test.ResetOptions (typeof(NUnitCategoryOptions), config);
			else
				test.SetOptions (options, config);
		}
	}
}

