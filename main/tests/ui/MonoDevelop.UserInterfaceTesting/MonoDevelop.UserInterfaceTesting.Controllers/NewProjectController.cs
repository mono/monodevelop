//
// NewProjectController.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 
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
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Ide.Commands;
using NUnit.Framework;
using System.Threading;
using System.Linq;

namespace MonoDevelop.UserInterfaceTesting.Controllers
{
	public class NewProjectController
	{
		static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		Func<AppQuery, AppQuery> previewTree = c => c.TreeView ().Marked ("folderTreeView").Model ("folderTreeStore__NodeName");
		Func<AppQuery, AppQuery> templateCategoriesQuery = c => c.TreeView ().Marked ("templateCategoriesTreeView").Model ("templateCategoriesListStore__Name");
		Func<AppQuery, AppQuery> templatesQuery = c => c.TreeView ().Marked ("templatesTreeView").Model ("templateListStore__Name");

		public void Open ()
		{
			Session.ExecuteCommand (FileCommands.NewProject);
			WaitForOpen ();
		}

		public void Open (string addToSolutionName)
		{
			SolutionExplorerController.SelectSolution (addToSolutionName);
			Session.ExecuteCommand (ProjectCommands.AddNewProject);
			WaitForOpen ();
		}

		public void WaitForOpen ()
		{
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Projects.GtkNewProjectDialogBackend"));
		}

		public void Select (TemplateSelectionOptions templateOptions)
		{
			SelectTemplateType (templateOptions.CategoryRoot, templateOptions.Category);
			SelectTemplate (templateOptions.TemplateKindRoot, templateOptions.TemplateKind);
		}

		public bool IsSelected  (TemplateSelectionOptions templateOptions)
		{
			return true;
//			return Session.SelectElement (templateCategoriesTreeViewQuery) && IsTemplateTypeSelected (templateOptions.CategoryRoot, templateOptions.Category)
//				&& Session.SelectElement (templatesTreeViewQuery) && IsTemplateSelected (templateOptions.TemplateKindRoot, templateOptions.TemplateKind);
		}

		public bool SelectTemplateType (string categoryRoot, string category)
		{
			return Session.SelectElement (c => templateCategoriesQuery (c).Contains (categoryRoot).NextSiblings ().Text (category))
				&& IsTemplateTypeSelected (categoryRoot, category);
		}

		public bool SelectTemplate (string kindRoot, string kind)
		{
			return Session.SelectElement (c => templatesQuery (c).Contains (kindRoot).NextSiblings ().Text (kind))
				&& IsTemplateSelected (kindRoot, kind);
		}

		public bool IsTemplateTypeSelected (string categoryRoot, string category)
		{
			return Session.WaitForElement (c => templateCategoriesQuery (c).Contains (categoryRoot).NextSiblings ().Text (category).Selected ()).Any ();
		}

		public bool IsTemplateSelected (string kindRoot, string kind)
		{
			return Session.WaitForElement (c => templatesQuery (c).Contains (kindRoot).NextSiblings ().Text (kind).Selected ()).Any ();
		}

		public bool Next ()
		{
			return Session.ClickElement (c => c.Button ().Text ("Next"));
		}

		public bool Create ()
		{
			return Session.ClickElement (c => c.Button ().Text ("Create"));
		}

		public bool Previous ()
		{
			return Session.ClickElement (c => c.Button ().Marked ("previousButton"));
		}

		public bool Close ()
		{
			return Session.ClickElement (c => c.Button ().Marked ("cancelButton"));
		}

		public bool SetProjectName (string projectName, bool addToExistingSolution)
		{
			Func<AppQuery, AppQuery> projectNameTextBox = c => c.Textfield ().Marked ("projectNameTextBox");
			if (addToExistingSolution && Session.Query (c => projectNameTextBox (c).Sensitivity (false)).Length > 0) {
				return Session.Query (c => projectNameTextBox (c).Text (projectName)).Length > 0;
			}
			return Session.EnterText (projectNameTextBox, projectName);
		}

