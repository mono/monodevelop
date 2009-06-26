// RenameItemDialog.cs
//
// Author:
//   Jeffrey Stedfast  <fejj@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom.Refactoring;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Refactoring
{
	public partial class RenameItemDialog : Gtk.Dialog
	{
		ProjectDom ctx;
		IDomVisitable item;
		string fileName;
		Rename rename;
		
		public RenameItemDialog (ProjectDom ctx, IDomVisitable item, Rename rename)
		{
			this.ctx  = ctx;
			this.item = item;
			this.rename = rename;

			this.Build ();

			if (item is IType) {
				IType type = (IType)item;
				if (type.IsPublic) {
					this.renameFileFlag.Visible = true;
					this.renameFileFlag.Active = true;
				}
				if (type.ClassType == ClassType.Interface)
					this.Title = GettextCatalog.GetString ("Rename Interface"); else
					this.Title = GettextCatalog.GetString ("Rename Class");
				this.fileName = type.CompilationUnit.FileName;
			} else if (item is IField) {
				this.Title = GettextCatalog.GetString ("Rename Field");
			} else if (item is IProperty) {
				if (((IProperty)item).IsIndexer) {
					this.Title = GettextCatalog.GetString ("Rename Indexer");
				} else {
					this.Title = GettextCatalog.GetString ("Rename Property");
				}
			} else if (item is IEvent) {
				this.Title = GettextCatalog.GetString ("Rename Event");
			} else if (item is IMethod) {
				this.Title = GettextCatalog.GetString ("Rename Method");
			} else if (item is IParameter) {
				this.Title = GettextCatalog.GetString ("Rename Parameter");
			} else if (item is LocalVariable) {
				this.Title = GettextCatalog.GetString ("Rename Variable");
			} else {
				this.Title = GettextCatalog.GetString ("Rename Item");
			}
			
			if (item is IMember) {
				IMember member = (IMember)item;
				entry.Text = member.Name;
				if (!(member is IType) && member.DeclaringType != null)
					this.fileName = member.DeclaringType.CompilationUnit.FileName;
			} else if (item is LocalVariable) {
				LocalVariable lvar = (LocalVariable)item;
				entry.Text = lvar.Name;
				this.fileName = lvar.FileName;
			} else {
				IParameter par = (IParameter)item;
				entry.Text = par.Name;
				this.fileName = par.DeclaringMember.DeclaringType.CompilationUnit.FileName;
			}
			entry.SelectRegion (0, -1);

			buttonPreview.Sensitive = buttonOk.Sensitive = false;
			entry.Changed += OnEntryChanged;
			entry.Activated += OnEntryActivated;

			buttonOk.Clicked += OnOKClicked;
			buttonCancel.Clicked += OnCancelClicked;
			buttonPreview.Clicked += OnPreviewClicked;
			entry.Changed += delegate { buttonPreview.Sensitive = buttonOk.Sensitive = ValidateName (); };
			ValidateName ();
		}

		bool ValidateName ()
		{
			INameValidator nameValidator = MonoDevelop.Projects.LanguageBindingService.GetRefactorerForFile (fileName ?? "default.cs");
			if (nameValidator == null)
				return true;
			ValidationResult result = nameValidator.ValidateName (this.item, entry.Text);
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

		void OnEntryChanged (object sender, EventArgs e)
		{
			// Don't allow the user to click OK unless there is a new name
			buttonPreview.Sensitive = buttonOk.Sensitive = entry.Text.Length > 0;
		}

		void OnEntryActivated (object sender, EventArgs e)
		{
			if (buttonOk.Sensitive)
				buttonOk.Click ();
		}

		void OnCancelClicked (object sender, EventArgs e)
		{
			this.Destroy ();
		}
		
		Rename.RenameProperties Properties {
			get {
				return new Rename.RenameProperties () {
					NewName = entry.Text,
					RenameFile = renameFileFlag.Active
				};
			}
		}
		
		void OnOKClicked (object sender, EventArgs e)
		{
			List<Change> changes = rename.PerformChanges (ctx, item, Properties);
			CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Title, null);
			refactorer.AcceptChanges (monitor, ctx, changes);
			((Widget)this).Destroy ();
		}
		
		void OnPreviewClicked (object sender, EventArgs e)
		{
			List<Change> changes = rename.PerformChanges (ctx, item, Properties);
			((Widget)this).Destroy ();
			RefactoringPreviewDialog refactoringPreviewDialog = new RefactoringPreviewDialog (ctx, changes);
			refactoringPreviewDialog.Show ();
		}
	
	}
}
