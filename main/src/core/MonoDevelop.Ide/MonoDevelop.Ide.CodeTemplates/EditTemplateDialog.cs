// 
// EditTemplateDialog.cs
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Components;
using Gtk;
 
using MonoDevelop.Core;

namespace MonoDevelop.Ide.CodeTemplates
{
	
	
	public partial class EditTemplateDialog : Gtk.Dialog
	{
		CodeTemplate template;
		Mono.TextEditor.TextEditor textEditor = new Mono.TextEditor.TextEditor ();
		
		ListStore variablesStore;
		List<CodeTemplateVariable> variables = new List<CodeTemplateVariable> ();
		
		public EditTemplateDialog (CodeTemplate template, bool isNew)
		{
			this.Build();
			this.Title = isNew ? GettextCatalog.GetString ("New template") : GettextCatalog.GetString ("Edit template");
			this.template = template;
			this.entryShortcut1.Text = template.Shortcut;
			this.comboboxentryGroups.Entry.Text = template.Group;
			this.comboboxentryMime.Entry.Text = template.MimeType;
			this.entryDescription.Text = template.Description;
			this.textEditor.Document.MimeType = template.MimeType;
			this.textEditor.Document.Text = template.Code;
			
			checkbuttonExpansion.Active = (template.CodeTemplateType & CodeTemplateType.Expansion) == CodeTemplateType.Expansion;
			checkbuttonSurroundWith.Active = (template.CodeTemplateType & CodeTemplateType.SurroundsWith) == CodeTemplateType.SurroundsWith;
			
			scrolledwindow1.Child = textEditor;
			textEditor.ShowAll ();
			Mono.TextEditor.TextEditorOptions options = new Mono.TextEditor.TextEditorOptions ();
			options.ShowLineNumberMargin = false;
			options.ShowFoldMargin = false;
			options.ShowIconMargin = false;
			options.ShowInvalidLines = false;
			options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = false;
			options.ColorScheme = PropertyService.Get ("ColorScheme", "Default");
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
			textEditor.Document.TextReplaced += delegate {
				List<string> vars = template.ParseVariables (textEditor.Document.Text);
				foreach (string var in vars) {
					if (!variables.Any (v => v.Name == var) && !template.Variables.Any (v => v.Name == var)) {
						variables.Add (new CodeTemplateVariable (var));
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
			};
			this.buttonOk.Clicked += delegate {
				template.Shortcut = this.entryShortcut1.Text;
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
			};
			
			checkbuttonWhiteSpaces.Toggled += delegate {
				options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = checkbuttonWhiteSpaces.Active;
				textEditor.QueueDraw ();
			};
			
			variablesStore = new ListStore (typeof (CodeTemplateVariable));
			treeviewVariables.Model = variablesStore;
			treeviewVariables.HeadersClickable = true;
			
			#region NameColumn
			TreeViewColumn column;
			CellRendererText nameRenderer = new CellRendererText ();
			column = treeviewVariables.AppendColumn (GettextCatalog.GetString ("Name"), nameRenderer, 
			                                delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				nameRenderer.Text = ((CodeTemplateVariable)model.GetValue (iter, 0)).Name;
			});
			//column.Resizable = true;
			#endregion
			
			#region TipColumn
			CellRendererText tipRenderer = new CellRendererText ();
			tipRenderer.Editable = true;
			tipRenderer.Edited += delegate(object o, EditedArgs args) {
				TreeIter iter;
				if (variablesStore.GetIterFromString (out iter, args.Path)) {
					CodeTemplateVariable var = (CodeTemplateVariable)variablesStore.GetValue (iter, 0);
					var.ToolTip = args.NewText;
				}
			};
			column = treeviewVariables.AppendColumn (GettextCatalog.GetString ("Tooltip"), tipRenderer, 
			                                delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				tipRenderer.Text = ((CodeTemplateVariable)model.GetValue (iter, 0)).ToolTip;
			});
			column.Resizable = true;
			#endregion
			
			#region DefaultValueColumn
			CellRendererText defaultRenderer = new CellRendererText ();
			defaultRenderer.Editable = true;
			defaultRenderer.Edited += delegate(object o, EditedArgs args) {
				TreeIter iter;
				if (variablesStore.GetIterFromString (out iter, args.Path)) {
					CodeTemplateVariable var = (CodeTemplateVariable)variablesStore.GetValue (iter, 0);
					var.Default = args.NewText;
				}
			};
			
			column = treeviewVariables.AppendColumn (GettextCatalog.GetString ("Default"), defaultRenderer, 
			                                delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				defaultRenderer.Text = ((CodeTemplateVariable)model.GetValue (iter, 0)).Default;
			});
			column.Resizable = true;
			#endregion
			
