// 
// IntroduceConstantDialog.cs
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
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Refactoring.IntroduceConstant
{


	public partial class IntroduceConstantDialog : Gtk.Dialog
	{
		RefactoringOperation refactoring;
		RefactoringOptions options;
		IntroduceConstantRefactoring.Parameters parameters;
		
		public IntroduceConstantDialog (RefactoringOperation refactoring, RefactoringOptions options, IntroduceConstantRefactoring.Parameters parameters)
		{
			this.Build ();
			this.refactoring = refactoring;
			this.options = options;
			this.parameters = parameters;
			
			buttonPreview.Sensitive = buttonOk.Sensitive = false;
			entry.Changed += delegate { buttonPreview.Sensitive = buttonOk.Sensitive = ValidateName (); };
			ValidateName ();
			
			buttonOk.Clicked += OnOKClicked;
			buttonPreview.Clicked += OnPreviewClicked;
			
			ListStore modifiers = new ListStore (typeof (string));
			modifiers.AppendValues ("");
			modifiers.AppendValues ("public");
			modifiers.AppendValues ("private");
			modifiers.AppendValues ("protected");
			modifiers.AppendValues ("internal");
			comboboxModifiers.Model = modifiers;
		}
		
		bool ValidateName ()
		{
			string fileName = options.Document.FileName;
			INameValidator nameValidator = MonoDevelop.Projects.LanguageBindingService.GetRefactorerForFile (fileName ?? "default.cs");
			if (nameValidator == null)
				return true;
			ValidationResult result = nameValidator.ValidateName (new DomField (), entry.Text);
			if (!result.IsValid) {
				imageWarning.IconName = Gtk.Stock.DialogError;
			} else if (result.HasWarning) {
				imageWarning.IconName = Gtk.Stock.DialogWarning;
			} else {
				imageWarning.IconName = Gtk.Stock.Apply;
			}
			labelWarning.Text = result.Message;
			return result.IsValid;
		}
		
		void SetProperties ()
		{
			parameters.Name = entry.Text;
			switch (comboboxModifiers.Active) {
			case 0:
				parameters.Modifiers = ICSharpCode.NRefactory.Ast.Modifiers.None;
				break;
			case 1:
				parameters.Modifiers = ICSharpCode.NRefactory.Ast.Modifiers.Public;
				break;
			case 2:
				parameters.Modifiers = ICSharpCode.NRefactory.Ast.Modifiers.Private;
				break;
			case 3:
				parameters.Modifiers = ICSharpCode.NRefactory.Ast.Modifiers.Protected;
				break;
			case 4:
				parameters.Modifiers = ICSharpCode.NRefactory.Ast.Modifiers.Internal;
				break;
			}
		}
		
		void OnOKClicked (object sender, EventArgs e)
		{
			SetProperties ();
			List<Change> changes = refactoring.PerformChanges (options, parameters);
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Title, null);
			RefactoringService.AcceptChanges (monitor, options.Dom, changes);
			((Widget)this).Destroy ();
		}
		
		void OnPreviewClicked (object sender, EventArgs e)
		{
			SetProperties ();
			List<Change> changes = refactoring.PerformChanges (options, parameters);
			((Widget)this).Destroy ();
			
			MessageService.ShowCustomDialog (new RefactoringPreviewDialog (options.Dom, changes));
		}
		
	}
}
