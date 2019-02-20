//
// DotNetCoreRestoreTests.cs
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;
using System;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class DotNetCoreRestoreTests : RestoreTestBase
	{
		/// <summary>
		/// PackageReference defined in project should override implicit package references from SDK.
		/// </summary>
		[Test]
		public async Task GetDependencies_FSharpCore45InProjectFSharpCore43Implicit_FSharpCore45Used ()
		{
			FilePath solutionFileName = Util.GetSampleProject ("DotNetCoreFSharpCore45", "DotNetCoreFSharpCore45.sln");

			CreateNuGetConfigFile (solutionFileName.ParentDirectory);

			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllDotNetProjects ().Single ();

			await RestoreDotNetCoreNuGetPackages (solution);

			var dependencies = (await project.GetPackageDependencies (ConfigurationSelector.Default, default (CancellationToken))).ToList ();
			var fsharpCore = dependencies.SingleOrDefault (d => d.Name == "FSharp.Core");
			var fsharpCoreVersion = Version.Parse (fsharpCore.Version);

			Assert.True (fsharpCoreVersion.Major == 4 && fsharpCoreVersion.Minor == 5 && fsharpCoreVersion.Build >= 0, $"Version {fsharpCoreVersion.Major}.{fsharpCoreVersion.Minor}.{fsharpCoreVersion.Build} != 4.5.x");
		}

		[Test]
		public async Task OfflineRestore_NetCore21Project ()
		{
			FilePath solutionFileName = Util.GetSampleProject ("restore-netcore-offline", "dotnetcoreconsole.sln");
			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllDotNetProjects ().Single ();

			using (var logger = new PackageManagementEventsConsoleLogger ()) {
				await RestoreDotNetCoreNuGetPackages (solution);
			}

			var packagesDirectory = solution.BaseDirectory.Combine ("packages-cache");
			Assert.IsFalse (Directory.Exists (packagesDirectory));
			Assert.IsTrue (File.Exists (project.BaseDirectory.Combine ("obj", "project.assets.json")));
		}
	}
}
