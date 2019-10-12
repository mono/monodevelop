//
// FrameworkReferenceTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class FrameworkReferenceTests : DotNetCoreTestBase
	{
		static bool IsDotNetCoreSdk30OrLaterInstalled ()
		{
			return DotNetCoreSdk.Versions.Any (version => version.Major >= 3);
		}

		[Test]
		public async Task UnknownNuGetPackageReferenceId_DesignTimeBuilds ()
		{
			if (!IsDotNetCoreSdk30OrLaterInstalled ()) {
				Assert.Ignore (".NET Core 3 SDK is not installed.");
			}

			FilePath solutionFileName = Util.GetSampleProject ("UnknownPackageReference", "NetStandard21.sln");
			CreateNuGetConfigFile (solutionFileName.ParentDirectory);

			// Run restore but do not check result since this will fail.
			var process = Process.Start ("msbuild", $"/t:Restore /p:RestoreDisableParallel=true \"{solutionFileName}\"");
			Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName)) {
				var project = sol.GetAllProjects ().Single () as DotNetProject;
				var references = (await project.GetFrameworkReferences (ConfigurationSelector.Default, CancellationToken.None)).ToArray ();

				Assert.IsTrue (references.Any ());
				Assert.IsTrue (references.Any (r => r.Include == "NETStandard.Library"));
			}
		}
	}
}
