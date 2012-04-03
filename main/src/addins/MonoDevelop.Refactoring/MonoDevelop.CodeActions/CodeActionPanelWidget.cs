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
using MonoDevelop.Core;
using System.Linq;
using System.Text;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CodeActions
{
	public class CodeActionPanel : OptionsPanel
	{
		ContextActionPanelWidget widget;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return widget = new ContextActionPanelWidget ("text/x-csharp");
		}
		
		public override void ApplyChanges ()
		{
			widget.ApplyChanges ();
		}
	}

	public partial class ContextActionPanelWidget : Gtk.Bin
	{
		string mimeType;
		
		Gtk.TreeStore treeStore = new Gtk.TreeStore (typeof(string), typeof(bool), typeof(CodeActionProvider));
		
		public ContextActionPanelWidget (string mimeType)
		{
			this.mimeType = mimeType;
			this.Build ();
			
			var col = new TreeViewColumn ();
			
			searchentryFilter.Ready = true;
			searchentryFilter.Visible = true;
			searchentryFilter.Entry.Changed += ApplyFilter;

			var togRender = new CellRendererToggle ();
			togRender.Toggled += delegate(object o, ToggledArgs args) {
				TreeIter iter;
				if (!treeStore.GetIterFromString (out iter, args.Path)) 
					return;
				bool enabled = (bool)treeStore.GetValue (iter, 1);
				treeStore.SetValue (iter, 1, !enabled);
			};
			col.PackStart (togRender, false);
			col.AddAttribute (togRender, "active", 1);
			
			var textRender = new CellRendererText ();
			col.PackStart (textRender, true);
			col.AddAttribute (textRender, "markup", 0);
			
			treeviewContextActions.AppendColumn (col);
			treeviewContextActions.HeadersVisible = false;
			treeviewContextActions.Model = treeStore;
			
			FillTreeStore (null);
			treeviewContextActions.Selection.Changed += HandleTreeviewContextActionsSelectionChanged;
		}

		void ApplyFilter (object sender, EventArgs e)
		{
			FillTreeStore (searchentryFilter.Entry.Text.Trim ());
		}

		void HandleTreeviewContextActionsSelectionChanged (object sender, EventArgs e)
		{
			this.labelDescription.Text = "";
			Gtk.TreeIter iter;
			if (!treeviewContextActions.Selection.GetSelected (out iter))
				return;
			var actionNode = (CodeActionProvider)treeStore.GetValue (iter, 2);
			this.labelDescription.Markup = "<b>" + actionNode.Title + "</b>" + Environment.NewLine + actionNode.Description;
		}

		public void FillTreeStore (string filter)
		{
			treeStore.Clear ();
			string disabledNodes = PropertyService.Get ("ContextActions." + mimeType, "");
			foreach (var node in RefactoringService.ContextAddinNodes.Where (n => n.MimeType == mimeType)) {
				if (!string.IsNullOrEmpty (filter) && node.Title.IndexOf (filter, StringComparison.OrdinalIgnoreCase) < 0)
					continue;
				
				bool isEnabled = disabledNodes.IndexOf (node.IdString) < 0;
				var title = node.Title;
				if (!string.IsNullOrEmpty (filter)) {
					var idx = title.IndexOf (filter, StringComparison.OrdinalIgnoreCase);
					title = title.Substring (0, idx) + "<span bgcolor=\"yellow\">" + title.Substring (idx, filter.Length) + "</span>" + title.Substring (idx + filter.Length);
				}
				
				treeStore.AppendValues (title, isEnabled, node);
			}
		}
		
		public void ApplyChanges ()
		{
			var sb = new StringBuilder ();
			Gtk.TreeIter iter;
			if (!treeStore.GetIterFirst (out iter))
				return;
			do {
				bool enabled = (bool)treeStore.GetValue (iter, 1);
				var node = (CodeActionProvider)treeStore.GetValue (iter, 2);
				
				if (!enabled) {
					if (sb.Length > 0)
						sb.Append (",");
					sb.Append (node.IdString);
				}
			} while (treeStore.IterNext (ref iter));
			PropertyService.Set ("ContextActions." + mimeType, sb.ToString ());
		}
	}
}

