// 
// XmlFormattingPolicyPanelWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Xml.Formatting
{
	class XmlFormattingPolicyPanelWidget : VBox
	{
		XmlFormattingPolicy policy;
		ListStore store;
		
		public XmlFormattingPolicyPanelWidget ()
		{
			Build ();
			
			store = new ListStore (typeof(string), typeof (XmlFormattingSettings));
			listView.Model = store;
			listView.AppendColumn ("", new CellRendererText (), "text", 0);
			
			listView.Selection.Changed += HandleListViewSelectionChanged;
			propertyGrid.ShowToolbar = false;
//			propertyGrid.PropertySort = MonoDevelop.Components.PropertyGrid.PropertySort.NoSort;
			propertyGrid.ShadowType = ShadowType.In;
		}

		VBox boxScopes;
		ScrolledWindow scrolledWindow;
		TreeView listView;
		Button buttonAdd;
		Button buttonRemove;
		Label labelScopes;
		Table tableScopes;
		MonoDevelop.Components.PropertyGrid.PropertyGrid propertyGrid;
		Button buttonAdvanced;

		void Build ()
		{
			Spacing = 6;

			listView = new TreeView {
				CanFocus = true,
				HeadersVisible = false
			};

			scrolledWindow = new ScrolledWindow { ShadowType = ShadowType.In, Child = listView };
			buttonAdd = new Button (Stock.Add);
			buttonRemove = new Button (Stock.Remove);

			labelScopes = new Label {
				Xalign = 0F,
				Text = GettextCatalog.GetString ("Enter one or more XPath expressions to which this format applies")
			};

			tableScopes = new Table (3, 3, false) {
				RowSpacing = 6,
				ColumnSpacing = 6
			};

			propertyGrid = new MonoDevelop.Components.PropertyGrid.PropertyGrid {
				ShowToolbar = false,
				ShowHelp = false
			};

			buttonAdvanced = new Button {
				Label = GettextCatalog.GetString ("Advanced Settings"),
				CanFocus = true,
				UseUnderline = true
			};

			var buttonBox = new HBox (false, 6);
			buttonBox.PackStart (buttonAdd, false, false, 0);
			buttonBox.PackStart (buttonRemove, false, false, 0);

			boxScopes = new VBox (false, 6);
			boxScopes.PackStart (scrolledWindow, true, true, 0);
			boxScopes.PackStart (buttonBox, false, false, 0);

			var rightVBox = new VBox (false, 6);
			rightVBox.PackStart (labelScopes, false, false, 0);
			rightVBox.PackStart (tableScopes, false, false, 0);
			rightVBox.PackStart (propertyGrid, true, true, 0);

			var mainBox = new HBox (false, 6);
			mainBox.PackStart (boxScopes, false, false, 0);
			mainBox.PackStart (rightVBox, true, true, 0);

			var abbBox = new HBox (false, 6);
			abbBox.PackStart (buttonAdvanced, false, false, 0);

			PackStart (mainBox, true, true, 0);
			PackStart (abbBox, false, false, 0);

			ShowAll ();

			buttonAdd.Clicked += OnButtonAddClicked;
			buttonRemove.Clicked += OnButtonRemoveClicked;
			buttonAdvanced.Clicked += OnButtonAdvancedClicked;
		}

		public void CommitPendingChanges ()
		{
			propertyGrid.CommitPendingChanges ();
		}
		
		public void SetFormat (XmlFormattingPolicy policy)
		{
			this.policy = policy;
			Update ();
			TreeIter it;
			if (store.GetIterFirst (out it))
				listView.Selection.SelectIter (it);
			if (policy.Formats.Count == 0) {
				boxScopes.Hide ();
				buttonAdvanced.Show ();
			} else {
				boxScopes.Show ();
				buttonAdvanced.Hide ();
			}
		}
		
		void Update ()
		{
			store.Clear ();
			AppendSettings (policy.DefaultFormat);
			foreach (XmlFormattingSettings s in policy.Formats)
				AppendSettings (s);
		}

		TreeIter AppendSettings (XmlFormattingSettings format)
		{
			return store.AppendValues (GetName (format), format);
		}
		
		string GetName (XmlFormattingSettings format)
		{
			if (format == policy.DefaultFormat)
				return GettextCatalog.GetString ("Default");
			
			string name = "";
			foreach (string s in format.ScopeXPath) {
				if (name.Length != 0)
					name += ", ";
				name += s;
			}
			if (name.Length != 0)
				return name;
			else {
				int i = policy.Formats.IndexOf (format) + 1;
				return string.Format (GettextCatalog.GetString ("Format #{0}"), i);
			}
		}
		
		void HandleListViewSelectionChanged (object sender, EventArgs e)
		{
			TreeIter it;
			if (listView.Selection.GetSelected (out it)) {
				var s = (XmlFormattingSettings) store.GetValue (it, 1);
				FillFormat (s);
			} else
				FillFormat (null);
			UpdateButtons ();
		}
		
		XmlFormattingSettings currentFormat;
		
		void FillFormat (XmlFormattingSettings format)
		{
			currentFormat = format;
			if (currentFormat != null && currentFormat.ScopeXPath.Count == 0)
				currentFormat.ScopeXPath.Add ("");
			propertyGrid.CurrentObject = format;
			UpdateScopes ();
			propertyGrid.Sensitive = currentFormat != null;
		}
		
		void UpdateScopes ()
		{
			if (currentFormat == policy.DefaultFormat || currentFormat == null) {
				tableScopes.Hide ();
				labelScopes.Hide ();
				return;
			}
			labelScopes.Show ();
			
			foreach (Widget w in tableScopes.Children) {
				tableScopes.Remove (w);
				w.Destroy ();
			}
			for (uint n=0; n<currentFormat.ScopeXPath.Count; n++) {
				int capn = (int) n;
				var la = new Label (GettextCatalog.GetString ("XPath scope:"));
				la.Xalign = 0;
				tableScopes.Attach (la, 0, 1, n, n + 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
				var en = new Entry ();
				en.Accessible.SetCommonAttributes("XmlFormattingPolicyPanel.Entry",
				                                  null, GettextCatalog.GetString("Enter a new XPath expression to which this format applies"));
				en.Accessible.SetTitleUIElement(la.Accessible);
				la.Accessible.SetTitleFor(en.Accessible);

				en.Text = currentFormat.ScopeXPath[capn];
				tableScopes.Attach (en, 1, 2, n, n + 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
				en.Changed += delegate {
					currentFormat.ScopeXPath [capn] = en.Text;
					UpdateCurrentName ();
				};
				uint c = 2;
				if (currentFormat.ScopeXPath.Count != 1) {
					var butRem = new Button (new ImageView (Stock.Remove, IconSize.Menu));
					butRem.Accessible.SetCommonAttributes("XmlFormattingPolicyPanel.Remove",
					                                      GettextCatalog.GetString("Remove Scope"),
					                                      GettextCatalog.GetString("Remove this scope expression"));
					tableScopes.Attach (butRem, 2, 3, n, n + 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
					butRem.Clicked += delegate {
						currentFormat.ScopeXPath.RemoveAt (capn);
						UpdateScopes ();
						UpdateCurrentName ();
					};
					c++;
				}
				if (n == currentFormat.ScopeXPath.Count - 1) {
					var butAdd = new Button (new ImageView (Stock.Add, IconSize.Menu));
					butAdd.Accessible.SetCommonAttributes("XmlFormattingPolicyPanel.Add",
					                                      GettextCatalog.GetString("Add Scope"),
					                                      GettextCatalog.GetString("Add a new scope expression"));
					tableScopes.Attach (butAdd, c, c + 1, n, n + 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
					butAdd.Clicked += delegate {
						currentFormat.ScopeXPath.Add ("");
						UpdateScopes ();
						UpdateCurrentName ();
					};
				}
			}

			tableScopes.ShowAll ();
		}
		
		protected virtual void OnButtonAddClicked (object sender, EventArgs e)
		{
			var format = new XmlFormattingSettings ();
			policy.Formats.Add (format);
			TreeIter it = AppendSettings (format);
			listView.Selection.SelectIter (it);
		}
		
		protected virtual void OnButtonRemoveClicked (object sender, EventArgs e)
		{
			TreeIter it;
			if (listView.Selection.GetSelected (out it)) {
				var s = (XmlFormattingSettings) store.GetValue (it, 1);
				policy.Formats.Remove (s);
				TreePath p = store.GetPath (it);
				store.Remove (ref it);
				if (store.IterIsValid (it))
					listView.Selection.SelectIter (it);
				else {
					if (p.Prev ()) {
						store.GetIter (out it, p);
						listView.Selection.SelectIter (it);
					}
				}
			}
		}
		
		void UpdateCurrentName ()
		{
			TreeIter it;
			if (listView.Selection.GetSelected (out it)) {
				var s = (XmlFormattingSettings) store.GetValue (it, 1);
				store.SetValue (it, 0, GetName (s));
			}
		}
		
		void UpdateButtons ()
		{
			TreeIter it;
			if (listView.Selection.GetSelected (out it)) {
				var s = (XmlFormattingSettings) store.GetValue (it, 1);
				buttonAdd.Sensitive = true;
				buttonRemove.Sensitive = s != policy.DefaultFormat;
			} else {
				buttonAdd.Sensitive = buttonRemove.Sensitive = false;
			}
		}
		
		protected virtual void OnButtonAdvancedClicked (object sender, EventArgs e)
		{
			boxScopes.Show ();
			buttonAdvanced.Hide ();
		}
	}
}

