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
using MonoDevelop.Refactoring;
using MonoDevelop.Core;
using System.Linq;
using System.Text;
using MonoDevelop.SourceEditor;

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
//		string mimeType;
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
		
		public InspectionPanelWidget (string mimeType)
		{
			this.Build ();
//			this.mimeType = mimeType;
			
			
			treeviewInspections.AppendColumn ("Title", new CellRendererText (), "text", 0);
			
			
			var comboRenderer = new CellRendererCombo ();
			var col = treeviewInspections.AppendColumn ("Severity", comboRenderer);
			
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
				Console.WriteLine ("new text:" + args.NewText);
				do {
					if ((string)comboBoxStore.GetValue (storeIter, 0) == args.NewText) {
						treeStore.SetValue (iter, 1, (QuickTaskSeverity)comboBoxStore.GetValue (storeIter, 1));
						return;
					}
				} while (comboBoxStore.IterNext (ref storeIter));
			};
			
			col.SetCellDataFunc (comboRenderer, delegate (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter) {
				var severity = (QuickTaskSeverity)treeStore.GetValue (iter, 1);
				comboRenderer.Text = GetDescription (severity);
			});
			treeviewInspections.HeadersVisible = false;
			treeviewInspections.Model = treeStore;
			treeviewInspections.Selection.Changed += HandleSelectionChanged;

			foreach (var node in RefactoringService.GetInspectors (mimeType)) {
				treeStore.AppendValues (node.Title, node.GetSeverity (), node);
			}
		}
		
		void HandleSelectionChanged (object sender, EventArgs e)
		{
			this.labelDescription.Text = "";
			Gtk.TreeIter iter;
			if (!treeviewInspections.Selection.GetSelected (out iter))
				return;
			var actionNode = (InspectorAddinNode)treeStore.GetValue (iter, 2);
			this.labelDescription.Markup = "<b>" + actionNode.Title + "</b>" + Environment.NewLine + actionNode.Description;
		}

		public void ApplyChanges ()
		{
			Gtk.TreeIter iter;
			if (!treeStore.GetIterFirst (out iter))
				return;
			do {
				var severity = (QuickTaskSeverity)treeStore.GetValue (iter, 1);
				var node = (InspectorAddinNode)treeStore.GetValue (iter, 2);
				node.SetSeverity (severity);
			} while (treeStore.IterNext (ref iter));
		}
	}
}

