// 
// ExtractMethodDialog.cs
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

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Refactoring.ExtractMethod
{
	public partial class ExtractMethodDialog : Gtk.Dialog
	{
		ProjectDom dom;
		ExtractMethod extractMethod;
		ExtractMethod.ExtractMethodParameters properties;
		ListStore store;
		RefactoringOptions options;
		
		public ExtractMethodDialog (RefactoringOptions options, ExtractMethod extractMethod, ExtractMethod.ExtractMethodParameters properties)
		{
			this.Build ();
			this.options = options;
			this.properties = properties;
			this.extractMethod = extractMethod;
			
			store = new ListStore (typeof (string), typeof (string));
			treeviewParameters.Model = store;
			treeviewParameters.AppendColumn ("Type", new CellRendererText (), "text", 0);
			treeviewParameters.AppendColumn ("Name", new CellRendererText (), "text", 1);
			FillStore ();
			buttonPreview.Sensitive = buttonOk.Sensitive = false;
			entry.Changed += delegate { buttonPreview.Sensitive = buttonOk.Sensitive = ValidateName (); };
			ValidateName ();
			
			buttonOk.Clicked += OnOKClicked;
			buttonCancel.Clicked += OnCancelClicked;
			buttonPreview.Clicked += OnPreviewClicked;
			
			buttonUp.Clicked += delegate {
				List<int> indices = new List<int> ();
				foreach (TreePath path in treeviewParameters.Selection.GetSelectedRows ()) {
					int index = Int32.Parse (path.ToString ());
					if (index > 0) {
						KeyValuePair<string, IReturnType> tmp = properties.Parameters [index - 1];
						properties.Parameters [index - 1] = properties.Parameters [index];
						properties.Parameters [index] = tmp;
						indices.Add (index - 1);
					}
				}
				FillStore ();
				treeviewParameters.Selection.SelectPath (new TreePath (indices.ToArray ()));
			};
			buttonDown.Clicked += delegate {
				List<int> indices = new List<int> ();
				foreach (TreePath path in treeviewParameters.Selection.GetSelectedRows ()) {
					int index = Int32.Parse (path.ToString ());
					if (index + 1 < properties.Parameters.Count) {
						KeyValuePair<string, IReturnType> tmp = properties.Parameters [index + 1];
						properties.Parameters [index + 1] = properties.Parameters [index];
						properties.Parameters [index] = tmp;
						indices.Add (index + 1);
					}
				}
				FillStore ();
				treeviewParameters.Selection.SelectPath (new TreePath (indices.ToArray ()));
			};
			ListStore modifiers = new ListStore (typeof (string));
			modifiers.AppendValues ("");
			modifiers.AppendValues ("public");
			modifiers.AppendValues ("private");
			modifiers.AppendValues ("protected");
			modifiers.AppendValues ("internal");
			comboboxModifiers.Model = modifiers;
		}
		
		void FillStore ()
		{
			store.Clear ();
			foreach (KeyValuePair<string, IReturnType> var in properties.Parameters) {
				store.AppendValues (var.Value != null ? var.Value.ToInvariantString () : "<null>" , var.Key);
			}
		}
		
		bool ValidateName ()
		{
			string fileName = properties.DeclaringMember.DeclaringType.CompilationUnit.FileName;
			INameValidator nameValidator = MonoDevelop.Projects.LanguageBindingService.GetRefactorerForFile (fileName ?? "default.cs");
			if (nameValidator == null)
				return true;
			ValidationResult result = nameValidator.ValidateName (new DomMethod (), entry.Text);
			if (!result.IsValid) {
				imageWarning.IconName = Stock.DialogError;
			} else if (result.HasWarning) {
				imageWarning.IconName = Stock.DialogWarning;
			} else {
				imageWarning.IconName = Stock.Apply;
			}
			labelWarning.Text = result.Message;
			return result.IsValid;
		}
		
		void OnCancelClicked (object sender, EventArgs e)
		{
			this.Destroy ();
		}
		
		void SetProperties ()
		{
			properties.Name = entry.Text;
			properties.GenerateComment = checkbuttonGenerateComment.Active;
			switch (comboboxModifiers.Active) {
			case 0:
				properties.Modifiers = ICSharpCode.NRefactory.Ast.Modifiers.None;
				break;
			case 1:
				properties.Modifiers = ICSharpCode.NRefactory.Ast.Modifiers.Public;
				break;
			case 2:
				properties.Modifiers = ICSharpCode.NRefactory.Ast.Modifiers.Private;
				break;
			case 3:
				properties.Modifiers = ICSharpCode.NRefactory.Ast.Modifiers.Protected;
				break;
			case 4:
				properties.Modifiers = ICSharpCode.NRefactory.Ast.Modifiers.Internal;
				break;
			}
		}
		
		void OnOKClicked (object sender, EventArgs e)
		{
			SetProperties ();
			List<Change> changes = extractMethod.PerformChanges (options, properties);
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Title, null);
			RefactoringService.AcceptChanges (monitor, dom, changes);
			((Widget)this).Destroy ();
		}
		
		void OnPreviewClicked (object sender, EventArgs e)
		{
			SetProperties ();
			List<Change> changes = extractMethod.PerformChanges (options, properties);
			((Widget)this).Destroy ();
			RefactoringPreviewDialog refactoringPreviewDialog = new RefactoringPreviewDialog (dom, changes);
			refactoringPreviewDialog.Show ();
		}
	}
}
