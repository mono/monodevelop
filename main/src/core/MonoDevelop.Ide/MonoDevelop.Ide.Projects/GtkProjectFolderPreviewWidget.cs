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
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Ide.Gui;
using System.ComponentModel;

namespace MonoDevelop.Ide.Projects
{
	[System.ComponentModel.ToolboxItem (true)]
	partial class GtkProjectFolderPreviewWidget : Gtk.Bin
	{
		const string FolderIconId = "md-open-folder";
		const string FileIconId = "md-empty-file-icon";

		const int TextColumn = 1;
		const int ImageColumn = 2;
		TreeStore folderTreeStore;
		TreeIter locationNode;
		TreeIter projectFolderNode;
		TreeIter projectNode;
		TreeIter solutionFolderNode;
		TreeIter solutionNode;
		TreeIter gitFolderNode;
		TreeIter gitIgnoreNode;

		FinalProjectConfigurationPage projectConfiguration;

		static GtkProjectFolderPreviewWidget ()
		{
			UpdateStyles ();
			Styles.Changed += (sender, e) => UpdateStyles ();
		}

		static void UpdateStyles ()
		{
			var bgColorHex = Styles.ColorGetHex (Styles.NewProjectDialog.ProjectConfigurationRightHandBackgroundColor);

			string rcstyle = "style \"projectFolderPreviewWidget\"\r\n{\r\n" +
				"    base[NORMAL] = \"" + bgColorHex + "\"\r\n" +
				"    GtkTreeView::even-row-color = \"" + bgColorHex + "\"\r\n" +
				"}\r\n";
			rcstyle += "widget \"*projectFolderPreviewWidget*\" style \"projectFolderPreviewWidget\"\r\n";

			Rc.ParseString (rcstyle);
		}

		public GtkProjectFolderPreviewWidget ()
		{
			this.Build ();

			folderTreeView.Name = "projectFolderPreviewWidget";

			previewLabel.LabelProp = String.Format (
				"<span weight='bold' foreground='{0}'>{1}</span>",
				Styles.ColorGetHex (Styles.NewProjectDialog.ProjectConfigurationPreviewLabelColor),
				global::Mono.Unix.Catalog.GetString ("PREVIEW"));

			CreateFolderTreeViewColumns ();

			// Accessibility
			previewLabel.Accessible.Name = "projectFolderPreviewLabel";
			previewLabel.Accessible.SetTitleFor (folderTreeView.Accessible);

			folderTreeView.Accessible.Name = "projectFolderPreviewWidget";
			folderTreeView.Accessible.Description = GettextCatalog.GetString ("A preview of how the folder will look");
			folderTreeView.Accessible.SetTitleUIElement (previewLabel.Accessible);
		}

		void CreateFolderTreeViewColumns ()
		{
			folderTreeStore = new TreeStore (typeof(string), typeof(string), typeof (Xwt.Drawing.Image));
			folderTreeView.Model = folderTreeStore;
			folderTreeView.Selection.SelectFunction = TreeViewSelection;
			folderTreeView.ShowExpanders = false;
			folderTreeView.LevelIndentation = 10;
			folderTreeView.CanFocus = false;

			SemanticModelAttribute modelAttr = new SemanticModelAttribute ("folderTreeStore__IconId", "folderTreeStore__NodeName", "folderTreeStore__Image");
			TypeDescriptor.AddAttributes (folderTreeStore, modelAttr);

			var column = new TreeViewColumn ();
			var iconRenderer = new CellRendererImage ();
			column.PackStart (iconRenderer, false);
			column.AddAttribute (iconRenderer, "stock-id", column: 0);
			column.AddAttribute (iconRenderer, "image", ImageColumn);

			var textRenderer = new CellRendererText ();
			textRenderer.Ellipsize = Pango.EllipsizeMode.Middle;
			column.PackStart (textRenderer, true);
			column.AddAttribute (textRenderer, "markup", TextColumn);

			folderTreeView.AppendColumn (column);
		}

