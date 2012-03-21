// 
// InspectionPanelWidget.cs
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
using MonoDevelop.Ide.Gui.Dialogs;
using Gtk;
using MonoDevelop.Core;
using System.Linq;
using System.Text;
using MonoDevelop.SourceEditor;
using MonoDevelop.Refactoring;
using System.Collections.Generic;
using Mono.TextEditor;

namespace MonoDevelop.Inspection
{
	public class InspectionPanel : OptionsPanel
	{
		InspectionPanelWidget widget;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return widget = new InspectionPanelWidget ("text/x-csharp");
		}
		
		public override void ApplyChanges ()
		{
			widget.ApplyChanges ();
		}
	}
	
	[System.ComponentModel.ToolboxItem(true)]
	public partial class InspectionPanelWidget : Gtk.Bin
	{
		readonly string mimeType;
		Gtk.TreeStore treeStore = new Gtk.TreeStore (typeof(string), typeof(QuickTaskSeverity), typeof(InspectorAddinNode));
		
		string GetDescription (QuickTaskSeverity severity)
		{
			switch (severity) {
			case QuickTaskSeverity.None:
				return GettextCatalog.GetString ("Do not show");
			case QuickTaskSeverity.Error:
				return GettextCatalog.GetString ("Error");
			case QuickTaskSeverity.Warning:
				return GettextCatalog.GetString ("Warning");
			case QuickTaskSeverity.Hint:
				return GettextCatalog.GetString ("Hint");
			case QuickTaskSeverity.Suggestion:
				return GettextCatalog.GetString ("Suggestion");
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		Gdk.Color GetColor (QuickTaskSeverity severity)
		{
			switch (severity) {
			case QuickTaskSeverity.None:
				return Style.Base (StateType.Normal);
			case QuickTaskSeverity.Error:
				return (HslColor)DefaultSourceEditorOptions.Instance.GetColorStyle (Style).ErrorUnderline;
			case QuickTaskSeverity.Warning:
				return (HslColor)DefaultSourceEditorOptions.Instance.GetColorStyle (Style).WarningUnderline;
			case QuickTaskSeverity.Hint:
				return (HslColor)DefaultSourceEditorOptions.Instance.GetColorStyle (Style).HintUnderline;
			case QuickTaskSeverity.Suggestion:
				return (HslColor)DefaultSourceEditorOptions.Instance.GetColorStyle (Style).SuggestionUnderline;
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		public void FillInspectors (string filter)
		{
			categories.Clear ();
			treeStore.Clear ();
			foreach (var node in RefactoringService.GetInspectors (mimeType)) {
				if (!string.IsNullOrEmpty (filter) && node.Inspector.Title.IndexOf (filter, StringComparison.OrdinalIgnoreCase) < 0)
					continue;
				Gtk.TreeIter iter;
				if (!categories.TryGetValue (node.Inspector.Category, out iter)) {
					iter = treeStore.AppendValues ("<b>" + node.Inspector.Category + "</b>");
					categories [node.Inspector.Category] = iter;
				}
				var title = node.Inspector.Title;
				if (!string.IsNullOrEmpty (filter)) {
					var idx = title.IndexOf (filter, StringComparison.OrdinalIgnoreCase);
					title = title.Substring (0, idx) + "<span bgcolor=\"yellow\">" + title.Substring (idx, filter.Length) + "</span>" + title.Substring (idx + filter.Length);
				}
				treeStore.AppendValues (iter, title, node.GetSeverity (), node);
			}
			treeviewInspections.ExpandAll ();
		}
		
		public InspectionPanelWidget (string mimeType)
		{
			this.mimeType = mimeType;
			Build ();

			var col1 = treeviewInspections.AppendColumn ("Title", new CellRendererText (), "markup", 0);
			col1.Expand = true;

			searchentryFilter.Ready = true;
			searchentryFilter.Visible = true;
			searchentryFilter.Entry.Changed += ApplyFilter;

			var comboRenderer = new CellRendererCombo ();
			comboRenderer.Alignment = Pango.Alignment.Center;
			var col = treeviewInspections.AppendColumn ("Severity", comboRenderer);
			col.Sizing = TreeViewColumnSizing.GrowOnly;
			col.MinWidth = 100;
			col.Expand = false;

			var comboBoxStore = new ListStore (typeof(string), typeof(QuickTaskSeverity));
			comboBoxStore.AppendValues (GetDescription (QuickTaskSeverity.None), QuickTaskSeverity.None);
			comboBoxStore.AppendValues (GetDescription (QuickTaskSeverity.Error), QuickTaskSeverity.Error);
			comboBoxStore.AppendValues (GetDescription (QuickTaskSeverity.Warning), QuickTaskSeverity.Warning);
			comboBoxStore.AppendValues (GetDescription (QuickTaskSeverity.Hint), QuickTaskSeverity.Hint);
			comboBoxStore.AppendValues (GetDescription (QuickTaskSeverity.Suggestion), QuickTaskSeverity.Suggestion);
			comboRenderer.Model = comboBoxStore;
			comboRenderer.Mode = CellRendererMode.Activatable;
			comboRenderer.TextColumn = 0;

			comboRenderer.Editable = true;
			comboRenderer.HasEntry = false;
			
			comboRenderer.Edited += delegate(object o, Gtk.EditedArgs args) {
				Gtk.TreeIter iter;
				if (!treeStore.GetIterFromString (out iter, args.Path))
					return;
				
				Gtk.TreeIter storeIter;
				if (!comboBoxStore.GetIterFirst (out storeIter))
					return;
				do {
					if ((string)comboBoxStore.GetValue (storeIter, 0) == args.NewText) {
						treeStore.SetValue (iter, 1, (QuickTaskSeverity)comboBoxStore.GetValue (storeIter, 1));
						return;
					}
				} while (comboBoxStore.IterNext (ref storeIter));
			};
			
			col.SetCellDataFunc (comboRenderer, delegate (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter) {
				var val = treeStore.GetValue (iter, 1);
				if (val == null) {
					comboRenderer.Visible = false;
					return;
				}
				var severity = (QuickTaskSeverity)val;
				comboRenderer.Visible = true;
				comboRenderer.Text = GetDescription (severity);
				comboRenderer.BackgroundGdk = GetColor (severity);
			});
			treeviewInspections.HeadersVisible = false;
			treeviewInspections.Model = treeStore;
			treeviewInspections.Selection.Changed += HandleSelectionChanged;

			FillInspectors (null);
		}

		void ApplyFilter (object sender, EventArgs e)
		{
			FillInspectors (searchentryFilter.Entry.Text.Trim ());
		}

		Dictionary<string, TreeIter> categories = new Dictionary<string, TreeIter> ();

		void HandleSelectionChanged (object sender, EventArgs e)
		{
			labelDescription.Text = "";
			Gtk.TreeIter iter;
			if (!treeviewInspections.Selection.GetSelected (out iter))
				return;
			var actionNode = (InspectorAddinNode)treeStore.GetValue (iter, 2);
			if (actionNode != null)
				labelDescription.Markup = "<b>" + actionNode.Inspector.Title + "</b>" + Environment.NewLine + actionNode.Inspector.Description;
		}

		public void ApplyChanges ()
		{
			Gtk.TreeIter iter;
			if (treeStore.GetIterFirst (out iter))
				ApplyChanges (iter);
			
		}

		public void ApplyChanges (Gtk.TreeIter iter)
		{
			do {
				var node = treeStore.GetValue (iter, 2) as InspectorAddinNode;

				if (node != null) {
					var severity = (QuickTaskSeverity)treeStore.GetValue (iter, 1);
					node.SetSeverity (severity);
				}

				TreeIter child;
				if (treeStore.IterChildren (out child, iter)) 
					ApplyChanges (child);
			} while (treeStore.IterNext (ref iter));
			MonoDevelop.SourceEditor.OptionPanels.ColorShemeEditor.RefreshAllColors ();
		}
	}
}

