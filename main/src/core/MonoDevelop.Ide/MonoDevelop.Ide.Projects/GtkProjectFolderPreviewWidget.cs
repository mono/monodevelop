//
// GtkProjectFolderPreviewWidget.cs
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

using System;
using Gdk;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Ide.Projects
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class GtkProjectFolderPreviewWidget : Gtk.Bin
	{
		Pixbuf folderImage;
		Pixbuf fileImage;

		const int TextColumn = 1;
		TreeStore folderTreeStore;
		TreeIter locationNode;
		TreeIter projectFolderNode;
		TreeIter projectNode;
		TreeIter solutionFolderNode;
		TreeIter solutionNode;
		TreeIter gitIgnoreNode;

		FinalProjectConfigurationPage projectConfiguration;

		public GtkProjectFolderPreviewWidget ()
		{
			folderImage = ImageService.GetIcon ("md-open-folder", IconSize.Menu).ToPixbuf ();
			fileImage = ImageService.GetIcon ("md-empty-file-icon", IconSize.Menu).ToPixbuf ();

			this.Build ();
			folderTreeView.ModifyBase (Gtk.StateType.Normal, new Gdk.Color (229, 233, 239));

			CreateFolderTreeViewColumns ();
		}

		void CreateFolderTreeViewColumns ()
		{
			folderTreeStore = new TreeStore (typeof(Pixbuf), typeof(string));
			folderTreeView.Model = folderTreeStore;
			folderTreeView.ShowExpanders = false;
			folderTreeView.LevelIndentation = 10;
			folderTreeView.CanFocus = false;

			var column = new TreeViewColumn ();
			var iconRenderer = new CellRendererPixbuf ();
			column.PackStart (iconRenderer, false);
			column.AddAttribute (iconRenderer, "pixbuf", column: 0);

			var textRenderer = new CellRendererText ();
			textRenderer.Ellipsize = Pango.EllipsizeMode.Middle;
			column.PackStart (textRenderer, true);
			column.AddAttribute (textRenderer, "markup", TextColumn);

			folderTreeView.AppendColumn (column);
		}

		public void Load (FinalProjectConfigurationPage projectConfiguration)
		{
			this.projectConfiguration = projectConfiguration;
			Refresh ();
		}

		public void Refresh ()
		{
			folderTreeStore.Clear ();
			if (projectConfiguration.IsNewSolution) {
				if (projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory) {
					AddProjectWithSolutionDirectoryToTree ();
				} else {
					AddProjectWithNoSolutionDirectoryToTree ();
				}
			} else {
				if (projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory) {
					AddProjectWithNoSolutionDirectoryToTree ();
				} else {
					AddProjectWithNoProjectDirectoryToTree ();
				}
			}

			UpdateTreeValues ();

			folderTreeView.ExpandAll ();
		}

		void AddProjectWithSolutionDirectoryToTree ()
		{
			locationNode = folderTreeStore.AppendValues (folderImage, string.Empty);

			solutionFolderNode = folderTreeStore.AppendValues (locationNode, folderImage, projectConfiguration.DefaultPreviewSolutionName);
			solutionNode = folderTreeStore.AppendValues (solutionFolderNode, fileImage, projectConfiguration.DefaultPreviewSolutionFileName);

			projectFolderNode = folderTreeStore.AppendValues (solutionFolderNode, folderImage, projectConfiguration.DefaultPreviewProjectName);
			gitIgnoreNode = AddGitIgnoreToTree ();
			projectNode = folderTreeStore.AppendValues (projectFolderNode, fileImage, projectConfiguration.DefaultPreviewProjectFileName);
		}

		void UpdateTreeValues ()
		{
			UpdateLocation ();
			UpdateSolutionName ();
			UpdateProjectName ();
			ShowGitIgnoreFile ();
		}

		void AddProjectWithNoSolutionDirectoryToTree ()
		{
			locationNode = folderTreeStore.AppendValues (folderImage, string.Empty);

			projectFolderNode = folderTreeStore.AppendValues (locationNode, folderImage, projectConfiguration.DefaultPreviewProjectName);
			gitIgnoreNode = AddGitIgnoreToTree ();
			projectNode = folderTreeStore.AppendValues (projectFolderNode, fileImage, projectConfiguration.DefaultPreviewProjectFileName);

			solutionFolderNode = TreeIter.Zero;
			solutionNode = TreeIter.Zero;
			if (projectConfiguration.IsNewSolution) {
				solutionNode = folderTreeStore.AppendValues (projectFolderNode, fileImage, projectConfiguration.DefaultPreviewSolutionFileName);
			}
		}

		void AddProjectWithNoProjectDirectoryToTree ()
		{
			locationNode = folderTreeStore.AppendValues (folderImage, string.Empty);

			projectFolderNode = TreeIter.Zero;
			projectNode = folderTreeStore.AppendValues (locationNode, fileImage, projectConfiguration.DefaultPreviewProjectFileName);

			solutionFolderNode = TreeIter.Zero;
			solutionNode = TreeIter.Zero;
		}

		TreeIter AddGitIgnoreToTree ()
		{
			return folderTreeStore.InsertWithValues (projectFolderNode, 0, fileImage, ".gitignore");
		}

		public void UpdateLocation ()
		{
			UpdateTextColumn (locationNode, projectConfiguration.Location);
		}

		void UpdateTextColumn (TreeIter iter, string value)
		{
			folderTreeStore.SetValue (iter, TextColumn, value);
		}

		public void UpdateProjectName ()
		{
			string projectName = projectConfiguration.GetValidProjectName ();
			string projectFileName = projectConfiguration.ProjectFileName;

			if (String.IsNullOrEmpty (projectName)) {
				projectName = projectConfiguration.DefaultPreviewProjectName;
				projectFileName = projectName + projectFileName;
			}
			UpdateTextColumn (projectFolderNode, projectName);
			UpdateTextColumn (projectNode, projectFileName);
		}

		public void UpdateSolutionName ()
		{
			string solutionName = projectConfiguration.GetValidSolutionName ();
			string solutionFileName = projectConfiguration.SolutionFileName;

			if (String.IsNullOrEmpty (solutionName)) {
				solutionName = projectConfiguration.DefaultPreviewSolutionName;
				solutionFileName = solutionName + solutionFileName;
			}

			if (ShowingSolutionFolderNode ()) {
				UpdateTextColumn (solutionFolderNode, solutionName);
			}
			UpdateTextColumn (solutionNode, solutionFileName);
		}

		public void ShowGitIgnoreFile ()
		{
			if (projectConfiguration.IsGitIgnoreEnabled && projectConfiguration.CreateGitIgnoreFile) {
				if (gitIgnoreNode.Equals (TreeIter.Zero)) {
					gitIgnoreNode = AddGitIgnoreToTree ();
				}
			} else {
				folderTreeStore.Remove (ref gitIgnoreNode);
				gitIgnoreNode = TreeIter.Zero;
			}
		}

		bool ShowingSolutionFolderNode ()
		{
			return !solutionFolderNode.Equals (TreeIter.Zero);
		}
	}
}



