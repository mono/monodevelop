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
using System.Threading.Tasks;
using RefactoringEssentials;
using System.IO;

namespace MonoDevelop.Refactoring.Rename
{
	public partial class RenameItemDialog : Gtk.Dialog
	{
		Func<RenameRefactoring.RenameProperties, Task<IList<Change>>> rename;

		internal List<string> ChangedDocuments { get; set; }

		public RenameItemDialog (string title, string currentName, Func<RenameRefactoring.RenameProperties, Task<IList<Change>>> renameOperation)
		{
			this.Build ();
			Init (title, currentName, renameOperation);
		}

		public RenameItemDialog (ISymbol symbol, RenameRefactoring rename)
		{
			this.Build ();

			string title;
			if (symbol is ITypeSymbol) {

				var t = (ITypeSymbol)symbol;
				if (t.TypeKind == TypeKind.TypeParameter) {
					title = GettextCatalog.GetString ("Rename Type Parameter");
					entry.Text = t.Name;

				} else {
					var typeDefinition = t;
					if (typeDefinition.ContainingType == null) {
						// not supported for inner types
						this.renameFileFlag.Visible = true;
						this.renameFileFlag.Active = t.Locations.First ().SourceTree.FilePath.Contains (typeDefinition.Name);
					} else {
						this.renameFileFlag.Active = false;
					}
					if (typeDefinition.TypeKind == TypeKind.Interface)
						title = GettextCatalog.GetString ("Rename Interface");
					else if (typeDefinition.TypeKind == TypeKind.Delegate)
						title = GettextCatalog.GetString ("Rename Delegate");
					else if (typeDefinition.TypeKind == TypeKind.Enum)
						title = GettextCatalog.GetString ("Rename Enum");
					else if (typeDefinition.TypeKind == TypeKind.Struct)
						title = GettextCatalog.GetString ("Rename Struct");
					else
						title = GettextCatalog.GetString ("Rename Class");
				}
				//				this.fileName = type.GetDefinition ().Region.FileName;
			} else if (symbol.Kind == SymbolKind.Field) {
				title = GettextCatalog.GetString ("Rename Field");
			} else if (symbol.Kind == SymbolKind.Property) {
				title = GettextCatalog.GetString ("Rename Property");
			} else if (symbol.Kind == SymbolKind.Event) {
				title = GettextCatalog.GetString ("Rename Event");
			} else if (symbol.Kind == SymbolKind.Method) { 
				var m = (IMethodSymbol)symbol;
				if (m.MethodKind == MethodKind.Constructor ||
					m.MethodKind == MethodKind.StaticConstructor ||
					m.MethodKind == MethodKind.Destructor) {
					title = GettextCatalog.GetString ("Rename Class");
				} else {
					title = GettextCatalog.GetString ("Rename Method");
					includeOverloadsCheckbox.Visible = m.ContainingType.GetMembers (m.Name).Length > 1;
				}
			} else if (symbol.Kind == SymbolKind.Parameter) {
				title = GettextCatalog.GetString ("Rename Parameter");
			} else if (symbol.Kind == SymbolKind.Local) {
				title = GettextCatalog.GetString ("Rename Variable");
			} else if (symbol.Kind == SymbolKind.TypeParameter) {
				title = GettextCatalog.GetString ("Rename Type Parameter");
			} else if (symbol.Kind == SymbolKind.Namespace) {
				title = GettextCatalog.GetString ("Rename Namespace");
			} else if (symbol.Kind == SymbolKind.Label) {
				title = GettextCatalog.GetString ("Rename Label");
			} else {
				title = GettextCatalog.GetString ("Rename Item");
			}

			Init (title, symbol.Name, async prop => { return await rename.PerformChangesAsync (symbol, prop); });

			renameFileFlag.Visible = false;

			foreach (var loc in symbol.Locations) {
				if (loc.IsInSource) {
					if (loc.SourceTree == null ||
						!System.IO.File.Exists (loc.SourceTree.FilePath) ||
						GeneratedCodeRecognition.IsFileNameForGeneratedCode (loc.SourceTree.FilePath)) {
						continue;
					} 
					var oldName = System.IO.Path.GetFileNameWithoutExtension (loc.SourceTree.FilePath);
					if (RenameRefactoring.IsCompatibleForRenaming(oldName, symbol.Name)) {
						renameFileFlag.Visible = true;
						break;
					}
				}
			}
		}


		void Init (string title, string currenName, Func<RenameRefactoring.RenameProperties, Task<IList<Change>>> rename)
		{
			this.Title = title;
			this.rename = rename;

			includeOverloadsCheckbox.Active = true;
			includeOverloadsCheckbox.Visible = false;
			entry.Text = currenName;
			entry.SelectRegion (0, -1);

			buttonPreview.Sensitive = buttonOk.Sensitive = false;
			entry.Changed += OnEntryChanged;
			entry.Activated += OnEntryActivated;

			buttonOk.Clicked += OnOKClicked;
			buttonPreview.Clicked += OnPreviewClicked;
			entry.Changed += delegate { buttonPreview.Sensitive = buttonOk.Sensitive = ValidateName (); };
			ValidateName ();
			this.hbox1.HideAll ();
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

		async void OnOKClicked (object sender, EventArgs e)
		{
			var properties = Properties;
			((Widget)this).Destroy ();
			var changes = await this.rename (properties);
			ProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (Title, null);


			if (ChangedDocuments != null) {
				AlertButton result = null;
				var msg = new QuestionMessage ();
				msg.Buttons.Add (AlertButton.MakeWriteable);
				msg.Buttons.Add (AlertButton.Cancel);
				msg.AllowApplyToAll = true;

				foreach (var path in ChangedDocuments) {
					try {
						var attr = File.GetAttributes (path);
						if (attr.HasFlag (FileAttributes.ReadOnly)) {
							msg.Text = GettextCatalog.GetString ("File {0} is read-only", path);
							msg.SecondaryText = GettextCatalog.GetString ("Would you like to make the file writable?");
							result = MessageService.AskQuestion (msg);

							if (result == AlertButton.Cancel) {
								return;
							} else if (result == AlertButton.MakeWriteable) {
								try {
									File.SetAttributes (path, attr & ~FileAttributes.ReadOnly);
								} catch (Exception ex) {
									MessageService.ShowError (ex.Message);
									return;
								}
							}
						}
					} catch (Exception ex) {
						MessageService.ShowError (ex.Message);
						return;
					}
				}
			}
			RefactoringService.AcceptChanges (monitor, changes);
		}
		
		async void OnPreviewClicked (object sender, EventArgs e)
		{
			var properties = Properties;
			((Widget)this).Destroy ();
			var changes = await this.rename (properties);
			using (var dlg = new RefactoringPreviewDialog (changes))
				MessageService.ShowCustomDialog (dlg);
		}
	}
		
}
