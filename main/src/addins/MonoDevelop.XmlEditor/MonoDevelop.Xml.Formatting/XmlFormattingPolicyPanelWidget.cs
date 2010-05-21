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
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Xml.Formatting
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class XmlFormattingPolicyPanelWidget : Gtk.Bin
	{
		XmlFormattingPolicy policy;
		ListStore store;
		
		public XmlFormattingPolicyPanelWidget ()
		{
			this.Build ();
			
			store = new ListStore (typeof(string), typeof (XmlFormatingSettings));
			listView.Model = store;
			listView.AppendColumn ("", new CellRendererText (), "text", 0);
			
			listView.Selection.Changed += HandleListViewSelectionChanged;
			propertyGrid.ShowToolbar = false;
//			propertyGrid.PropertySort = MonoDevelop.Components.PropertyGrid.PropertySort.NoSort;
			propertyGrid.ShadowType = ShadowType.In;
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
			foreach (XmlFormatingSettings s in policy.Formats)
				AppendSettings (s);
		}

		TreeIter AppendSettings (XmlFormatingSettings format)
		{
			return store.AppendValues (GetName (format), format);
		}
		
		string GetName (XmlFormatingSettings format)
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
				XmlFormatingSettings s = (XmlFormatingSettings) store.GetValue (it, 1);
				FillFormat (s);
			} else
				FillFormat (null);
			UpdateButtons ();
		}
		
		XmlFormatingSettings currentFormat;
		
		void FillFormat (XmlFormatingSettings format)
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
				Label la = new Label (GettextCatalog.GetString ("XPath scope:"));
				la.Xalign = 0;
				tableScopes.Attach (la, 0, 1, n, n + 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
				Entry en = new Entry ();
				en.Text = currentFormat.ScopeXPath[capn];
				tableScopes.Attach (en, 1, 2, n, n + 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
				en.Changed += delegate {
					currentFormat.ScopeXPath [capn] = en.Text;
					UpdateCurrentName ();
				};
				uint c = 2;
				if (currentFormat.ScopeXPath.Count != 1) {
					Button butRem = new Button (ImageService.GetImage (Gtk.Stock.Remove, IconSize.Menu));
					tableScopes.Attach (butRem, 2, 3, n, n + 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
					butRem.Clicked += delegate {
						currentFormat.ScopeXPath.RemoveAt (capn);
						UpdateScopes ();
						UpdateCurrentName ();
					};
					c++;
				}
				if (n == currentFormat.ScopeXPath.Count - 1) {
					Button butAdd = new Button (ImageService.GetImage (Gtk.Stock.Add, IconSize.Menu));
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
		
		protected virtual void OnButtonAddClicked (object sender, System.EventArgs e)
		{
			XmlFormatingSettings format = new XmlFormatingSettings ();
			policy.Formats.Add (format);
			TreeIter it = AppendSettings (format);
			listView.Selection.SelectIter (it);
		}
		
		protected virtual void OnButtonRemoveClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (listView.Selection.GetSelected (out it)) {
				XmlFormatingSettings s = (XmlFormatingSettings) store.GetValue (it, 1);
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
				XmlFormatingSettings s = (XmlFormatingSettings) store.GetValue (it, 1);
				store.SetValue (it, 0, GetName (s));
			}
		}
		
		void UpdateButtons ()
		{
			TreeIter it;
			if (listView.Selection.GetSelected (out it)) {
				XmlFormatingSettings s = (XmlFormatingSettings) store.GetValue (it, 1);
				buttonAdd.Sensitive = true;
				buttonRemove.Sensitive = s != policy.DefaultFormat;
			} else {
				buttonAdd.Sensitive = buttonRemove.Sensitive = false;
			}
		}
		
		protected virtual void OnButtonAdvancedClicked (object sender, System.EventArgs e)
		{
			boxScopes.Show ();
			buttonAdvanced.Hide ();
		}
	}
}

