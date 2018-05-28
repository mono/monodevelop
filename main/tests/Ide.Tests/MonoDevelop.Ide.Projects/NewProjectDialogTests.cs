//
// NewProjectDialogTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using UnitTests;
using NUnit.Framework;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Projects
{
	class NewProjectDialogTests : IdeTestBase
	{
		TestableNewProjectDialogController controller;
		bool createProjectDirectoryOriginalValue;
		bool useGitOriginalValue;
		bool useGitIgnoreOriginalValue;

		protected override void InternalSetup (string rootDir)
		{
			base.InternalSetup (rootDir);

			createProjectDirectoryOriginalValue = PropertyService.Get (
				NewProjectDialogController.CreateProjectSubDirectoryPropertyName,
				true);

			useGitOriginalValue = PropertyService.Get (NewProjectDialogController.UseGitPropertyName, false);
			useGitIgnoreOriginalValue = PropertyService.Get (NewProjectDialogController.CreateGitIgnoreFilePropertyName, true);
		}

		public override void TearDown ()
		{
			// Reset original property values.
			PropertyService.Set (
				NewProjectDialogController.CreateProjectSubDirectoryPropertyName,
				createProjectDirectoryOriginalValue);

			PropertyService.Set (NewProjectDialogController.UseGitPropertyName, useGitOriginalValue);
			PropertyService.Set (NewProjectDialogController.CreateGitIgnoreFilePropertyName, useGitIgnoreOriginalValue);
		}

		void CreateDialog ()
		{
			controller = new TestableNewProjectDialogController ();

			// Set base path to avoid use of IdeApp.Preferences which is not
			// initialized during tests.
			controller.BasePath = Util.TestsRootDir;
		}

		void CSharpLibraryTemplateSelectedByDefault ()
		{
			controller.SelectedTemplateId = "MonoDevelop.CSharp.Library";
		}

		void UseExistingSolution ()
		{
			var parentFolder = new SolutionFolder ();
			controller.ParentFolder = parentFolder;
		}

		[Test]
		public void Show_CSharpLibrary_NoItemsCreated ()
		{
			CreateDialog ();
			CSharpLibraryTemplateSelectedByDefault ();

			controller.Backend.OnShowDialogCalled = () => {
				controller.MoveToNextPage ();
			};

			bool result = controller.Show ();

			Assert.IsFalse (result);
		}

		[Test]
		public void CreateProjectDirectorySetting_IsTrue_FinalPageHasCreateDirectoryEnabledAndChecked ()
		{
			CreateDialog ();
			CSharpLibraryTemplateSelectedByDefault ();
			PropertyService.Set (NewProjectDialogController.CreateProjectSubDirectoryPropertyName, true);

			controller.Backend.OnShowDialogCalled = () => {
				controller.MoveToNextPage ();
			};

			controller.Show ();

			Assert.IsTrue (controller.FinalConfiguration.CreateProjectDirectoryInsideSolutionDirectory);
			Assert.IsTrue (controller.FinalConfiguration.IsCreateProjectDirectoryInsideSolutionDirectoryEnabled);
		}

		[Test]
		public void CreateProjectDirectorySetting_IsFalse_FinalPageHasCreateDirectoryEnabledAndNotChecked ()
		{
			CreateDialog ();
			CSharpLibraryTemplateSelectedByDefault ();
			PropertyService.Set (NewProjectDialogController.CreateProjectSubDirectoryPropertyName, false);

			controller.Backend.OnShowDialogCalled = () => {
				controller.MoveToNextPage ();
			};

			controller.Show ();

			Assert.IsFalse (controller.FinalConfiguration.CreateProjectDirectoryInsideSolutionDirectory);
			Assert.IsTrue (controller.FinalConfiguration.IsCreateProjectDirectoryInsideSolutionDirectoryEnabled);
		}

		[Test]
		public void CreateProjectDirectorySetting_IsTrueAndAddingProjectToExistingSolution_FinalPageHasCreateDirectoryDisabledAndChecked ()
		{
			CreateDialog ();
			CSharpLibraryTemplateSelectedByDefault ();
			UseExistingSolution ();
			PropertyService.Set (NewProjectDialogController.CreateProjectSubDirectoryPropertyName, true);

			controller.Backend.OnShowDialogCalled = () => {
				controller.MoveToNextPage ();
			};

			controller.Show ();

			Assert.IsTrue (controller.FinalConfiguration.CreateProjectDirectoryInsideSolutionDirectory);
			Assert.IsFalse (controller.FinalConfiguration.IsCreateProjectDirectoryInsideSolutionDirectoryEnabled);
		}

		/// <summary>
		/// Ensure a project directory is always created when adding a project to an existing solution even
		/// if previously the create project directory setting was disabled.
		/// </summary>
		[Test]
		public void CreateProjectDirectorySetting_IsFalseAndAddingProjectToExistingSolution_FinalPageHasCreateDirectoryDisabledAndChecked ()
		{
			CreateDialog ();
			CSharpLibraryTemplateSelectedByDefault ();
			UseExistingSolution ();
			PropertyService.Set (NewProjectDialogController.CreateProjectSubDirectoryPropertyName, false);

			controller.Backend.OnShowDialogCalled = () => {
				controller.MoveToNextPage ();
			};

			controller.Show ();

			Assert.IsTrue (controller.FinalConfiguration.CreateProjectDirectoryInsideSolutionDirectory);
			Assert.IsFalse (controller.FinalConfiguration.IsCreateProjectDirectoryInsideSolutionDirectoryEnabled);
		}

		[Test]
		public void FinalPage_ProjectNameTests ()
		{
			CreateDialog ();
			CSharpLibraryTemplateSelectedByDefault ();
			PropertyService.Set (NewProjectDialogController.CreateProjectSubDirectoryPropertyName, true);

			controller.Backend.OnShowDialogCalled = () => {
				controller.MoveToNextPage ();
			};

			controller.Show ();
			controller.FinalConfiguration.UpdateFromParameters ();
			controller.FinalConfiguration.ProjectName = "Test";

			Assert.IsTrue (controller.FinalConfiguration.IsProjectNameEnabled);
			Assert.AreEqual ("Test", controller.FinalConfiguration.ProjectName);

			controller.FinalConfiguration.Parameters ["ProjectName"] = "ChangedName";
			controller.FinalConfiguration.Parameters ["IsProjectNameReadOnly"] = bool.TrueString;

			controller.FinalConfiguration.UpdateFromParameters ();

			Assert.IsFalse (controller.FinalConfiguration.IsProjectNameEnabled);
			Assert.AreEqual ("ChangedName", controller.FinalConfiguration.ProjectName);
		}

		[TestCase (true)]
		[TestCase (false)]
		public void CreateProjectDirectorySetting_WizardOverridesProperty (bool createProjectSubDirectory)
		{
			CreateDialog ();
			CSharpLibraryTemplateSelectedByDefault ();
			PropertyService.Set (NewProjectDialogController.CreateProjectSubDirectoryPropertyName, createProjectSubDirectory);

			controller.Backend.OnShowDialogCalled = () => {
				controller.MoveToNextPage ();
			};

			controller.Show ();

			controller.FinalConfiguration.Parameters ["CreateProjectDirectoryInsideSolutionDirectory"] = bool.TrueString;
			controller.FinalConfiguration.Parameters ["IsCreateProjectDirectoryInsideSolutionDirectoryEnabled"] = bool.TrueString;
			controller.FinalConfiguration.UpdateFromParameters ();

			Assert.IsTrue (controller.FinalConfiguration.CreateProjectDirectoryInsideSolutionDirectory);
			Assert.IsTrue (controller.FinalConfiguration.IsCreateProjectDirectoryInsideSolutionDirectoryEnabled);

			controller.FinalConfiguration.Parameters ["CreateProjectDirectoryInsideSolutionDirectory"] = bool.FalseString;
			controller.FinalConfiguration.Parameters ["IsCreateProjectDirectoryInsideSolutionDirectoryEnabled"] = bool.FalseString;
			controller.FinalConfiguration.UpdateFromParameters ();

			Assert.IsFalse (controller.FinalConfiguration.CreateProjectDirectoryInsideSolutionDirectory);
			Assert.IsFalse (controller.FinalConfiguration.IsCreateProjectDirectoryInsideSolutionDirectoryEnabled);
		}

		[TestCase (true, true)]
		[TestCase (true, false)]
		[TestCase (false, true)]
		[TestCase (false, false)]
		public void Git_NewSolution_FinalConfigurationPage (bool useGit, bool createGitIgnore)
		{
			CreateDialog ();
			CSharpLibraryTemplateSelectedByDefault ();
			PropertyService.Set (NewProjectDialogController.UseGitPropertyName, useGit);
			PropertyService.Set (NewProjectDialogController.CreateGitIgnoreFilePropertyName, createGitIgnore);

			controller.Backend.OnShowDialogCalled = () => {
				controller.MoveToNextPage ();
			};

			controller.Show ();

			Assert.AreEqual (useGit, controller.FinalConfiguration.UseGit);
			Assert.AreEqual (createGitIgnore, controller.FinalConfiguration.CreateGitIgnoreFile);
			Assert.AreEqual (useGit, controller.FinalConfiguration.IsGitIgnoreEnabled);
			Assert.IsTrue (controller.FinalConfiguration.IsUseGitEnabled);
		}

		[TestCase (true, true)]
		[TestCase (true, false)]
		[TestCase (false, true)]
		[TestCase (false, false)]
		public void Git_ExistingSolution_FinalConfigurationPage (bool useGit, bool createGitIgnore)
		{
			CreateDialog ();
			CSharpLibraryTemplateSelectedByDefault ();
			UseExistingSolution ();
			PropertyService.Set (NewProjectDialogController.UseGitPropertyName, useGit);
			PropertyService.Set (NewProjectDialogController.CreateGitIgnoreFilePropertyName, createGitIgnore);

			controller.Backend.OnShowDialogCalled = () => {
				controller.MoveToNextPage ();
			};

			controller.Show ();

			Assert.AreEqual (useGit, controller.FinalConfiguration.UseGit);
			Assert.AreEqual (createGitIgnore, controller.FinalConfiguration.CreateGitIgnoreFile);
			Assert.IsFalse (controller.FinalConfiguration.IsGitIgnoreEnabled);
			Assert.IsFalse (controller.FinalConfiguration.IsUseGitEnabled);
		}

		[TestCase (true, true)]
		[TestCase (true, false)]
		[TestCase (false, true)]
		[TestCase (false, false)]
		public void Git_NewSolution_OverriddenGitIgnoreSettings_FinalConfigurationPage (bool useGit, bool createGitIgnore)
		{
			CreateDialog ();
			CSharpLibraryTemplateSelectedByDefault ();
			PropertyService.Set (NewProjectDialogController.UseGitPropertyName, useGit);
			PropertyService.Set (NewProjectDialogController.CreateGitIgnoreFilePropertyName, createGitIgnore);

			controller.Backend.OnShowDialogCalled = () => {
				controller.MoveToNextPage ();
			};

			controller.Show ();

			controller.FinalConfiguration.Parameters ["CreateGitIgnoreFile"] = bool.TrueString;
			controller.FinalConfiguration.Parameters ["IsGitIgnoreEnabled"] = bool.FalseString;
			controller.FinalConfiguration.UpdateFromParameters ();

			Assert.IsTrue (controller.FinalConfiguration.CreateGitIgnoreFile);
			Assert.IsFalse (controller.FinalConfiguration.IsGitIgnoreEnabled);
			Assert.IsTrue (controller.FinalConfiguration.IsUseGitEnabled);

			controller.FinalConfiguration.Parameters ["CreateGitIgnoreFile"] = bool.FalseString;
			controller.FinalConfiguration.Parameters ["IsGitIgnoreEnabled"] = bool.FalseString;
			controller.FinalConfiguration.UpdateFromParameters ();

			Assert.IsFalse (controller.FinalConfiguration.CreateGitIgnoreFile);
			Assert.IsFalse (controller.FinalConfiguration.IsGitIgnoreEnabled);
			Assert.IsTrue (controller.FinalConfiguration.IsUseGitEnabled);
		}
	}
}