		public bool SetSolutionName (string solutionName, bool addToExistingSolution)
		{
			Func<AppQuery, AppQuery> solutionNameTextBox = c => c.Textfield ().Marked ("solutionNameTextBox");
			if (addToExistingSolution) {
				return Session.Query (c => solutionNameTextBox (c).Sensitivity (false)).Length > 0 &&
					Session.Query (c => solutionNameTextBox (c).Text (solutionName)).Length > 0;
			}
			return Session.EnterText (solutionNameTextBox, solutionName);
		}

		public bool SetSolutionLocation (string solutionLocation)
		{
			return Session.EnterText (c => c.Textfield ().Marked ("locationTextBox"), solutionLocation);
		}

		public bool CreateProjectInSolutionDirectory (bool projectInSolution)
		{
			return SetCheckBox ("createProjectWithinSolutionDirectoryCheckBox", projectInSolution);
		}

		public bool UseGit (GitOptions options)
		{
			var gitSelectionSuccess = SetCheckBox ("useGitCheckBox", options.UseGit);
			if (gitSelectionSuccess && options.UseGit)
				return gitSelectionSuccess && SetCheckBox ("createGitIgnoreFileCheckBox", options.UseGitIgnore);
			return gitSelectionSuccess;
		}

		public bool SetCheckBox (string checkBoxName, bool active)
		{
			return Session.ToggleElement (c => c.CheckButton ().Marked (checkBoxName), active);
		}
			
		public bool GetSensitivity (string widgetName)
		{
			AppResult[] results = Session.Query (c => c.Marked (widgetName).Sensitivity (true));
			return results.Length > 0;
		}

		public void ValidatePreviewTree (ProjectDetails projectDetails, GitOptions gitOptions)
		{
			var rootFolder = projectDetails.ProjectInSolution ? projectDetails.SolutionName : projectDetails.ProjectName;

			Func<AppQuery, AppQuery> solutionLocation = (c) => previewTree (c).Contains (projectDetails.SolutionLocation);
			Func<AppQuery, AppQuery> solutionLocationChildren = (c) => solutionLocation (c).Children ();

			Func<AppQuery, AppQuery> rootFolderChildren = (c) => solutionLocationChildren (c).Contains (rootFolder).Children ();
			Func<AppQuery, AppQuery> checkForGit = c => rootFolderChildren (c).Index (0).Contains ("<span color='#AAAAAA'>.git</span>");
			Func<AppQuery, AppQuery> checkForGitIgnore = c => rootFolderChildren (c).Index (1).Contains ("<span color='#AAAAAA'>.gitignore</span>");

			Assert.IsNotEmpty (Session.Query (c => solutionLocation (c)));
			Assert.IsNotEmpty (Session.Query (c => solutionLocation (c).Children ().Contains (rootFolder)));

			if (gitOptions.UseGit) {
				Assert.IsNotEmpty (Session.Query (checkForGit));
				if (gitOptions.UseGitIgnore)
					Assert.IsNotEmpty (Session.Query (checkForGitIgnore));
				else
					Assert.IsEmpty (Session.Query (checkForGitIgnore));
			} else {
				Assert.IsEmpty (Session.Query (checkForGit));
				Assert.IsEmpty (Session.Query (checkForGitIgnore));
			}

			Assert.IsNotEmpty (Session.Query (c => rootFolderChildren (c).Contains (projectDetails.SolutionName + ".sln")));

			if (projectDetails.ProjectInSolution) {
				Assert.IsNotEmpty (Session.Query (c => rootFolderChildren (c).Contains (projectDetails.ProjectName)));
				Assert.IsNotEmpty (Session.Query (c => rootFolderChildren (c).Contains (projectDetails.ProjectName).Children ().Contains (projectDetails.ProjectName + ".csproj")));
			} else {
				Assert.IsNotEmpty (Session.Query (c => rootFolderChildren (c).Contains (projectDetails.ProjectName + ".csproj")));
			}
		}
	}
}

