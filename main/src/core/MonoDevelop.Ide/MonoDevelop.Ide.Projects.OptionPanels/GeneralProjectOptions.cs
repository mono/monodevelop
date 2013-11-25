// GeneralProjectOptions.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Ide.Gui.Dialogs;

using Gtk;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal class GeneralProjectOptions : ItemOptionsPanel
	{
		GeneralProjectOptionsWidget widget;

		public override Widget CreatePanelWidget()
		{
			return widget = new GeneralProjectOptionsWidget (ConfiguredProject, ParentDialog);
		}
		
		public override void ApplyChanges()
		{
			widget.Store ();
		}
	}

	partial class GeneralProjectOptionsWidget : Gtk.Bin
	{
		Project project;
		OptionsDialog dialog;

		public GeneralProjectOptionsWidget (Project project, OptionsDialog dialog)
		{
			Build ();
			
			this.project = project;
			this.dialog = dialog;
			
			nameLabel.UseUnderline = true;
			
			descriptionLabel.UseUnderline = true;

			projectNameEntry.Text = project.Name;
			projectDescriptionTextView.Buffer.Text = project.Description;

			if (project is DotNetProject) {
				projectDefaultNamespaceEntry.Text = ((DotNetProject)project).DefaultNamespace;
			} else {
				defaultNamespaceLabel.Visible = false;
				projectDefaultNamespaceEntry.Visible = false;
			}
			
			switch (project.NewFileSearch) 
			{
			case NewFileSearch.None:
				newFilesOnLoadCheckButton.Active = false; 
				autoInsertNewFilesCheckButton.Active = false;
				break;
			case NewFileSearch.OnLoad:
				newFilesOnLoadCheckButton.Active = true; 
				autoInsertNewFilesCheckButton.Active = false;
				break;
			default:
				newFilesOnLoadCheckButton.Active = true; 
				autoInsertNewFilesCheckButton.Active = true;
				break;
			}
			
			entryVersion.Text = project.Version;
			checkSolutionVersion.Active = project.SyncVersionWithSolution;
			entryVersion.Sensitive = !project.SyncVersionWithSolution;
			
			newFilesOnLoadCheckButton.Clicked += new EventHandler(AutoLoadCheckBoxCheckedChangeEvent);
			AutoLoadCheckBoxCheckedChangeEvent(null, null);
		}			

		void AutoLoadCheckBoxCheckedChangeEvent(object sender, EventArgs e)
		{
			autoInsertNewFilesCheckButton.Sensitive = newFilesOnLoadCheckButton.Active;
			if (newFilesOnLoadCheckButton.Active == false) 
				autoInsertNewFilesCheckButton.Active = false;
		}
		
		public void Store ()
		{
			if (projectNameEntry.Text != project.Name) {
				ProjectOptionsDialog.RenameItem (project, projectNameEntry.Text);
				if (project.ParentSolution != null)
					dialog.ModifiedObjects.Add (project.ParentSolution);
			}
			
			project.Description = projectDescriptionTextView.Buffer.Text;
			if (project is DotNetProject) {
				((DotNetProject)project).DefaultNamespace = projectDefaultNamespaceEntry.Text;
			}
			
			if (newFilesOnLoadCheckButton.Active) {
				project.NewFileSearch = autoInsertNewFilesCheckButton.Active ?  NewFileSearch.OnLoadAutoInsert : NewFileSearch.OnLoad;
			} else {
				project.NewFileSearch = NewFileSearch.None;
			}
			if (checkSolutionVersion.Active)
				project.SyncVersionWithSolution = true;
			else {
				project.SyncVersionWithSolution = false;
				project.Version = entryVersion.Text;
			}
		}

		protected virtual void OnCheckSolutionVersionClicked (object sender, System.EventArgs e)
		{
			entryVersion.Sensitive = !checkSolutionVersion.Active;
			if (!entryVersion.Sensitive)
				entryVersion.Text = project.ParentSolution.Version;
		}
	}

}

