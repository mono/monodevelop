//
// ProjectJsonBuildIntegratedNuGetProjectTests.cs
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

using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NuGet.ProjectManagement;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using System.IO;
using System.Linq;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	class ProjectJsonBuildIntegratedNuGetProjectTests
	{
		ProjectJsonBuildIntegratedNuGetProject projectJsonNuGetProject;

		static DummyDotNetProject CreateDotNetProject (
			string projectName,
			string fileName)
		{
			var project = new DummyDotNetProject ();
			project.Name = projectName;
			project.FileName = fileName;
			return project;
		}

		void CreateSolution (params Project [] projects)
		{
			var solution = new Solution ();
			foreach (var project in projects) {
				solution.RootFolder.AddItem (project);
			}
		}

		void CreateProjectJsonNuGetProject (DotNetProject project)
		{
			var settings = new FakeNuGetSettings ();
			var projectJsonFileName = project.BaseDirectory.Combine ("project.json");
			projectJsonNuGetProject = new ProjectJsonBuildIntegratedNuGetProject (
				projectJsonFileName,
				project.FileName,
				project,
				settings);

			string json = "{ \"frameworks\": { \".NETPortable,Version=v4.5,Profile=Profile111\": {} }}";
			File.WriteAllText (projectJsonFileName, json);
		}

		[Test]
		public async Task GetPackageSpecsAsync_ProjectHasOneProjectReference_RestoreMetadataHasProjectReference ()
		{
			FilePath directory = Util.CreateTmpDir ("ProjectJsonNuGetTests");
			string expectedProjectFileName = directory.Combine ("ReferencedProject.csproj");
			var projectToBeReferenced = CreateDotNetProject ("ReferencedProject", expectedProjectFileName);
			string mainProjectFileName = directory.Combine ("MainTest.csproj");
			var mainProject = CreateDotNetProject ("MainTest", mainProjectFileName);
			CreateSolution (mainProject, projectToBeReferenced);
			var projectReference = ProjectReference.CreateProjectReference (projectToBeReferenced);
			mainProject.References.Add (projectReference);
			CreateProjectJsonNuGetProject (mainProject);
			var context = new DependencyGraphCacheContext ();

			var specs = await projectJsonNuGetProject.GetPackageSpecsAsync (context);

			var spec = specs [0];
			var targetFramework = spec.RestoreMetadata.TargetFrameworks.FirstOrDefault ();
			Assert.AreEqual (1, specs.Count);
			Assert.AreEqual (1, spec.RestoreMetadata.TargetFrameworks.Count);
			Assert.AreEqual (1, targetFramework.ProjectReferences.Count);
			Assert.AreEqual (expectedProjectFileName, targetFramework.ProjectReferences [0].ProjectPath);
		}
	}
}
