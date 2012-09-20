// 
// EditTemplateDialog.cs
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Components;
using Gtk;
 
using MonoDevelop.Core;
using Gdk;


namespace MonoDevelop.Ide.CodeTemplates
{
	
	
	public partial class EditTemplateDialog : Gtk.Dialog
	{
		CodeTemplate template;
		Mono.TextEditor.TextEditor textEditor = new Mono.TextEditor.TextEditor ();
		Mono.TextEditor.TextEditorOptions options;
		
		ListStore variablesListStore;
		List<CodeTemplateVariable> variables = new List<CodeTemplateVariable> ();
		MonoDevelop.Components.PropertyGrid.PropertyGrid grid;
		
		TreeStore variableStore;
		
		public EditTemplateDialog (CodeTemplate template, bool isNew)
		{
			this.Build();
			this.Title = isNew ? GettextCatalog.GetString ("New template") : GettextCatalog.GetString ("Edit template");
			this.template = template;
			this.entryShortcut1.Text = template.Shortcut  ?? "";
			this.comboboxentryGroups.Entry.Text = template.Group ?? "";
			this.comboboxentryMime.Entry.Text = template.MimeType ?? "";
			this.entryDescription.Text = template.Description ?? "";
			this.textEditor.Document.MimeType = template.MimeType;
			this.textEditor.Document.Text = template.Code;
			
			checkbuttonExpansion.Active = (template.CodeTemplateType & CodeTemplateType.Expansion) == CodeTemplateType.Expansion;
			checkbuttonSurroundWith.Active = (template.CodeTemplateType & CodeTemplateType.SurroundsWith) == CodeTemplateType.SurroundsWith;
			
			scrolledwindow1.Child = textEditor;
			textEditor.ShowAll ();
			textEditor.Caret.PositionChanged += CaretPositionChanged;
			options = new Mono.TextEditor.TextEditorOptions ();
			options.ShowLineNumberMargin = false;
			options.ShowFoldMargin = false;
			options.ShowIconMargin = false;
			options.ColorScheme = IdeApp.Preferences.ColorScheme;
			textEditor.Options = options;
			
			HashSet<string> mimeTypes = new HashSet<string> ();
			HashSet<string> groups    = new HashSet<string> ();
			foreach (CodeTemplate ct in CodeTemplateService.Templates) {
				mimeTypes.Add (ct.MimeType);
				groups.Add (ct.Group);
			}
			comboboxentryMime.AppendText ("");
			foreach (string mime in mimeTypes) {
				comboboxentryMime.AppendText (mime);
			}
			comboboxentryGroups.AppendText ("");
			foreach (string group in groups) {
				comboboxentryGroups.AppendText (group);
			}
			textEditor.Document.TextReplaced += DocumentTextReplaced;
			this.buttonOk.Clicked += ButtonOkClicked;
			
			checkbuttonWhiteSpaces.Hide ();
			
			variablesListStore = new ListStore (typeof (string), typeof (CodeTemplateVariable));
			comboboxVariables.Model = variablesListStore;
			comboboxVariables.Changed += ComboboxVariablesChanged;
			
			variableStore = new TreeStore (typeof (string), typeof (CodeTemplateVariable), typeof (string), typeof (int));
			treeviewVariable.Model = variableStore;
			treeviewVariable.HeadersVisible = false;
			
			treeviewVariable.AppendColumn ("", new Gtk.CellRendererText (), "text", 0);
			CellRendererText nameRenderer = new CellRendererText ();
			treeviewVariable.AppendColumn ("", nameRenderer, delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				nameRenderer.Markup = ((string)model.GetValue (iter, 2));
			});
			
			grid = new MonoDevelop.Components.PropertyGrid.PropertyGrid ();
			grid.PropertySort = MonoDevelop.Components.PropertyGrid.PropertySort.Alphabetical;
			grid.ShowHelp = true;
			grid.ShowAll ();
			grid.ShowToolbar = false;
			
