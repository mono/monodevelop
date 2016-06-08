// 
// NamingConventionPanelWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Diagnostics.InconsistentNaming
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class NameConventionPanelWidget : Gtk.Bin
	{
		TreeStore treeStore = new TreeStore (typeof(NameConventionRule));
		NameConventionPolicy policy;

		internal NameConventionPolicy Policy {
			get {
				return policy;
			}
			set {
				policy = value;
				FillRules (policy.Rules);
			}
		}

		public NameConventionPanelWidget ()
		{
			Build ();	
			Show ();

			var ct1 = new CellRendererText ();
			var col1 = treeviewConventions.AppendColumn (GettextCatalog.GetString ("Rule"), ct1);
			col1.Expand = true;
			col1.SetCellDataFunc (ct1, NameConventionRuleNameDataFunc);
			
			
			var ct2 = new CellRendererText ();
			var col2 = treeviewConventions.AppendColumn (GettextCatalog.GetString ("Example"), ct2);
			col2.Expand = true;
			col2.SetCellDataFunc (ct2, NameConventionRulePreviewDataFunc);
			
			treeviewConventions.Model = treeStore;
			treeviewConventions.SearchColumn = -1; // disable the interactive search
			treeviewConventions.Selection.Changed += HandleSelectionChanged;
			treeviewConventions.RowActivated += (o, args) => EditSelectedEntry ();
			buttonEdit.Clicked += (o, s) => EditSelectedEntry ();
			buttonRemove.Clicked += (o, s) => RemoveSelectedEntry ();
			buttonAdd.Clicked += (o, s) => AddEntry ();

			HandleSelectionChanged (null, null);
		}

		static void NameConventionRuleNameDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var rule = (NameConventionRule)model.GetValue (iter, 0);
			((CellRendererText)cell).Text = rule.Name;
		}

		static void NameConventionRulePreviewDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var rule = (NameConventionRule)model.GetValue (iter, 0);
			((CellRendererText)cell).Text = rule.GetPreview ();
		}

		void HandleSelectionChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			buttonEdit.Sensitive = treeviewConventions.Selection.GetSelected (out iter);
		}

		public void ApplyChanges ()
		{
			var rules = new List<NameConventionRule> ();
			TreeIter iter;
			if (treeStore.GetIterFirst (out iter)) {
				do {
					var rule = (NameConventionRule)treeStore.GetValue (iter, 0);
					rules.Add (rule);
				} while (treeStore.IterNext (ref iter));
			}
			policy.Rules = rules.ToArray ();
			if (IdeApp.Workbench.ActiveDocument != null)
				IdeApp.Workbench.ActiveDocument.UpdateParseDocument ();
		}

		void AddEntry ()
		{
			var newRule = new NameConventionRule ();
			newRule.Name = "New Rule";
			using (var diag = new NameConventionEditRuleDialog (newRule)) {
				var result = MessageService.ShowCustomDialog (diag);
				if (result == (int)ResponseType.Ok)
					treeStore.AppendValues (newRule);
				OnPolicyChanged (EventArgs.Empty);
			}
		}

		void EditSelectedEntry ()
		{
			TreeIter iter;
			if (!treeviewConventions.Selection.GetSelected (out iter))
				return;
			var rule = treeStore.GetValue (iter, 0) as NameConventionRule;
			using (var diag = new NameConventionEditRuleDialog (rule)) {
				int result = MessageService.ShowCustomDialog (diag);
				treeviewConventions.QueueResize ();
				if (result == (int)Gtk.ResponseType.Ok)
					OnPolicyChanged (EventArgs.Empty);
			}
		}
		
		void RemoveSelectedEntry ()
		{
			TreeIter iter;
			if (!treeviewConventions.Selection.GetSelected (out iter))
				return;
			treeStore.Remove (ref iter);
			OnPolicyChanged (EventArgs.Empty);
		}

		void FillRules (IEnumerable<NameConventionRule> rules)
		{
			treeStore.Clear ();
			foreach (var rule in rules) {
				treeStore.AppendValues (rule);
			}
		}

		protected virtual void OnPolicyChanged (EventArgs e)
		{
			var handler = PolicyChanged;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler PolicyChanged;
	}
}

