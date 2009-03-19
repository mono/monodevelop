// 
// CodeTemplatePanel.cs
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
using System.Collections.Generic;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;

namespace MonoDevelop.Ide.CodeTemplates
{
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class CodeTemplatePanelWidget : Gtk.Bin
	{
		OptionsDialog parent;
		List<CodeTemplate> templates;
		Gtk.TreeStore templateStore;
		CellRendererText templateCellRenderer;
		Mono.TextEditor.TextEditor textEditor = new Mono.TextEditor.TextEditor ();

		public CodeTemplatePanelWidget(OptionsDialog parent)
		{
			this.parent = parent;
			this.Build();
			scrolledwindow1.Child = textEditor;
			textEditor.ShowAll ();
			
			templateStore = new TreeStore (typeof (CodeTemplate), typeof (string), typeof (string));
			templateCellRenderer = new CellRendererText ();
			treeviewCodeTemplates.AppendColumn (GettextCatalog.GetString ("Key"), templateCellRenderer, new Gtk.TreeCellDataFunc (RenderTemplateName));
			treeviewCodeTemplates.Model = templateStore;
			templates = new List<CodeTemplate> (CodeTemplateService.Templates);
			templates.ForEach (t => InsertTemplate (t));
			
			treeviewCodeTemplates.ExpandAll ();
			treeviewCodeTemplates.Selection.Changed += HandleChanged;
			
			Mono.TextEditor.TextEditorOptions options = new Mono.TextEditor.TextEditorOptions ();
			options.ShowLineNumberMargin = false;
			options.ShowFoldMargin = false;
			options.ShowIconMargin = false;
			options.ShowInvalidLines = false;
			options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = false;
			options.ColorScheme = PropertyService.Get ("ColorScheme", "Default");
			textEditor.Options = options;
			textEditor.Document.ReadOnly = true;
			this.buttonAdd.Clicked += delegate {
				CodeTemplate newTemplate = new CodeTemplate ();
				EditTemplateDialog editDialog = new EditTemplateDialog (newTemplate, true);
				
				editDialog.Parent = parent;
				if (ResponseType.Ok == (ResponseType)editDialog.Run ()) {
					InsertTemplate (newTemplate);
					templates.Add (newTemplate);
				}
				editDialog.Destroy ();
			};
			
			this.buttonEdit.Clicked += delegate {
				TreeIter selected;
				if (treeviewCodeTemplates.Selection.GetSelected (out selected)) {
					EditTemplateDialog editDialog = new EditTemplateDialog ((CodeTemplate)templateStore.GetValue (selected, 0), false);
					editDialog.Parent = parent;
					editDialog.Run ();
					editDialog.Destroy ();
				}
			};
			
			this.buttonRemove.Clicked += delegate {
				TreeIter selected;
				if (treeviewCodeTemplates.Selection.GetSelected (out selected)) {
					if (MessageService.AskQuestion (GettextCatalog.GetString ("Remove template"), GettextCatalog.GetString ("Are you sure you want to remove this template?"), AlertButton.Cancel, AlertButton.Remove) == AlertButton.Remove) {
						CodeTemplate template = (CodeTemplate)templateStore.GetValue (selected, 0);
						templates.Remove (template);
						templateStore.Remove (ref selected);
					}
				}
			};
			checkbuttonWhiteSpaces.Toggled += delegate {
				options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = checkbuttonWhiteSpaces.Active;
				textEditor.QueueDraw ();
			};

		}
		
		public void Store ()
		{
			CodeTemplateService.Templates = templates;
			CodeTemplateService.SaveTemplates ();
		}
		
		void RenderTemplateName (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CodeTemplate template = (CodeTemplate)templateStore.GetValue (iter, 0);
			if (template == null) {
				templateCellRenderer.Markup = (string)templateStore.GetValue (iter, 2);
				return;
			}
			
			if (treeviewCodeTemplates.Selection.IterIsSelected (iter)) {
				templateCellRenderer.Markup = template.Shortcut + " (" + template.Description + ")";
			} else {
				templateCellRenderer.Markup = template.Shortcut + " <span foreground=\"" + GetColorString (Style.Text (StateType.Insensitive)) + "\">(" + template.Description + ")</span>";
			}
		}
		
		void HandleChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			if (treeviewCodeTemplates.Selection.GetSelected (out iter)) {
				CodeTemplate template = templateStore.GetValue (iter, 0) as CodeTemplate;
				if (template != null) {
					textEditor.ClearSelection ();
					textEditor.Document.MimeType = template.MimeType;
					textEditor.Document.Text     = template.Code;
				} else {
					textEditor.Document.Text = "";
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
		
		public override Widget CreatePanelWidget ()
		{
			
			return codeTemplatePanelWidget = new CodeTemplatePanelWidget (this.ParentDialog);
		}
		
		public override void ApplyChanges ()
		{
			codeTemplatePanelWidget.Store ();
		}
	}
}
