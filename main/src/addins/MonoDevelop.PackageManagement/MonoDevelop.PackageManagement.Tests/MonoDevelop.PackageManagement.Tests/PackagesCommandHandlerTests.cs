//
// PackagesCommandHandlerTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackagesCommandHandlerTests : RestoreTestBase
	{
		TestableRestorePackagesHandler restorePackagesHandler;
		TestableRestorePackagesInProjectHandler restorePackagesInProjectHandler;

		[SetUp]
		public void Init ()
		{
			restorePackagesHandler = new TestableRestorePackagesHandler ();
			restorePackagesInProjectHandler = new TestableRestorePackagesInProjectHandler ();
		}

		[Test]
		public async Task ProjectWithNoPackages ()
		{
			FilePath solutionFileName = Util.GetSampleProject ("csharp-console", "csharp-console.sln");

			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllDotNetProjects ().Single ();

			// Project selected.
			restorePackagesHandler.RunUpdate (solution, project);
			Assert.IsFalse (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project);
			Assert.IsFalse (restorePackagesInProjectHandler.Enabled);

			// Solution only selected
			restorePackagesHandler.RunUpdate (solution, project: null);
			Assert.IsFalse (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project: null);
			Assert.IsFalse (restorePackagesInProjectHandler.Enabled);
		}

		[Test]
		public async Task ProjectWithPackagesConfig ()
		{
			FilePath solutionFileName = Util.GetSampleProject ("csharp-console", "csharp-console.sln");

			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllDotNetProjects ().Single ();

			var packagesConfigFileName = project.BaseDirectory.Combine ("packages.config");
			File.WriteAllText (packagesConfigFileName, "<packages />");

			// Project selected.
			restorePackagesHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesInProjectHandler.Enabled);

			// Solution only selected
			restorePackagesHandler.RunUpdate (solution, project: null);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project: null);
			Assert.IsFalse (restorePackagesInProjectHandler.Enabled, "Should be false - no project selected"); 
		}

		[Test]
		public async Task SdkProject_PackageReference ()
		{
			FilePath solutionFileName = Util.GetSampleProject ("NetStandardXamarinForms", "NetStandardXamarinForms.sln");

			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllDotNetProjects ().Single ();

			// Project selected.
			restorePackagesHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesInProjectHandler.Enabled);

			// Solution only selected
			restorePackagesHandler.RunUpdate (solution, project: null);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project: null);
			Assert.IsFalse (restorePackagesInProjectHandler.Enabled, "Should be false - no project selected");
		}

		[Test]
		public async Task SdkProject_NoPackageReferences ()
		{
			FilePath solutionFileName = Util.GetSampleProject ("netstandard-sdk", "netstandard-sdk.sln");

			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllDotNetProjects ().Single ();

			// Project selected.
			restorePackagesHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesInProjectHandler.Enabled);

			// Solution only selected
			restorePackagesHandler.RunUpdate (solution, project: null);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project: null);
			Assert.IsFalse (restorePackagesInProjectHandler.Enabled, "Should be false - no project selected");
		}

		[Test]
		public async Task SdkProject_NetFramework472 ()
		{
			FilePath solutionFileName = Util.GetSampleProject ("netframework-sdk", "netframework-sdk.sln");

			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllDotNetProjects ().Single ();

			// Project selected.
			restorePackagesHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesInProjectHandler.Enabled);

			// Solution only selected
			restorePackagesHandler.RunUpdate (solution, project: null);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project: null);
			Assert.IsFalse (restorePackagesInProjectHandler.Enabled, "Should be false - no project selected");
		}

		[Test]
		public async Task PackageReferenceProject_NonSdk ()
		{
			FilePath solutionFileName = Util.GetSampleProject ("package-reference", "package-reference.sln");

			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllDotNetProjects ().Single ();

			// Project selected.
			restorePackagesHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesInProjectHandler.Enabled);

			// Solution only selected
			restorePackagesHandler.RunUpdate (solution, project: null);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project: null);
			Assert.IsFalse (restorePackagesInProjectHandler.Enabled, "Should be false - no project selected");
		}

		[Test]
		public async Task RestoreProjectStyle_NoPackageReferences ()
		{
			FilePath solutionFileName = Util.GetSampleProject ("RestoreStylePackageReference", "RestoreStylePackageReference.sln");

			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllDotNetProjects ().Single ();

			// Project selected.
			restorePackagesHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesInProjectHandler.Enabled);

			// Solution only selected
			restorePackagesHandler.RunUpdate (solution, project: null);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project: null);
			Assert.IsFalse (restorePackagesInProjectHandler.Enabled, "Should be false - no project selected");
		}

		[Test]
		public void NuGetAwareProject ()
		{
			var project = new FakeNuGetAwareProject ();
			var solution = new Solution ();
			solution.RootFolder.AddItem (project);

			// No packages in project.
			project.HasPackagesReturnValue = false;

			// Project selected.
			restorePackagesHandler.RunUpdate (solution, project);
			Assert.IsFalse (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project);
			Assert.IsFalse (restorePackagesInProjectHandler.Enabled);

			// Solution only selected
			restorePackagesHandler.RunUpdate (solution, project: null);
			Assert.IsFalse (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project: null);
			Assert.IsFalse (restorePackagesInProjectHandler.Enabled, "Should be false - no project selected");

			// Project has packages.
			project.HasPackagesReturnValue = true;

			// Project selected.
			restorePackagesHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project);
			Assert.IsTrue (restorePackagesInProjectHandler.Enabled);

			// Solution only selected
			restorePackagesHandler.RunUpdate (solution, project: null);
			Assert.IsTrue (restorePackagesHandler.Enabled);

			restorePackagesInProjectHandler.RunUpdate (solution, project: null);
			Assert.IsFalse (restorePackagesInProjectHandler.Enabled, "Should be false - no project selected");
		}
	}
}
