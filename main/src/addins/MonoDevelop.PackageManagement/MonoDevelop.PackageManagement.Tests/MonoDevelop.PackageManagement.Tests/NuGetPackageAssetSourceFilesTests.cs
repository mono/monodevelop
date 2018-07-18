//
// NuGetPackageAssetSourceFilesTests.cs
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

using System.Diagnostics;
using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;
using System.Linq;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class NuGetPackageAssetSourceFilesTests : RestoreTestBase
	{
		/// <summary>
		/// The LibLog NuGet package has .cs.pp content files that are pre-processed and the
		/// generated .cs files are implicitly included in the project. This test ensures that
		/// these generated files are available to the type system when Project.GetSourceFilesAsync
		/// is called. 
		/// </summary>
		[Test]
		public async Task NetCoreAndPackageReferenceProjectWithLibLogPackage ()
		{
			string solutionFileName = Util.GetSampleProject ("NuGetPackageAssetFiles", "NuGetPackageAssetFiles.sln");
			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			CreateNuGetConfigFile (solution.BaseDirectory);
			var netStandardProject = (DotNetProject)solution.FindProjectByName ("NetStandardProject");
			var packageReferenceProject = (DotNetProject)solution.FindProjectByName ("NetFrameworkProject");

			var process = Process.Start ("msbuild", $"/t:Restore /p:RestoreDisableParallel=true \"{solutionFileName}\"");
			Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
			Assert.AreEqual (0, process.ExitCode);

			var netStandardProjectSourceFiles = await netStandardProject.GetSourceFilesAsync (ConfigurationSelector.Default);
			var packageReferenceProjectSourceFiles = await packageReferenceProject.GetSourceFilesAsync (ConfigurationSelector.Default);

			var libLogExceptionFileFromNetCoreProject = netStandardProjectSourceFiles.FirstOrDefault (f => f.FilePath.FileName == "LibLogException.cs");
			var libLogExceptionFileFromPackageReferenceProject = packageReferenceProjectSourceFiles.FirstOrDefault (f => f.FilePath.FileName == "LibLogException.cs");

			Assert.IsNotNull (libLogExceptionFileFromNetCoreProject);
			Assert.IsNotNull (libLogExceptionFileFromPackageReferenceProject);
		}
	}
}
