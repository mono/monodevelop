//
// InstallPackageWithAvailableItemNameTests.cs
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

using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NuGet.Versioning;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class InstallPackageWithAvailableItemNameTests : RestoreTestBase
	{
		[Test]
		public async Task InstallPackage_PackageDefinesCustomAvailableItemNames_BuildActionsIncludeCustomAvailableItemNames ()
		{
			string solutionFileName = Util.GetSampleProject ("RestoreStylePackageReference", "RestoreStylePackageReference.sln");
			using (solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName)) {
				CreateNuGetConfigFile (solution.BaseDirectory);
				var project = (DotNetProject)solution.FindProjectByName ("RestoreStylePackageReference");

				var originalBuildActions = project.GetBuildActions ();
				await InstallNuGetPackage (project, "Test.Xam.AvailableItemName", "0.1.0");
				var updatedBuildActions = project.GetBuildActions ();

				Assert.IsFalse (originalBuildActions.Contains ("TestXamAvailableItem"));
				Assert.That (updatedBuildActions, Contains.Item ("TestXamAvailableItem"));
			}
		}

		Task InstallNuGetPackage (DotNetProject project, string packageId, string packageVersion)
		{
			var solutionManager = new MonoDevelopSolutionManager (project.ParentSolution);
			var context = CreateNuGetProjectContext (solutionManager.Settings);
			var sources = solutionManager.CreateSourceRepositoryProvider ().GetRepositories ();

			var action = new InstallNuGetPackageAction (sources, solutionManager, new DotNetProjectProxy (project), context);
			action.LicensesMustBeAccepted = false;
			action.OpenReadmeFile = false;
			action.PackageId = packageId;
			action.Version = NuGetVersion.Parse (packageVersion);

			return Task.Run (() => {
				action.Execute ();
			});
		}
	}
}
