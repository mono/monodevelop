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
using MonoDevelop.SourceEditor;
using MonoDevelop.Refactoring;
using System.Collections.Generic;
using Mono.TextEditor;
using ICSharpCode.NRefactory.Refactoring;
using GLib;

namespace MonoDevelop.CodeIssues
{
	class CodeIssuePanel : OptionsPanel
	{
		CodeIssuePanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			return widget = new CodeIssuePanelWidget ("text/x-csharp");
		}
		
		public override void ApplyChanges ()
		{
			widget.ApplyChanges ();
		}
	}
	
	partial class CodeIssuePanelWidget : Bin
	{
		readonly string mimeType;
		readonly TreeStore treeStore = new TreeStore (typeof(string), typeof(BaseCodeIssueProvider));
		readonly Dictionary<BaseCodeIssueProvider, Severity> severities = new Dictionary<BaseCodeIssueProvider, Severity> ();

		void GetAllSeverities ()
		{
			foreach (var node in RefactoringService.GetInspectors (mimeType)) {
				severities [node] = node.GetSeverity ();
				if (node.HasSubIssues) {
					foreach (var subIssue in node.SubIssues)
						severities [subIssue] = subIssue.GetSeverity ();
				}
			}
		}

		static string GetDescription (Severity severity)
		{
			switch (severity) {
			case Severity.None:
				return GettextCatalog.GetString ("Do not show");
			case Severity.Error:
				return GettextCatalog.GetString ("Error");
			case Severity.Warning:
				return GettextCatalog.GetString ("Warning");
			case Severity.Hint:
				return GettextCatalog.GetString ("Hint");
			case Severity.Suggestion:
				return GettextCatalog.GetString ("Suggestion");
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		Gdk.Color GetColor (Severity severity)
		{
			switch (severity) {
			case Severity.None:
				return Style.Base (StateType.Normal);
			case Severity.Error:
				return (HslColor)DefaultSourceEditorOptions.Instance.GetColorStyle ().UnderlineError.Color;
			case Severity.Warning:
				return (HslColor)DefaultSourceEditorOptions.Instance.GetColorStyle ().UnderlineWarning.Color;
			case Severity.Hint:
				return (HslColor)DefaultSourceEditorOptions.Instance.GetColorStyle ().UnderlineHint.Color;
			case Severity.Suggestion:
				return (HslColor)DefaultSourceEditorOptions.Instance.GetColorStyle ().UnderlineSuggestion.Color;
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		public void FillInspectors (string filter)
		{
			categories.Clear ();
			treeStore.Clear ();

			var grouped = severities.Keys.OfType<CodeIssueProvider> ()
				.Where (node => string.IsNullOrEmpty (filter) || node.Title.IndexOf (filter, StringComparison.OrdinalIgnoreCase) > 0)
				.GroupBy (node => node.Category)
				.OrderBy (g => g.Key, StringComparer.Ordinal);

			foreach (var g in grouped) {
				TreeIter categoryIter = treeStore.AppendValues ("<b>" + g.Key + "</b>");
				categories [g.Key] = categoryIter;

				foreach (var node in g.OrderBy (n => n.Title, StringComparer.Ordinal)) {
					var title = node.Title;
					MarkupSearchResult (filter, ref title);
					var nodeIter = treeStore.AppendValues (categoryIter, title, node);
					if (node.HasSubIssues) {
						foreach (var subIssue in node.SubIssues) {
							title = subIssue.Title;
							MarkupSearchResult (filter, ref title);
							treeStore.AppendValues (nodeIter, title, subIssue);
						}
					}
				}
			}
			treeviewInspections.ExpandAll ();
		}

		public static void MarkupSearchResult (string filter, ref string title)
		{
			if (!string.IsNullOrEmpty (filter)) {
				var idx = title.IndexOf (filter, StringComparison.OrdinalIgnoreCase);
				if (idx >= 0) {
					title =
						Markup.EscapeText (title.Substring (0, idx)) +
						"<span bgcolor=\"yellow\">" +
						Markup.EscapeText (title.Substring (idx, filter.Length)) +
						"</span>" +
						Markup.EscapeText (title.Substring (idx + filter.Length));
					return;
				}
			}
			title = Markup.EscapeText (title);
		}
		
		public CodeIssuePanelWidget (string mimeType)
		{
			this.mimeType = mimeType;
			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			Build ();

			// lock description label to allocated width so it can wrap
			labelDescription.Wrap = true;
			labelDescription.WidthRequest = 100;
			labelDescription.SizeAllocated += (o, args) => {
				if (labelDescription.WidthRequest != args.Allocation.Width)
					labelDescription.WidthRequest = args.Allocation.Width;
			};

			// ensure selected row remains visible
			treeviewInspections.SizeAllocated += (o, args) => {
				TreeIter iter;
				if (treeviewInspections.Selection.GetSelected (out iter)) {
					var path = treeviewInspections.Model.GetPath (iter);
					treeviewInspections.ScrollToCell (path, treeviewInspections.Columns[0], false, 0f, 0f);
				}
			};

			var cellRendererText = new CellRendererText {
				Ellipsize = Pango.EllipsizeMode.End
			};
			var col1 = treeviewInspections.AppendColumn ("Title", cellRendererText, "markup", 0);
			col1.Expand = true;

			searchentryFilter.Ready = true;
			searchentryFilter.Visible = true;
			searchentryFilter.Entry.Changed += ApplyFilter;

			var comboRenderer = new CellRendererCombo {
				Alignment = Pango.Alignment.Center
			};
			var col = treeviewInspections.AppendColumn ("Severity", comboRenderer);
			col.Sizing = TreeViewColumnSizing.GrowOnly;
			col.MinWidth = 100;
			col.Expand = false;

			var comboBoxStore = new ListStore (typeof(string), typeof(Severity));
			comboBoxStore.AppendValues (GetDescription (Severity.None), Severity.None);
			comboBoxStore.AppendValues (GetDescription (Severity.Error), Severity.Error);
			comboBoxStore.AppendValues (GetDescription (Severity.Warning), Severity.Warning);
			comboBoxStore.AppendValues (GetDescription (Severity.Hint), Severity.Hint);
			comboBoxStore.AppendValues (GetDescription (Severity.Suggestion), Severity.Suggestion);
			comboRenderer.Model = comboBoxStore;
			comboRenderer.Mode = CellRendererMode.Activatable;
			comboRenderer.TextColumn = 0;

			comboRenderer.Editable = true;
			comboRenderer.HasEntry = false;
			
			comboRenderer.Edited += delegate(object o, EditedArgs args) {
				TreeIter iter;
				if (!treeStore.GetIterFromString (out iter, args.Path))
					return;

				TreeIter storeIter;
				if (!comboBoxStore.GetIterFirst (out storeIter))
					return;
				do {
					if ((string)comboBoxStore.GetValue (storeIter, 0) == args.NewText) {
						var provider = (BaseCodeIssueProvider)treeStore.GetValue (iter, 1);
						var severity = (Severity)comboBoxStore.GetValue (storeIter, 1);
						severities[provider] = severity;
						return;
					}
				} while (comboBoxStore.IterNext (ref storeIter));
			};
			
			col.SetCellDataFunc (comboRenderer, delegate (TreeViewColumn treeColumn, CellRenderer cell, TreeModel model, TreeIter iter) {
				var provider = (BaseCodeIssueProvider)model.GetValue (iter, 1);
				if (provider == null) {
					comboRenderer.Visible = false;
					return;
				}
				var severity = severities[provider];
				comboRenderer.Visible = true;
				comboRenderer.Text = GetDescription (severity);
				comboRenderer.BackgroundGdk = GetColor (severity);
			});
			treeviewInspections.HeadersVisible = false;
			treeviewInspections.Model = treeStore;
			treeviewInspections.Selection.Changed += HandleSelectionChanged;
			GetAllSeverities ();
			FillInspectors (null);
		}

		void ApplyFilter (object sender, EventArgs e)
		{
			FillInspectors (searchentryFilter.Entry.Text.Trim ());
		}

		readonly Dictionary<string, TreeIter> categories = new Dictionary<string, TreeIter> ();

		void HandleSelectionChanged (object sender, EventArgs e)
		{
			labelDescription.Visible = false;
			TreeIter iter;
			if (!treeviewInspections.Selection.GetSelected (out iter))
				return;
			var actionNode = (BaseCodeIssueProvider)treeStore.GetValue (iter, 1);
			if (actionNode == null)
				return;
			labelDescription.Visible = true;
			labelDescription.Markup = string.Format (
				"<b>{0}</b>\n{1}",
				Markup.EscapeText (actionNode.Title),
				Markup.EscapeText (actionNode.Description)
			);
		}

		public void ApplyChanges ()
		{
			foreach (var kv in severities)
				kv.Key.SetSeverity (kv.Value);
		}
	}
}

