// 
// RenameItemDialog.cs
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
using System.Collections.Generic;
using MonoDevelop.Ide;
using MonoDevelop.Ide.ProgressMonitoring;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Refactoring.Rename
{
	public partial class RenameItemDialog : Gtk.Dialog
	{
		readonly ISymbol symbol;
		readonly RenameRefactoring rename;
		readonly List<Tuple<string, TextSpan>> locations;
		
		public RenameItemDialog (ISymbol symbol, List<Tuple<string, TextSpan>> locations, RenameRefactoring rename)
		{
			this.symbol = symbol;
			this.rename = rename;
			this.locations = locations;
			
			this.Build ();
			includeOverloadsCheckbox.Active = true;
			includeOverloadsCheckbox.Visible = false;
			if (symbol is ITypeSymbol) {

				var t = (ITypeSymbol)symbol;
				if (t.TypeKind == TypeKind.TypeParameter) {
					this.Title = GettextCatalog.GetString ("Rename Type Parameter");
					entry.Text = t.Name;

				} else {
					var typeDefinition = t;
					if (typeDefinition.ContainingType == null) {
						// not supported for inner types
						this.renameFileFlag.Visible = true;
						this.renameFileFlag.Active = t.Locations.First ().FilePath.Contains (typeDefinition.Name);
					} else {
						this.renameFileFlag.Active = false;
					}
					if (typeDefinition.TypeKind == TypeKind.Interface)
						this.Title = GettextCatalog.GetString ("Rename Interface");
					else if (typeDefinition.TypeKind == TypeKind.Delegate)
						this.Title = GettextCatalog.GetString ("Rename Delegate");
					else if (typeDefinition.TypeKind == TypeKind.Enum)
						this.Title = GettextCatalog.GetString ("Rename Enum");
					else if (typeDefinition.TypeKind == TypeKind.Struct)
						this.Title = GettextCatalog.GetString ("Rename Struct");
					else
						this.Title = GettextCatalog.GetString ("Rename Class");
				}
				//				this.fileName = type.GetDefinition ().Region.FileName;
			} else if (symbol.Kind == SymbolKind.Field) {
				this.Title = GettextCatalog.GetString ("Rename Field");
			} else if (symbol.Kind == SymbolKind.Property) {
				this.Title = GettextCatalog.GetString ("Rename Property");
			} else if (symbol.Kind == SymbolKind.Event) {
				this.Title = GettextCatalog.GetString ("Rename Event");
			} else if (symbol.Kind == SymbolKind.Method) { 
				var m = (IMethodSymbol)symbol;
				if (m.MethodKind == MethodKind.Constructor ||
					m.MethodKind == MethodKind.StaticConstructor ||
					m.MethodKind == MethodKind.Destructor) {
					this.Title = GettextCatalog.GetString ("Rename Class");
				} else {
					this.Title = GettextCatalog.GetString ("Rename Method");
					includeOverloadsCheckbox.Visible = m.ContainingType.GetMembers (m.Name).Count () > 1;
				}
			} else if (symbol.Kind == SymbolKind.Parameter) {
				this.Title = GettextCatalog.GetString ("Rename Parameter");
			} else if (symbol.Kind == SymbolKind.Local) {
				this.Title = GettextCatalog.GetString ("Rename Variable");
			} else if (symbol.Kind == SymbolKind.TypeParameter) {
				this.Title = GettextCatalog.GetString ("Rename Type Parameter");
			} else if (symbol.Kind == SymbolKind.Namespace) {
				this.Title = GettextCatalog.GetString ("Rename Namespace");
			} if (symbol.Kind == SymbolKind.Label) {
				this.Title = GettextCatalog.GetString ("Rename Label");
			} else {
				this.Title = GettextCatalog.GetString ("Rename Item");
			}
			entry.Text = symbol.Name;
			entry.SelectRegion (0, -1);
			
			buttonPreview.Sensitive = buttonOk.Sensitive = false;
			entry.Changed += OnEntryChanged;
			entry.Activated += OnEntryActivated;
			
			buttonOk.Clicked += OnOKClicked;
			buttonPreview.Clicked += OnPreviewClicked;
			entry.Changed += delegate { buttonPreview.Sensitive = buttonOk.Sensitive = ValidateName (); };
			ValidateName ();
		}

		bool ValidateName ()
		{
			return true; // TODO: Name validation.
//			var nameValidator = MonoDevelop.Projects.LanguageBindingService.GetRefactorerForFile (fileName ?? "default.cs");
//			if (nameValidator == null)
//				return true;
//			ValidationResult result = nameValidator.ValidateName (this.options.SelectedItem, entry.Text);
//			if (!result.IsValid) {
//				imageWarning.IconName = Gtk.Stock.DialogError;
//			} else if (result.HasWarning) {
//				imageWarning.IconName = Gtk.Stock.DialogWarning;
//			} else {
//				imageWarning.IconName = Gtk.Stock.Apply;
//			}
//			labelWarning.Text = result.Message;
//			return result.IsValid;
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
		
		RenameRefactoring.RenameProperties Properties {
			get {
				return new RenameRefactoring.RenameProperties () {
					NewName = entry.Text,
					RenameFile = renameFileFlag.Visible && renameFileFlag.Active,
					IncludeOverloads = includeOverloadsCheckbox.Visible && includeOverloadsCheckbox.Active
				};
			}
		}
		
		void OnOKClicked (object sender, EventArgs e)
		{
			var properties = Properties;
			((Widget)this).Destroy ();
			List<Change> changes = rename.PerformChanges (symbol, locations, properties);
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Title, null);
			RefactoringService.AcceptChanges (monitor, changes);
		}
		
		void OnPreviewClicked (object sender, EventArgs e)
		{
			var properties = Properties;
			((Widget)this).Destroy ();
			List<Change> changes = rename.PerformChanges (symbol, locations, properties);
			MessageService.ShowCustomDialog (new RefactoringPreviewDialog (changes));
		}
	}
		
}
