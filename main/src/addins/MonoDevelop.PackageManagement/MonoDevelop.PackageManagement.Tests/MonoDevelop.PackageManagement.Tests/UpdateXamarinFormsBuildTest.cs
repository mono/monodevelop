//
// UpdateXamarinFormsBuildTest.cs
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NuGet.Versioning;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UpdateXamarinFormsBuildTest : RestoreTestBase
	{
		static readonly string UseNSUrlSessionHandlerProperty = "MonoDevelop.MacIntegration.UseNSUrlSessionHandler";
		static bool originalSessionHandlerPropertyValue;

		[TestFixtureSetUp]
		public void TestFixtureSetup ()
		{
			// Disable use of Xamarin.Mac's NSUrlSessionHandler for tests. Xamarin.Mac is not initialized.
			originalSessionHandlerPropertyValue = PropertyService.Get (UseNSUrlSessionHandlerProperty, true);
			PropertyService.Set (UseNSUrlSessionHandlerProperty, false);
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown ()
		{
			PropertyService.Set (UseNSUrlSessionHandlerProperty, originalSessionHandlerPropertyValue);
		}

		/// <summary>
		/// Tests that a build error due to a mismatched Xamarin.Forms NuGet package used
		/// in two projects can be fixed by updating the NuGet package to match without
		/// having to close and re-open the solution
		/// </summary>
		[Test]
		public async Task UpdateXamarinFormsNuGetPackage_ThenBuild ()
		{
			string solutionFileName = Util.GetSampleProject ("FormsUpdateTest", "FormsUpdateTest.sln");
			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			CreateNuGetConfigFile (solution.BaseDirectory);
			var pclProject = (DotNetProject)solution.FindProjectByName ("PclProject");

			var restoreResult = await RestoreNuGetPackages (solution);
			Assert.IsTrue (restoreResult.Restored);
			Assert.AreEqual (2, restoreResult.RestoredPackages.Count ());

			// Build should fail due to mismatched Xamarin.Forms versions being used.
			var result = await solution.Build (Util.GetMonitor (), ConfigurationSelector.Default);
			Assert.IsTrue (result.HasErrors);
			var buildError = result.Errors [0];
			Assert.AreEqual ("XF002", buildError.ErrorNumber);

			// Update NuGet package in PCL project.
			await UpdateNuGetPackage (pclProject, "Xamarin.Forms", "3.0.0.482510");

			// Build should not fail.
			result = await solution.Build (Util.GetMonitor (), ConfigurationSelector.Default);
			buildError = result.Errors.FirstOrDefault ();
			Assert.IsFalse (result.HasErrors, "Build failed: {0}", buildError);
		}

		/// <summary>
		/// Tests that a build error due to a mismatched Xamarin.Forms NuGet package used
		/// in two projects can be fixed by updating the NuGet package to match without
		/// having to close and re-open the solution
		/// </summary>
		[Test]
		public async Task UpdateXamarinFormsNuGetPackageReference_ThenBuild ()
		{
			string solutionFileName = Util.GetSampleProject ("FormsUpdatePackageRef", "FormsUpdatePackageRef.sln");
			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			CreateNuGetConfigFile (solution.BaseDirectory);
			var netStandardProject = (DotNetProject)solution.FindProjectByName ("NetStandardProject");

			var process = Process.Start ("msbuild", $"/t:Restore /p:RestoreDisableParallel=true \"{solutionFileName}\"");
			Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
			Assert.AreEqual (0, process.ExitCode);

			// Build should fail due to mismatched Xamarin.Forms versions being used.
			var result = await solution.Build (Util.GetMonitor (), ConfigurationSelector.Default);
			Assert.IsTrue (result.HasErrors);
			var buildError = result.Errors [0];
			Assert.AreEqual ("XF002", buildError.ErrorNumber);

			// Update NuGet package in PCL project.
			await UpdateNuGetPackage (netStandardProject, "Xamarin.Forms", "3.0.0.482510");

			// Build should not fail.
			result = await solution.Build (Util.GetMonitor (), ConfigurationSelector.Default);
			buildError = result.Errors.FirstOrDefault ();
			Assert.IsFalse (result.HasErrors, "Build failed: {0}", buildError);
		}

		Task UpdateNuGetPackage (DotNetProject project, string packageId, string packageVersion)
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
