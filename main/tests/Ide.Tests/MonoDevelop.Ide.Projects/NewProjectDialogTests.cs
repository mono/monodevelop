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
	class NewProjectDialogTests : TestBase
	{
		TestableNewProjectDialogController controller;
		bool createProjectDirectoryOriginalValue;

		[TestFixtureSetUp]
		public void SetUp ()
		{
			Simulate ();

			createProjectDirectoryOriginalValue = PropertyService.Get (
				NewProjectDialogController.CreateProjectSubDirectoryPropertyName,
				true);
		}

		public override void TearDown ()
		{
			// Reset original property values.
			PropertyService.Set (
				NewProjectDialogController.CreateProjectSubDirectoryPropertyName,
				createProjectDirectoryOriginalValue);
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
	}
}
