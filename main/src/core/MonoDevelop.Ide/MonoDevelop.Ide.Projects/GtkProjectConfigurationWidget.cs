//
// GtkProjectConfigurationWidget.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System.IO;
using Gtk;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Projects
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class GtkProjectConfigurationWidget : Gtk.Bin
	{
		FinalProjectConfigurationPage projectConfiguration;

		public GtkProjectConfigurationWidget ()
		{
			this.Build ();
			eventBox.ModifyBg (StateType.Normal, new Gdk.Color (229, 233, 239));
			projectConfigurationTableEventBox.ModifyBg (StateType.Normal, new Gdk.Color (255, 255, 255));

			RegisterEvents ();
		}

		void RegisterEvents ()
		{
			locationTextBox.Changed += (sender, e) => OnLocationTextBoxChanged ();
			projectNameTextBox.Changed += (sender, e) => OnProjectNameTextBoxChanged ();
			solutionNameTextBox.Changed += (sender, e) => OnSolutionNameTextBoxChanged ();
			createGitIgnoreFileCheckBox.Clicked += (sender, e) => OnCreateGitIgnoreFileCheckBoxClicked ();
			createProjectWithinSolutionDirectoryCheckBox.Clicked += (sender, e) => OnCreateProjectWithinSolutionDirectoryCheckBoxClicked ();
			browseButton.Clicked += (sender, e) => BrowseButtonClicked ();
		}

		void OnLocationTextBoxChanged ()
		{
			projectConfiguration.Location = locationTextBox.Text;
			projectFolderPreviewWidget.UpdateLocation ();
		}

		void OnProjectNameTextBoxChanged ()
		{
			projectConfiguration.ProjectName = projectNameTextBox.Text;
			solutionNameTextBox.Text = projectConfiguration.SolutionName;
			projectFolderPreviewWidget.UpdateProjectName ();
			projectFolderPreviewWidget.UpdateSolutionName ();
		}

		void OnSolutionNameTextBoxChanged ()
		{
			projectConfiguration.SolutionName = solutionNameTextBox.Text;
			projectFolderPreviewWidget.UpdateSolutionName ();
		}

		void OnCreateGitIgnoreFileCheckBoxClicked ()
		{
			projectConfiguration.CreateGitIgnoreFile = createGitIgnoreFileCheckBox.Active;
			projectFolderPreviewWidget.ShowGitIgnoreFile ();
		}

		void OnCreateProjectWithinSolutionDirectoryCheckBoxClicked ()
		{
			projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory = createProjectWithinSolutionDirectoryCheckBox.Active;
			projectFolderPreviewWidget.Refresh ();
		}

		void BrowseButtonClicked ()
		{
			FilePath startingFolder = GetStartingFolder ();
			FilePath selectedFolder = BrowseForFolder (startingFolder);
			if (selectedFolder != null) {
				locationTextBox.Text = selectedFolder;
			}
		}

		FilePath GetStartingFolder ()
		{
			try {
				FilePath folder = locationTextBox.Text;

				if (!folder.IsNullOrEmpty && !folder.IsDirectory) {
					folder = folder.ParentDirectory;
					if (!folder.IsNullOrEmpty && !folder.IsDirectory)
						folder = FilePath.Null;
				}
				return folder;

			} catch (FileNotFoundException) {
			}

			return FilePath.Null;
		}

		FilePath BrowseForFolder (FilePath startingFolder)
		{
			var dialog = new SelectFolderDialog ();
			if (startingFolder != null)
				dialog.CurrentFolder = startingFolder;

			dialog.TransientFor = Toplevel as Window;

			if (dialog.Run ())
				return dialog.SelectedFile;
			return null;
		}

		public void Load (FinalProjectConfigurationPage projectConfiguration)
		{
			this.projectConfiguration = projectConfiguration;
			LoadWidget ();
		}

		void LoadWidget ()
		{
			projectFolderPreviewWidget.Load (projectConfiguration);
			solutionNameLabel.Text = GetSolutionNameLabel ();
			locationTextBox.Text = projectConfiguration.Location;
			solutionNameTextBox.Text = projectConfiguration.SolutionName;

			solutionNameTextBox.Sensitive = projectConfiguration.IsSolutionNameEnabled;
			projectNameTextBox.Sensitive = projectConfiguration.IsProjectNameEnabled;
			createProjectWithinSolutionDirectoryCheckBox.Sensitive = projectConfiguration.IsCreateProjectDirectoryInsideSolutionDirectoryEnabled;
			useGitCheckBox.Sensitive = projectConfiguration.IsUseGitEnabled;
			createGitIgnoreFileCheckBox.Sensitive = projectConfiguration.IsGitIgnoreEnabled;
		}

		string GetSolutionNameLabel ()
		{
			if (projectConfiguration.IsWorkspace) {
				return GettextCatalog.GetString ("Workspace Name");
			}
			return GettextCatalog.GetString ("Solution Name");
		}
	}
}

