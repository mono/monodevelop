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

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.PackageExtraction;
using NuGet.Packaging.Signing;
using NuGet.ProjectManagement;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public abstract class RestoreTestBase : TestBase
	{
		protected Solution solution;

		protected override void InternalSetup (string rootDir)
		{
			base.InternalSetup (rootDir);
			Xwt.Application.Initialize (Xwt.ToolkitType.Gtk);
			Gtk.Application.Init ();
			DesktopService.Initialize ();
		}

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
			var restoreManager = new MonoDevelopBuildIntegratedRestorer (solutionManager);

			var projects = solution.GetAllDotNetProjects ().Select (p => new DotNetCoreNuGetProject (p));

			return restoreManager.RestorePackages (
				projects,
				CancellationToken.None);
		}

		protected INuGetProjectContext CreateNuGetProjectContext (ISettings settings)
		{
			var context = new FakeNuGetProjectContext {
				LogToConsole = true
			};

			var logger = new LoggerAdapter (context);
			//context.PackageExtractionContext = new PackageExtractionContext (
				//PackageSaveMode.Defaultv2,
				//PackageExtractionBehavior.XmlDocFileSaveMode,
				//ClientPolicyContext.GetClientPolicy (settings, logger),
				//logger);

			return context;
		}

		protected class PackageManagementEventsConsoleLogger : IDisposable
		{
			public PackageManagementEventsConsoleLogger ()
			{
				PackageManagementServices.PackageManagementEvents.PackageOperationMessageLogged += PackageOperationMessageLogged;
			}

			public void Dispose ()
			{
				PackageManagementServices.PackageManagementEvents.PackageOperationMessageLogged -= PackageOperationMessageLogged;
			}

			void PackageOperationMessageLogged (object sender, PackageOperationMessageLoggedEventArgs e)
			{
				Console.WriteLine (e.Message.ToString ());
			}
		}
	}
}