		static bool TreeViewSelection (TreeSelection selection, TreeModel model, TreePath path, bool path_currently_selected)
		{
			return false;
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
				if (!projectConfiguration.HasProjects) {
					AddSolutionToTree ();
				} else if (projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory) {
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
			locationNode = folderTreeStore.AppendValues (FolderIconId, string.Empty);

			solutionFolderNode = folderTreeStore.AppendValues (locationNode, FolderIconId, projectConfiguration.DefaultPreviewSolutionName);
			solutionNode = folderTreeStore.AppendValues (solutionFolderNode, FileIconId, projectConfiguration.DefaultPreviewSolutionFileName);

			projectFolderNode = folderTreeStore.AppendValues (solutionFolderNode, FolderIconId, projectConfiguration.DefaultPreviewProjectName);
			gitFolderNode = AddGitFolderToTree ();
			gitIgnoreNode = AddGitIgnoreToTree ();
			projectNode = folderTreeStore.AppendValues (projectFolderNode, FileIconId, projectConfiguration.DefaultPreviewProjectFileName);
		}

		void UpdateTreeValues ()
		{
			UpdateLocation ();
			UpdateSolutionName ();
			UpdateProjectName ();
			ShowGitFolder ();
			ShowGitIgnoreFile ();
		}

		void AddProjectWithNoSolutionDirectoryToTree ()
		{
			locationNode = folderTreeStore.AppendValues (FolderIconId, string.Empty);

			projectFolderNode = folderTreeStore.AppendValues (locationNode, FolderIconId, projectConfiguration.DefaultPreviewProjectName);
			projectNode = folderTreeStore.AppendValues (projectFolderNode, FileIconId, projectConfiguration.DefaultPreviewProjectFileName);

			solutionFolderNode = TreeIter.Zero;
			solutionNode = TreeIter.Zero;
			if (projectConfiguration.IsNewSolution) {
				solutionNode = folderTreeStore.AppendValues (projectFolderNode, FileIconId, projectConfiguration.DefaultPreviewSolutionFileName);
			}

			gitFolderNode = AddGitFolderToTree ();
			gitIgnoreNode = AddGitIgnoreToTree ();
		}

		void AddProjectWithNoProjectDirectoryToTree ()
		{
			locationNode = folderTreeStore.AppendValues (FolderIconId, string.Empty);

			projectFolderNode = TreeIter.Zero;
			projectNode = folderTreeStore.AppendValues (locationNode, FileIconId, projectConfiguration.DefaultPreviewProjectFileName);

			solutionFolderNode = TreeIter.Zero;
			solutionNode = TreeIter.Zero;
		}

		void AddSolutionToTree ()
		{
			locationNode = folderTreeStore.AppendValues (FolderIconId, string.Empty);

			solutionFolderNode = folderTreeStore.AppendValues (locationNode, FolderIconId, projectConfiguration.DefaultPreviewSolutionName);
			solutionNode = folderTreeStore.AppendValues (solutionFolderNode, FileIconId, projectConfiguration.DefaultPreviewSolutionFileName);

			projectFolderNode = TreeIter.Zero;
			gitFolderNode = TreeIter.Zero;
			gitIgnoreNode = TreeIter.Zero;
			projectNode = TreeIter.Zero;
		}

		TreeIter AddGitFolderToTree ()
		{
			TreeIter parent = solutionFolderNode;
			if (parent.Equals (TreeIter.Zero)) {
				parent = projectFolderNode;
			}
			return folderTreeStore.InsertWithValues (parent, 0, null, GetLightTextMarkup (".git"), GetTransparentIcon (FolderIconId));
		}

		static Xwt.Drawing.Image GetTransparentIcon (IconId iconId)
		{
			return ImageService.GetIcon (iconId, IconSize.Menu).WithAlpha (0.3);
		}

		TreeIter AddGitIgnoreToTree ()
		{
			TreeIter parent = solutionFolderNode;
			if (parent.Equals (TreeIter.Zero)) {
				parent = projectFolderNode;
			}
			return folderTreeStore.InsertWithValues (parent, 1, null, GetLightTextMarkup (".gitignore"), GetTransparentIcon (FileIconId));
		}

		static string GetLightTextMarkup (string text)
		{
			return String.Format ("<span color='#AAAAAA'>{0}</span>", text);
		}

		public void UpdateLocation ()
		{
			UpdateTextColumn (locationNode, projectConfiguration.Location);
		}

		void UpdateTextColumn (TreeIter iter, string value)
		{
			if (!iter.Equals (TreeIter.Zero)) {
				folderTreeStore.SetValue (iter, TextColumn, GLib.Markup.EscapeText (value));
			}
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

		public void ShowGitFolder ()
		{
			if (projectConfiguration.IsUseGitEnabled && projectConfiguration.UseGit && projectConfiguration.IsNewSolution) {
				if (gitFolderNode.Equals (TreeIter.Zero)) {
					gitFolderNode = AddGitFolderToTree ();
				}
			} else if (!gitFolderNode.Equals (TreeIter.Zero)) {
				folderTreeStore.Remove (ref gitFolderNode);
				gitFolderNode = TreeIter.Zero;
			}
		}

		public void ShowGitIgnoreFile ()
		{
			if (projectConfiguration.IsGitIgnoreEnabled && projectConfiguration.CreateGitIgnoreFile && projectConfiguration.IsNewSolution) {
				if (gitIgnoreNode.Equals (TreeIter.Zero)) {
					gitIgnoreNode = AddGitIgnoreToTree ();
				}
			} else if (!gitIgnoreNode.Equals (TreeIter.Zero)) {
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



