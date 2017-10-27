//
// ProjectSystemReferencesReaderTests.cs
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using MonoDevelop.Projects.SharedAssetsProjects;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	class ProjectSystemReferencesReaderTests
	{
		ProjectSystemReferencesReader reader;
		PackageManagementEvents packageManagementEvents;
		PackageManagementLogger logger;

		static DummyDotNetProject CreateDotNetProject (
			string projectName = "MyProject",
			string fileName = @"d:\projects\MyProject\MyProject.csproj")
		{
			var project = new DummyDotNetProject ();
			project.Name = projectName;
			project.FileName = fileName.ToNativePath ();
			return project;
		}

		void CreateProjectReferencesReader (DotNetProject project)
		{
			packageManagementEvents = new PackageManagementEvents ();
			logger = new PackageManagementLogger (packageManagementEvents);
			reader = new ProjectSystemReferencesReader (project);
		}

		void CreateSolution (params Project [] projects)
		{
			var solution = new Solution ();
			foreach (var project in projects) {
				solution.RootFolder.AddItem (project);
			}
		}

		List<ProjectRestoreReference> GetProjectReferences ()
		{
			return reader.GetProjectReferences (logger).ToList ();
		}

		[Test]
		public void GetProjectReferencesAsync_OneProjectReference_ReturnedInProjectReferences ()
		{
			string expectedProjectFileName = @"d:\projects\Test\MyTest.csproj".ToNativePath ();
			var projectToBeReferenced = CreateDotNetProject ("Test", expectedProjectFileName);
			var mainProject = CreateDotNetProject ();
			CreateSolution (mainProject, projectToBeReferenced);
			var projectReference = ProjectReference.CreateProjectReference (projectToBeReferenced);
			mainProject.References.Add (projectReference);
			CreateProjectReferencesReader (mainProject);

			var references = GetProjectReferences ();

			Assert.AreEqual (1, references.Count);
			Assert.AreEqual (expectedProjectFileName, references [0].ProjectPath);
			Assert.AreEqual (expectedProjectFileName, references [0].ProjectUniqueName);
		}

		[Test]
		public void GetProjectReferencesAsync_OneProjectReferenceWithReferenceOutputAssemblyFalse_NoProjectReferences ()
		{
			var projectToBeReferenced = CreateDotNetProject ("Test");
			var mainProject = CreateDotNetProject ();
			CreateSolution (mainProject, projectToBeReferenced);
			var projectReference = ProjectReference.CreateProjectReference (projectToBeReferenced);
			// Do not reference output assembly of project.
			projectReference.ReferenceOutputAssembly = false;
			mainProject.References.Add (projectReference);
			CreateProjectReferencesReader (mainProject);

			var references = GetProjectReferences ();

			Assert.AreEqual (0, references.Count);
		}

		[Test]
		public void GetProjectReferencesAsync_OneInvalidProjectReference_NoProjectReferencesAndWarningLogged ()
		{
			var projectToBeReferenced = CreateDotNetProject ("Test");
			var mainProject = CreateDotNetProject ("MyProject");
			CreateSolution (mainProject, projectToBeReferenced);
			var projectReference = ProjectReference.CreateProjectReference (projectToBeReferenced);
			// Mark project reference as invalid.
			projectReference.SetInvalid ("Not valid");
			mainProject.References.Add (projectReference);
			CreateProjectReferencesReader (mainProject);
			PackageOperationMessage messageLogged = null;
			packageManagementEvents.PackageOperationMessageLogged += (sender, e) => {
				messageLogged = e.Message;
			};

			var references = GetProjectReferences ();

			string expectedMessage = "Failed to resolve all project references. The package restore result for 'MyProject' or a dependant project may be incomplete.";
			Assert.AreEqual (0, references.Count);
			Assert.IsNotNull (messageLogged);
			Assert.AreEqual (MessageLevel.Warning, messageLogged.Level);
			Assert.AreEqual (expectedMessage, messageLogged.ToString ());
		}

		[Test]
		public void GetProjectReferencesAsync_OneSharedAssetProjectReference_SharedProjectIsNotIncluded ()
		{
			var projectToBeReferenced = new SharedAssetsProject ("C#");
			var mainProject = CreateDotNetProject ();
			CreateSolution (mainProject, projectToBeReferenced);
			var projectReference = ProjectReference.CreateProjectReference (projectToBeReferenced);
			mainProject.References.Add (projectReference);
			CreateProjectReferencesReader (mainProject);

			var references = GetProjectReferences ();

			Assert.AreEqual (0, references.Count);
		}

		[Test]
		public void GetProjectReferencesAsync_ExceptionThrownWhenResolvingProject_ErrorLogged ()
		{
			var projectToBeReferenced = CreateDotNetProject ("Test");
			var mainProject = CreateDotNetProject ("MyProject");
			var projectReference = ProjectReference.CreateProjectReference (projectToBeReferenced);
			// Project has no parent solution - which will cause an exception.
			mainProject.References.Add (projectReference);
			CreateProjectReferencesReader (mainProject);
			var messagesLogged = new List<PackageOperationMessage> ();
			packageManagementEvents.PackageOperationMessageLogged += (sender, e) => {
				messagesLogged.Add (e.Message);
			};

			var references = GetProjectReferences ();

			string expectedMessage = "Failed to resolve all project references. The package restore result for 'MyProject' or a dependant project may be incomplete.";
			Assert.AreEqual (0, references.Count);
			Assert.AreEqual (2, messagesLogged.Count);
			Assert.AreEqual (MessageLevel.Debug, messagesLogged [0].Level);
			Assert.That (messagesLogged [0].ToString (), Contains.Substring ("ArgumentNullException"));
			Assert.AreEqual (MessageLevel.Warning, messagesLogged [1].Level);
			Assert.AreEqual (expectedMessage, messagesLogged [1].ToString ());
		}
	}
}
