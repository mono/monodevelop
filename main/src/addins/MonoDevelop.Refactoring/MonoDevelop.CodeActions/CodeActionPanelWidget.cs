// 
// ContextActionPanelWidget.cs
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
using MonoDevelop.Components;
using MonoDevelop.Core;
using System.Linq;
using System.Text;
using MonoDevelop.Refactoring;
using System.Collections.Generic;
using GLib;
using MonoDevelop.CodeIssues;

namespace MonoDevelop.CodeActions
{
	class CodeActionPanel : OptionsPanel
	{
		ContextActionPanelWidget widget;
		
		public override Control CreatePanelWidget ()
		{
			return widget = new ContextActionPanelWidget ("text/x-csharp");
		}
		
		public override void ApplyChanges ()
		{
			widget.ApplyChanges ();
		}
	}

	partial class ContextActionPanelWidget : Bin
	{
		readonly string mimeType;
		
		readonly TreeStore treeStore = new TreeStore (typeof(string), typeof(bool), typeof(CodeRefactoringDescriptor));
		readonly Dictionary<CodeRefactoringDescriptor, bool> providerStates = new Dictionary<CodeRefactoringDescriptor, bool> ();

		void GetAllProviderStates ()
		{
			var language = CodeRefactoringService.MimeTypeToLanguage (mimeType);
			foreach (var node in BuiltInCodeDiagnosticProvider.GetBuiltInCodeRefactoringDescriptorsAsync (CodeRefactoringService.MimeTypeToLanguage(language), true).Result) {
				providerStates [node] = node.IsEnabled;
			}
		}

		CellRendererToggle togRender = new CellRendererToggle ();
		public ContextActionPanelWidget (string mimeType)
		{
			this.mimeType = mimeType;
			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			this.Build ();

			// ensure selected row remains visible
			treeviewContextActions.SizeAllocated += (o, args) => {
				TreeIter iter;
				if (treeviewContextActions.Selection.GetSelected (out iter)) {
					var path = treeviewContextActions.Model.GetPath (iter);
					treeviewContextActions.ScrollToCell (path, treeviewContextActions.Columns[0], false, 0f, 0f);
				}
			};

			var col = new TreeViewColumn ();
			
			searchentryFilter.ForceFilterButtonVisible = true;
			searchentryFilter.RoundedShape = true;
			searchentryFilter.HasFrame = true;
			searchentryFilter.Ready = true;
			searchentryFilter.Visible = true;
			searchentryFilter.Entry.Changed += ApplyFilter;

			togRender.Toggled += OnActionToggled;
			col.PackStart (togRender, false);
			col.AddAttribute (togRender, "active", 1);
			
			var textRender = new CellRendererText {
				Ellipsize = Pango.EllipsizeMode.End
			};
			col.PackStart (textRender, true);
			col.AddAttribute (textRender, "markup", 0);
			
			treeviewContextActions.AppendColumn (col);
			treeviewContextActions.HeadersVisible = false;
			treeviewContextActions.Model = treeStore;
			treeviewContextActions.SearchColumn = -1; // disable the interactive search
			GetAllProviderStates ();
			FillTreeStore (null);
			treeviewContextActions.TooltipColumn = 3;
			treeviewContextActions.HasTooltip = true;
		}

		void OnActionToggled (object sender, ToggledArgs args)
		{
			TreeIter iter;
			if (!treeStore.GetIterFromString (out iter, args.Path))
				return;
			var provider = (CodeRefactoringDescriptor)treeStore.GetValue (iter, 2);
			providerStates [provider] = !providerStates [provider];
			treeStore.SetValue (iter, 1, providerStates [provider]);
		}

		void ApplyFilter (object sender, EventArgs e)
		{
			FillTreeStore (searchentryFilter.Entry.Text.Trim ());
		}
	
		public void FillTreeStore (string filter)
		{
			treeStore.Clear ();
			var sortedAndFiltered = providerStates.Keys
				.Where (node => !string.IsNullOrEmpty(node.Name) && (string.IsNullOrEmpty (filter) || node.Name.IndexOf (filter, StringComparison.OrdinalIgnoreCase) > 0))
				.OrderBy (n => n.Name, StringComparer.Ordinal);
			foreach (var node in sortedAndFiltered) {
				var title = node.Name;
				MonoDevelop.CodeIssues.CodeIssuePanelWidget.MarkupSearchResult (filter, ref title);
				treeStore.AppendValues (title, providerStates [node], node);
			}
		}

		public void ApplyChanges ()
		{
			foreach (var kv in providerStates) {
				kv.Key.IsEnabled = kv.Value;
			}
		}

		protected override void OnDestroyed()
		{
			togRender.Toggled -= OnActionToggled;
			base.OnDestroyed();
		}
	}
}

