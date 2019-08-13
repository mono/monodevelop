//
// FullyQualifiedReferencePathTests.cs
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

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class FullyQualifiedReferencePathTests : RestoreTestBase
	{
		[Test]
		public async Task UpdatePackage_ReferenceHasFullyQualifiedPath_ReferenceHasFullyQualifiedPathAfterUpdate ()
		{
			FilePath solutionFileName = Util.GetSampleProject ("ReferenceFullPath", "ReferenceFullPath.sln");
			FilePath projectFileName = solutionFileName.ParentDirectory.Combine ("ReferenceFullPath.csproj");
			FilePath packagesDirectory = solutionFileName.ParentDirectory.Combine ("packages");
			string projectXml = File.ReadAllText (projectFileName);
			string solutionDirectory = MSBuildProjectService.ToMSBuildPath (null, solutionFileName.ParentDirectory);
			string updatedProjectXml = projectXml.Replace ("$(SolutionDir)", solutionDirectory);
			File.WriteAllText (projectFileName, updatedProjectXml);

			using (solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName)) {
				CreateNuGetConfigFile (solution.BaseDirectory);
				var project = (DotNetProject)solution.FindProjectByName ("ReferenceFullPath");

				var restoreResult = await RestorePackagesConfigNuGetPackages (solution);
				Assert.IsTrue (restoreResult.Restored);
				Assert.AreEqual (1, restoreResult.RestoredPackages.Count ());

				// Update NuGet package
				await UpdateNuGetPackage (project, "Test.Xam.NetStandard");

				string expectedXml = Util.ToSystemEndings (File.ReadAllText (project.FileName.ChangeExtension (".csproj-saved")));
				expectedXml = expectedXml.Replace ("$(SolutionDir)", solutionDirectory);

				string actualXml = File.ReadAllText (project.FileName);
				Assert.AreEqual (expectedXml, actualXml);
			}
		}

		Task UpdateNuGetPackage (DotNetProject project, string packageId)
		{
			var solutionManager = new MonoDevelopSolutionManager (project.ParentSolution);
			var context = CreateNuGetProjectContext (solutionManager.Settings);

			var packageManager = new MonoDevelopNuGetPackageManager (solutionManager);

			var action = new UpdateNuGetPackageAction (
				solutionManager,
				new DotNetProjectProxy (project),
				context,
				packageManager,
				PackageManagementServices.PackageManagementEvents) {
				PackageId = packageId
			};

			return Task.Run (() => {
				action.Execute ();
			});
		}
	}
}