			#region EditableColumn
			CellRendererToggle toggleRenderer = new CellRendererToggle ();
			toggleRenderer.Activatable = true;
			toggleRenderer.Toggled += delegate(object o, ToggledArgs args) {
				TreeIter iter;
				if (variablesStore.GetIterFromString (out iter, args.Path)) {
					CodeTemplateVariable var = (CodeTemplateVariable)variablesStore.GetValue (iter, 0);
					var.IsEditable = !var.IsEditable;
				}
			};
			treeviewVariables.AppendColumn (GettextCatalog.GetString ("Editable"), toggleRenderer, 
			                                delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				toggleRenderer.Active = ((CodeTemplateVariable)model.GetValue (iter, 0)).IsEditable;
			});
			#endregion
			
			#region FunctionColumn
			Gtk.CellRendererCombo cellRendererFunction = new Gtk.CellRendererCombo ();
			cellRendererFunction.Mode = CellRendererMode.Editable;
			cellRendererFunction.Editable = true;
			cellRendererFunction.HasEntry = true;
			cellRendererFunction.TextColumn = 0;
			cellRendererFunction.Edited += delegate(object o, EditedArgs args) {
				TreeIter iter;
				if (variablesStore.GetIterFromString (out iter, args.Path)) {
					CodeTemplateVariable var = (CodeTemplateVariable)variablesStore.GetValue (iter, 0);
					var.Function = args.NewText;
				}
			};
			
			ListStore store = new ListStore (typeof(string));
			ExpansionObject expansion = CodeTemplateService.GetExpansionObject (template);
			foreach (string str in expansion.Descriptions) {
				store.AppendValues (str);
			}
			cellRendererFunction.Model = store;
			
			column = treeviewVariables.AppendColumn (GettextCatalog.GetString ("Function"), cellRendererFunction, 
			                                delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				cellRendererFunction.Text = ((CodeTemplateVariable)model.GetValue (iter, 0)).Function;
			});
			column.Resizable = true;
			#endregion
			
			#region ValueColumn
			CellRendererText valueRenderer = new CellRendererText ();
			valueRenderer.Editable = true;
			valueRenderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
				Console.WriteLine ("Editing Started !!!");
			};
			
			treeviewVariables.AppendColumn (GettextCatalog.GetString ("Values"), valueRenderer, 
			                                delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				CodeTemplateVariable var = (CodeTemplateVariable)model.GetValue (iter, 0);
				if (var.Values.Count == 0) {
					valueRenderer.Markup = "<span foreground=\"" + CodeTemplatePanelWidget.GetColorString (Style.Text (StateType.Insensitive)) + "\">(empty)</span>";
				} else if (var.Values.Count == 1) {
					valueRenderer.Text = var.Values[0];
				} else { 
					valueRenderer.Text = var.Values[0] + ", ...";
				}
			});
			#endregion
			UpdateVariables ();
		}
		
		void UpdateVariables ()
		{
			variablesStore.Clear ();
			foreach (CodeTemplateVariable var in variables) {
				variablesStore.AppendValues (var);
			}
			foreach (CodeTemplateVariable var in template.Variables) {
				variablesStore.AppendValues (var);
			}
			
		}
	}
}