			vbox4.Remove (scrolledwindow2);
			vbox4.PackEnd (grid, true, true, 0);
			
			UpdateVariables ();
		}

		void ComboboxVariablesChanged (object sender, EventArgs e)
		{
			if (comboboxVariables.Active < 0) {
				this.FillVariableTree (null);
				return;
			}
			TreeIter iter;
			if (variablesListStore.GetIterFromString (out iter, comboboxVariables.Active.ToString ())) {
				this.FillVariableTree ((CodeTemplateVariable)variablesListStore.GetValue (iter, 1));
			} else {
				this.FillVariableTree (null);
			}
		}

		void ButtonOkClicked (object sender, EventArgs e)
		{
			template.Shortcut = this.entryShortcut1.Text.Trim ();
			template.Group = this.comboboxentryGroups.Entry.Text;
			template.MimeType = this.comboboxentryMime.Entry.Text;
			template.Description = this.entryDescription.Text;
			template.Code = this.textEditor.Document.Text;
			variables.ForEach (v => template.AddVariable (v));
			template.CodeTemplateType = CodeTemplateType.Unknown;
			if (checkbuttonExpansion.Active)
				template.CodeTemplateType |= CodeTemplateType.Expansion;
			if (checkbuttonSurroundWith.Active)
				template.CodeTemplateType |= CodeTemplateType.SurroundsWith;
		}

		void DocumentTextReplaced (object sender, Mono.TextEditor.DocumentChangeEventArgs e)
		{
			List<string> vars = template.ParseVariables (textEditor.Document.Text);
			foreach (string var in vars) {
				if (!variables.Any (v => v.Name == var) && !template.Variables.Any (v => v.Name == var)) {
					variables.Add (new CodeTemplateVariable (var) {
						Default = GettextCatalog.GetString ("notset")
					});
				}
			}
			for (int i = 0; i < variables.Count; i++) {
				CodeTemplateVariable var = variables[i];
				if (!vars.Any (v => v == var.Name)) {
					variables.RemoveAt (i);
					i--;
				}
			}
			this.UpdateVariables ();
		}


		void CaretPositionChanged (object sender, Mono.TextEditor.DocumentLocationEventArgs e)
		{
			comboboxVariables.Active = -1;
			int offset = textEditor.Caret.Offset;
			int start = offset;
			while (start >= 0 && start < textEditor.Document.TextLength) { // caret offset may be behind the text
				char ch = textEditor.Document.GetCharAt (start);
				if (ch == '$')
					break;
				if (!char.IsLetterOrDigit (ch) && ch != '_')
					return;
				start--;
			}
			
			int end = offset;
			while (end < textEditor.Document.TextLength) {
				char ch = textEditor.Document.GetCharAt (end);
				if (ch == '$')
					break;
				if (!char.IsLetterOrDigit (ch) && ch != '_')
					return;
				end++;
			}
			if (start >= 0 && end < textEditor.Document.TextLength) {
				string varName = textEditor.Document.GetTextBetween (start, end).Trim ('$');
				TreeIter iter;
				if (variablesListStore.GetIterFirst (out iter)) {
					int i = -1;
					do {
						i++;
						CodeTemplateVariable var = (CodeTemplateVariable)variablesListStore.GetValue (iter, 1);
						if (var.Name == varName) {
							comboboxVariables.Active = i;
							break;
						}
					} while (variablesListStore.IterNext (ref iter));
				}
				
			}
		}
		
		void FillVariableTree (CodeTemplateVariable var)
		{
			grid.CurrentObject = var;
		}
		
		void UpdateVariables ()
		{
			variablesListStore.Clear ();
			foreach (CodeTemplateVariable var in variables) {
				variablesListStore.AppendValues (var.Name, var);
			}
			foreach (CodeTemplateVariable var in template.Variables) {
				variablesListStore.AppendValues (var.Name, var);
			}
			
		}
	}
}
