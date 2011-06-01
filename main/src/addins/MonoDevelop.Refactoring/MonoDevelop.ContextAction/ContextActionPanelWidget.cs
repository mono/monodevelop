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
using MonoDevelop.Refactoring;
using MonoDevelop.Core;
using System.Linq;
using System.Text;

namespace MonoDevelop.ContextAction
{
	public class ContextActionPanel : OptionsPanel
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
		
		Gtk.TreeStore treeStore = new Gtk.TreeStore (typeof (string), typeof (bool), typeof (ContextActionAddinNode));
		
		public ContextActionPanelWidget (string mimeType)
		{
			this.mimeType = mimeType;
			this.Build ();
			
			var col = new TreeViewColumn ();
			
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
			col.AddAttribute (textRender, "text", 0);
			
			treeviewContextActions.AppendColumn (col);
			treeviewContextActions.HeadersVisible = false;
			treeviewContextActions.Model = treeStore;
			
			FillTreeStore ();
			treeviewContextActions.Selection.Changed += HandleTreeviewContextActionsSelectionChanged;
		}

		void HandleTreeviewContextActionsSelectionChanged (object sender, EventArgs e)
		{
			this.labelDescription.Text = "";
			Gtk.TreeIter iter;
			if (!treeviewContextActions.Selection.GetSelected (out iter))
				return;
			var actionNode = (ContextActionAddinNode)treeStore.GetValue (iter, 2);
			this.labelDescription.Markup = "<b>" + actionNode.Title + "</b>" + Environment.NewLine + actionNode.Description;
		}

		public void FillTreeStore ()
		{
			string disabledNodes = PropertyService.Get ("ContextActions." + mimeType, "");
			foreach (var node in RefactoringService.ContextAddinNodes.Where (n => n.MimeType == mimeType)) {
				bool isEnabled = disabledNodes.IndexOf (node.Type.FullName) < 0;
				treeStore.AppendValues (node.Title, isEnabled, node);
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
				var node = (ContextActionAddinNode)treeStore.GetValue (iter, 2);
				
				if (!enabled) {
					if (sb.Length > 0)
						sb.Append (",");
					sb.Append (node.Type.FullName);
				}
			} while (treeStore.IterNext (ref iter));
			PropertyService.Set ("ContextActions." + mimeType, sb.ToString ());
		}
	}
}

