// 
// EditVariablesDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Mike Krüger
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

namespace MonoDevelop.Ide.CodeTemplates
{
	
	
	public partial class EditVariablesDialog : Gtk.Dialog
	{
		ListStore store;
		CodeTemplate template;
		CellRendererText cellRendererText;
		
		public EditVariablesDialog (CodeTemplate template)
		{
			this.template = template;
			this.Build();
			store  = new ListStore (typeof (CodeTemplateVariable));
			cellRendererText = new CellRendererText ();
			treeviewVariables.AppendColumn ("name", cellRendererText, new Gtk.TreeCellDataFunc (RenderVariableName));
			treeviewVariables.Model = store;
			treeviewVariables.HeadersVisible = false;
			
			foreach (CodeTemplateVariable var in template.Variables) {
				store.AppendValues (var);
			}
			treeviewVariables.Selection.Changed += HandleChanged;
			
			ExpansionObject expansion = CodeTemplateService.GetExpansionObject (template);
			comboboxEntryExpression.AppendText ("");
			foreach (string description in expansion.Descriptions) {
				comboboxEntryExpression.AppendText (description);
			}
		}
		
		void RenderVariableName (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CodeTemplateVariable var = (CodeTemplateVariable)store.GetValue (iter, 0);
			if (var == null) {
				cellRendererText.Markup = "var == null";
				return;
			}
			if (string.IsNullOrEmpty (var.ToolTip)) {
				cellRendererText.Markup = var.Name;
			} else {
				if (treeviewVariables.Selection.IterIsSelected (iter)) {
					cellRendererText.Markup = var.Name + " (" + var.ToolTip + ")";
				} else {
					cellRendererText.Markup = var.Name + " <span foreground=\"" + CodeTemplatePanelWidget.GetColorString (Style.Text (StateType.Insensitive)) + "\">(" + var.ToolTip + ")</span>";
				}
			}
		}


		void HandleChanged(object sender, EventArgs e)
		{
			TreeIter iter;
			if (treeviewVariables.Selection.GetSelected (out iter)) {
				CodeTemplateVariable var = (CodeTemplateVariable)store.GetValue (iter, 0);
				
				entryDefaultValue.Text = var.Default ?? "";
				entryTooltip.Text = var.ToolTip ?? "";
				comboboxEntryExpression.Entry.Text = var.Function ?? "";
				checkbuttonIsEditable.Active = var.IsEditable;
			}
		}
	}
}
