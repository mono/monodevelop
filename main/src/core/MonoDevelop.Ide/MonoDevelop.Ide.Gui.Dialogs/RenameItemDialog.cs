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
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.Ide.Gui.Dialogs {
	public partial class RenameItemDialog : Gtk.Dialog {
		ILanguageItem item;
		
		public RenameItemDialog (IParserContext ctx, ILanguageItem item)
		{
			this.item = item;
			
			this.Build ();

			if (item is IClass) {
				if ((((IClass)item).Modifiers & ModifierEnum.Public) == ModifierEnum.Public) {
					this.renameFileFlag.Visible = true;
					this.renameFileFlag.Active = true;
				}
				if (((IClass) item).ClassType == ClassType.Interface)
					this.Title = GettextCatalog.GetString ("Rename Interface");
				else
					this.Title = GettextCatalog.GetString ("Rename Class");
			} else if (item is IField) {
				this.Title = GettextCatalog.GetString ("Rename Field");
			} else if (item is IProperty) {
				this.Title = GettextCatalog.GetString ("Rename Property");
			} else if (item is IEvent) {
				this.Title = GettextCatalog.GetString ("Rename Event");
			} else if (item is IMethod) {
				this.Title = GettextCatalog.GetString ("Rename Method");
			} else if (item is IIndexer) {
				this.Title = GettextCatalog.GetString ("Rename Indexer");
			} else if (item is IParameter) {
				this.Title = GettextCatalog.GetString ("Rename Parameter");
			} else if (item is LocalVariable) {
				this.Title = GettextCatalog.GetString ("Rename Variable");
			} else {
				this.Title = GettextCatalog.GetString ("Rename Item");
			}
			
			entry.Text = item.Name;
			entry.SelectRegion (0, -1);
			
			buttonOk.Sensitive = false;
			entry.Changed += new EventHandler (OnEntryChanged);
			entry.Activated += new EventHandler (OnEntryActivated);
			
			buttonOk.Clicked += new EventHandler (OnOKClicked);
			buttonCancel.Clicked += new EventHandler (OnCancelClicked);
		}
		
		void OnEntryChanged (object sender, EventArgs e)
		{
			// Don't allow the user to click OK unless there is a new name
			buttonOk.Sensitive = entry.Text.Length > 0;
		}
		
		void OnEntryActivated (object sender, EventArgs e)
		{
			if (buttonOk.Sensitive)
				buttonOk.Click ();
		}
		
		void OnCancelClicked (object sender, EventArgs e)
		{
			((Widget) this).Destroy ();
		}
		
		void OnOKClicked (object sender, EventArgs e)
		{
			CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Title, null);
			string name = entry.Text;
			
			if (item is IMember) {
				IMember member = (IMember) item;
				
				refactorer.RenameMember (monitor, member.DeclaringType, member, name, RefactoryScope.Solution);
			} else if (item is IClass) {
				refactorer.RenameClass (monitor, (IClass) item, name, RefactoryScope.Solution);
				if (this.renameFileFlag.Active) {
					IClass cls = ((IClass) item);
					if ((cls.Modifiers & ModifierEnum.Public) == ModifierEnum.Public) {
						foreach (IClass part in cls.Parts) {
							if (System.IO.Path.GetFileNameWithoutExtension(part.Region.FileName) == cls.Name) {
								string newFileName = System.IO.Path.HasExtension(part.Region.FileName) ? name + System.IO.Path.GetExtension(part.Region.FileName) : name;
								newFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(part.Region.FileName), newFileName);
								FileService.RenameFile(part.Region.FileName, newFileName);
							}
						}
						IdeApp.ProjectOperations.Save(IdeApp.ProjectOperations.CurrentSelectedProject);
					}
				}
			} else if (item is LocalVariable) {
				refactorer.RenameVariable (monitor, (LocalVariable) item, name);
			} else if (item is IParameter) {
				refactorer.RenameParameter (monitor, (IParameter) item, name);
			}
			
			((Widget) this).Destroy ();
		}
	}
}
