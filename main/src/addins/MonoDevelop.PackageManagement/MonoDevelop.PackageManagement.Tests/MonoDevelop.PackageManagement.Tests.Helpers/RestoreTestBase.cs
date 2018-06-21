//
// RestoreTestBase.cs
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
using MonoDevelop.Projects;
using NUnit.Framework;
using NuGet.PackageManagement;
using UnitTests;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public abstract class RestoreTestBase : TestBase
	{
		protected Solution solution;

		[TearDown]
		public void TearDownTest ()
		{
			solution?.Dispose ();
		}

		/// <summary>
		/// Clear all other package sources and just use the main NuGet package source when
		/// restoring the packages for the project temlate tests.
		/// </summary>
		protected static void CreateNuGetConfigFile (FilePath directory)
		{
			var fileName = directory.Combine ("NuGet.Config");

			string xml =
				"<configuration>\r\n" +
				"  <packageSources>\r\n" +
				"    <clear />\r\n" +
				"    <add key=\"NuGet v3 Official\" value=\"https://api.nuget.org/v3/index.json\" />\r\n" +
				"  </packageSources>\r\n" +
				"</configuration>";

			File.WriteAllText (fileName, xml);
		}

		protected static Task<PackageRestoreResult> RestoreNuGetPackages (Solution solution)
		{
			var solutionManager = new MonoDevelopSolutionManager (solution);
			var context = new FakeNuGetProjectContext {
				LogToConsole = true
			};

			var restoreManager = new PackageRestoreManager (
				solutionManager.CreateSourceRepositoryProvider (),
				solutionManager.Settings,
				solutionManager
			);

			return restoreManager.RestoreMissingPackagesInSolutionAsync (
				solutionManager.SolutionDirectory,
				context,
				CancellationToken.None);
		}

		protected static Task RestoreDotNetCoreNuGetPackages (Solution solution)
		{
			var solutionManager = new MonoDevelopSolutionManager (solution);
			var context = new FakeNuGetProjectContext {
				LogToConsole = true
			};

			var restoreManager = new MonoDevelopBuildIntegratedRestorer (solutionManager);

			var projects = solution.GetAllDotNetProjects ().Select (p => new DotNetCoreNuGetProject (p));

			return restoreManager.RestorePackages (
				projects,
				CancellationToken.None);
		}
	}
}
