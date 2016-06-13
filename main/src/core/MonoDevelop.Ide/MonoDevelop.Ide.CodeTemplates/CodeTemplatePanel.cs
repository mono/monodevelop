// 
// CodeTemplatePanel.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.CodeTemplates
{
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class CodeTemplatePanelWidget : Gtk.Bin
	{
		List<CodeTemplate> templates;
		Gtk.TreeStore templateStore;
		CellRendererText   templateCellRenderer;
		CellRendererImage pixbufCellRenderer;
		TextEditor textEditor = TextEditorFactory.CreateNewEditor ();

		public CodeTemplatePanelWidget (OptionsDialog parent)
		{
			this.Build();
			Gtk.Widget control = textEditor;
			scrolledwindow1.Add (control);
			control.ShowAll ();
			
			templateStore = new TreeStore (typeof (CodeTemplate), typeof (string), typeof (string));
			
			
			TreeViewColumn column = new TreeViewColumn ();
			column.Title = GettextCatalog.GetString ("Key");
			
			pixbufCellRenderer = new CellRendererImage ();
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, new Gtk.TreeCellDataFunc (RenderIcon));
			
			templateCellRenderer = new CellRendererText ();
			column.PackStart (templateCellRenderer, true);
			column.SetCellDataFunc (templateCellRenderer, new Gtk.TreeCellDataFunc (RenderTemplateName));
			
			
			treeviewCodeTemplates.AppendColumn (column);
			
			treeviewCodeTemplates.Model = templateStore;
			treeviewCodeTemplates.SearchColumn = -1; // disable the interactive search
			templates = new List<CodeTemplate> (CodeTemplateService.Templates);
			templates.ForEach (t => InsertTemplate (t));
			
			treeviewCodeTemplates.ExpandAll ();
			treeviewCodeTemplates.Selection.Changed += HandleChanged;

			textEditor.Options = DefaultSourceEditorOptions.PlainEditor;
			textEditor.IsReadOnly = true;
			this.buttonAdd.Clicked += ButtonAddClicked;
			this.buttonEdit.Clicked += ButtonEditClicked;
			this.buttonRemove.Clicked += ButtonRemoveClicked;
			this.treeviewCodeTemplates.Selection.Changed += SelectionChanged;
			SelectionChanged (null, null);
			checkbuttonWhiteSpaces.Hide ();
		}

		void SelectionChanged (object sender, EventArgs e)
		{
			buttonRemove.Sensitive = buttonEdit.Sensitive = (GetSelectedTemplate () != null);
		}

		void ButtonRemoveClicked (object sender, EventArgs e)
		{
			TreeIter selected;
			if (treeviewCodeTemplates.Selection.GetSelected (out selected)) {
				var template = (CodeTemplate)templateStore.GetValue (selected, 0);
				if (template != null) {
					if (MessageService.AskQuestion (GettextCatalog.GetString ("Remove template"),
					                                GettextCatalog.GetString ("Are you sure you want to remove this template?"),
					                                AlertButton.Cancel, AlertButton.Remove) == AlertButton.Remove) {
						templates.Remove (template);
						templateStore.Remove (ref selected);
						templatesToRemove.Add (template);
					}
				}
			}
		}
		List<CodeTemplate> templatesToSave = new List<CodeTemplate> ();
		List<CodeTemplate> templatesToRemove = new List<CodeTemplate> ();
		void ButtonEditClicked (object sender, EventArgs e)
		{
			var template = GetSelectedTemplate ();
			if (template != null) {
				templatesToSave.Add (template);
				using (var editDialog = new EditTemplateDialog (template, false))
					if (MessageService.ShowCustomDialog (editDialog, this.Toplevel as Gtk.Window) == (int)ResponseType.Ok)
						templatesToSave.Add (template);
				HandleChanged (this, EventArgs.Empty);
			}
		}
		
		CodeTemplate GetSelectedTemplate ()
		{
			TreeIter selected;
			if (treeviewCodeTemplates.Selection.GetSelected (out selected))
				return (CodeTemplate)templateStore.GetValue (selected, 0);
			return null;
		}

		void ButtonAddClicked (object sender, EventArgs e)
		{
			var newTemplate = new CodeTemplate ();
			using (var editDialog = new EditTemplateDialog (newTemplate, true)) {
				if (MessageService.ShowCustomDialog (editDialog, this.Toplevel as Gtk.Window) == (int)ResponseType.Ok) {
					InsertTemplate (newTemplate);
					templates.Add (newTemplate);
					templatesToSave.Add (newTemplate);
				}
			}
		}
		
		public void Store ()
		{
			templatesToSave.ForEach (CodeTemplateService.SaveTemplate);
			templatesToSave.Clear ();
			templatesToRemove.ForEach (CodeTemplateService.DeleteTemplate);
			templatesToRemove.Clear ();
			CodeTemplateService.Templates = templates;
		}
		
		static void RenderIcon (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CodeTemplate template = (CodeTemplate)model.GetValue (iter, 0);

			var cri = (CellRendererImage)cell;
			if (template == null) {
				cri.Image = ImageService.GetIcon (((TreeView)column.TreeView).GetRowExpanded (model.GetPath (iter)) ? MonoDevelop.Ide.Gui.Stock.OpenFolder : MonoDevelop.Ide.Gui.Stock.ClosedFolder, IconSize.Menu);
			} else {
				cri.Image = ImageService.GetIcon (template.Icon, IconSize.Menu);
			}
				
		}
		
		void RenderTemplateName (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CodeTemplate template = (CodeTemplate)model.GetValue (iter, 0);
			var crt = (CellRendererText)cell;
			if (template == null) {
				crt.Markup = (string)model.GetValue (iter, 2);
				return;
			}
			
			if (((TreeView)column.TreeView).Selection.IterIsSelected (iter)) {
				crt.Markup = GLib.Markup.EscapeText (template.Shortcut) + " (" + 
					GLib.Markup.EscapeText (GettextCatalog.GetString (template.Description)) + ")";
			} else {
				crt.Markup =  GLib.Markup.EscapeText (template.Shortcut) + " <span foreground=\"" + 
					GetColorString (Style.Text (StateType.Insensitive)) + "\">(" 
					+ GLib.Markup.EscapeText (GettextCatalog.GetString (template.Description)) + ")</span>";
			}
		}
		
		void HandleChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			if (treeviewCodeTemplates.Selection.GetSelected (out iter)) {
				CodeTemplate template = templateStore.GetValue (iter, 0) as CodeTemplate;
				if (template != null) {
					textEditor.ClearSelection ();
					textEditor.MimeType = template.MimeType;
					textEditor.Text     = template.Code;
				} else {
					textEditor.Text = "";
				}
			}
		}
		
		TreeIter GetGroup (string groupName)
		{
			TreeIter iter;
			if (templateStore.GetIterFirst (out iter)) {
				do {
					string name = (string)templateStore.GetValue (iter, 1);
					if (name == groupName)
						return iter;
				} while (templateStore.IterNext (ref iter));
			}
			return templateStore.AppendValues (null, groupName, "<b>" + groupName + "</b>");
		}
		
		internal static string GetColorString (Gdk.Color color)
		{
			return string.Format ("#{0:X02}{1:X02}{2:X02}", color.Red / 256, color.Green / 256, color.Blue / 256);
		}
		
		TreeIter InsertTemplate (CodeTemplate template)
		{
			TreeIter iter = GetGroup (template.Group);
			return templateStore.AppendValues (iter, template, template.Shortcut, null);
		}
	}

	internal class CodeTemplatePane : OptionsPanel
	{
		CodeTemplatePanelWidget codeTemplatePanelWidget;
		
		public override Control CreatePanelWidget ()
		{
			
			return codeTemplatePanelWidget = new CodeTemplatePanelWidget (this.ParentDialog);
		}
		
		public override void ApplyChanges ()
		{
			codeTemplatePanelWidget.Store ();
		}
	}
}
