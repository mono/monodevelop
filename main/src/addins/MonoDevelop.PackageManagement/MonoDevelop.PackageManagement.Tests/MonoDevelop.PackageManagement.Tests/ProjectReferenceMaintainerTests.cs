//
// ProjectReferenceMaintainerTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ProjectReferenceMaintainerTests
	{
		ProjectReferenceMaintainer maintainer;
		FakeDotNetProject project;
		FakeNuGetProject nugetProject;

		void CreateProject ()
		{
			project = new FakeDotNetProject (@"d:\projects\MyProject\MyProject.csproj".ToNativePath ());
			nugetProject = new FakeNuGetProject (project);
		}

		void CreateProjectReferenceMaintainer (FakeNuGetProject fakeNuGetProject)
		{
			maintainer = new ProjectReferenceMaintainer (fakeNuGetProject);
		}

		void CreateProjectReferenceMaintainer ()
		{
			CreateProject ();
			maintainer = new ProjectReferenceMaintainer (nugetProject);
		}

		async Task<ProjectReference> AddAssemblyReferenceToMaintainer (string hintPath)
		{
			var reference = ProjectReference.CreateAssemblyFileReference (hintPath);
			await maintainer.AddReference (reference);
			return reference;
		}

		ProjectReference AddReferenceToProject (string hintPath)
		{
			var reference = ProjectReference.CreateAssemblyFileReference (hintPath);
			project.References.Add (reference);
			return reference;
		}

		[Test]
		public async Task OneReferenceAdded_ReferenceAddedToProject ()
		{
			CreateProjectReferenceMaintainer ();
			string hintPath = @"d:\projects\MyProject\packages\Test.dll".ToNativePath ();
			await AddAssemblyReferenceToMaintainer (hintPath);

			await maintainer.ApplyChanges ();

			var reference = project.References.Single ();
			Assert.AreEqual ("Test", reference.Reference);
			Assert.AreEqual (hintPath, reference.HintPath.ToString ());
		}

		[Test]
		public async Task OneReferenceRemoved_ReferenceRemovedFromProject ()
		{
			CreateProjectReferenceMaintainer ();
			string hintPath = @"d:\projects\MyProject\packages\Test.dll".ToNativePath ();
			var reference = AddReferenceToProject (hintPath);
			await maintainer.RemoveReference (reference);

			await maintainer.ApplyChanges ();

			Assert.AreEqual (0, project.References.Count);
		}

		[Test]
		public async Task ReferenceRemovedReferenceAdded_ReferenceUpdatedInProject ()
		{
			CreateProjectReferenceMaintainer ();
			string hintPath1 = @"d:\projects\MyProject\packages\1.0\Test.dll".ToNativePath ();
			var referenceToRemove = AddReferenceToProject (hintPath1);
			await maintainer.RemoveReference (referenceToRemove);
			string hintPath2 = @"d:\projects\MyProject\packages\1.1\Test.dll".ToNativePath ();
			await AddAssemblyReferenceToMaintainer (hintPath2);

			await maintainer.ApplyChanges ();

			var reference = project.References.Single ();
			Assert.AreEqual ("Test", reference.Reference);
			Assert.AreEqual (hintPath2, reference.HintPath.ToString ());
			Assert.AreSame (referenceToRemove, reference);
		}

		[Test]
		public async Task ReferenceRemovedReferenceAdded_DifferentReferenceCase_ReferenceUpdatedInProject ()
		{
			CreateProjectReferenceMaintainer ();
			string hintPath1 = @"d:\projects\MyProject\packages\1.0\Test.dll".ToNativePath ();
			var referenceToRemove = AddReferenceToProject (hintPath1);
			await maintainer.RemoveReference (referenceToRemove);
			string hintPath2 = @"d:\projects\MyProject\packages\1.1\TEST.dll".ToNativePath ();
			await AddAssemblyReferenceToMaintainer (hintPath2);

			await maintainer.ApplyChanges ();

			var reference = project.References.Single ();
			Assert.AreEqual (hintPath2, reference.HintPath.ToString ());
			Assert.AreSame (referenceToRemove, reference);
		}

		[Test]
		public async Task OneReferenceAdded_ProjectThrowsOnSaveDuringApplyChanges_ExceptionThrown ()
		{
			CreateProjectReferenceMaintainer ();
			string hintPath = @"d:\projects\MyProject\packages\Test.dll".ToNativePath ();
			await AddAssemblyReferenceToMaintainer (hintPath);
			var expectedException = new ApplicationException ("Error");
			project.SaveAction = () => throw expectedException;

			try {
				await maintainer.ApplyChanges ();
				Assert.Fail ("Expected an exception.");
			} catch (Exception ex) {
				Assert.AreEqual (expectedException, ex);
			}
		}

		[Test]
		public async Task References_OneReferenceRemoved ()
		{
			CreateProject ();
			string hintPath = @"d:\projects\MyProject\packages\Test.dll".ToNativePath ();
			var reference = AddReferenceToProject (hintPath);
			CreateProjectReferenceMaintainer (nugetProject);

			Assert.AreEqual (maintainer.GetReferences ().Single (), reference);

			await maintainer.RemoveReference (reference);

			Assert.AreEqual (0, maintainer.GetReferences ().Count ());
		}

		[Test]
		public async Task References_OneReferenceAdded ()
		{
			CreateProjectReferenceMaintainer ();

			Assert.AreEqual (0, maintainer.GetReferences ().Count ());

			string hintPath = @"d:\projects\MyProject\packages\Test.dll".ToNativePath ();
			await AddAssemblyReferenceToMaintainer (hintPath);

			var reference = maintainer.GetReferences ().Single ();
			Assert.AreEqual (hintPath, reference.HintPath.ToString ());
		}
	}
}
