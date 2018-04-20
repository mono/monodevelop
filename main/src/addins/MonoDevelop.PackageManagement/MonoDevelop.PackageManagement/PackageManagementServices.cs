﻿// 
// PackageManagementServices.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012-2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

namespace MonoDevelop.PackageManagement
{
	public static class PackageManagementServices
	{
		static readonly PackageManagementOptions options;
		static readonly PackageManagementEvents packageManagementEvents = new PackageManagementEvents();
		static readonly PackageManagementProjectService projectService = new PackageManagementProjectService();
		static readonly BackgroundPackageActionRunner backgroundPackageActionRunner;
		static readonly IPackageManagementProgressMonitorFactory progressMonitorFactory;
		static readonly ProjectTargetFrameworkMonitor projectTargetFrameworkMonitor;
		static readonly PackageCompatibilityHandler packageCompatibilityHandler;
		static readonly UpdatedNuGetPackagesInWorkspace updatedPackagesInWorkspace;
		static readonly PackageManagementProjectOperations projectOperations;
		static readonly PackageManagementWorkspace workspace;
		static readonly PackageManagementCredentialService credentialService;
		static readonly AnalyzerPackageMonitor analyzerPackageMonitor;
		static readonly MonoDevelopHttpUserAgent userAgent = new MonoDevelopHttpUserAgent ();
		static readonly NuGetConfigFileChangedMonitor nuGetConfigFileChangedMonitor = new NuGetConfigFileChangedMonitor ();

		static PackageManagementServices()
		{
			options = new PackageManagementOptions();

			progressMonitorFactory = new PackageManagementProgressMonitorFactory ();
			backgroundPackageActionRunner = new BackgroundPackageActionRunner (progressMonitorFactory, packageManagementEvents);

			projectTargetFrameworkMonitor = new ProjectTargetFrameworkMonitor (projectService);
			packageCompatibilityHandler = new PackageCompatibilityHandler ();
			packageCompatibilityHandler.MonitorTargetFrameworkChanges (projectTargetFrameworkMonitor);

			updatedPackagesInWorkspace = new UpdatedNuGetPackagesInWorkspace (packageManagementEvents);

			projectOperations = new PackageManagementProjectOperations (backgroundPackageActionRunner, packageManagementEvents);

			workspace = new PackageManagementWorkspace ();

			credentialService = new PackageManagementCredentialService ();
			credentialService.Initialize ();

			ConfigureNuGetSdkResolver ();

			PackageManagementBackgroundDispatcher.Initialize ();

			nuGetConfigFileChangedMonitor.MonitorFileChanges ();

			//analyzerPackageMonitor = new AnalyzerPackageMonitor ();
			MonoDevelop.Refactoring.PackageInstaller.PackageInstallerServiceFactory.PackageServices = new MonoDevelop.PackageManagement.Refactoring.NuGetPackageServicesProxy ();
		}

		internal static void InitializeCredentialService ()
		{
			credentialService.Initialize ();
		}

		/// <summary>
		/// Tell the NuGet SDK resolver included with MSBuild to look at the NuGet addin directory when
		/// resolving the NuGet assemblies. The NuGet SDK resolver will download NuGet packages for
		/// .NET Core projects that use SDKs provided by a NuGet package.
		/// </summary>
		static void ConfigureNuGetSdkResolver ()
		{
			string directory = Path.GetDirectoryName (typeof (PackageManagementServices).Assembly.Location);
			Environment.SetEnvironmentVariable ("MSBUILD_NUGET_PATH", directory);
		}

		internal static PackageManagementOptions Options {
			get { return options; }
		}
		
		internal static IPackageManagementEvents PackageManagementEvents {
			get { return packageManagementEvents; }
		}
		
		internal static IPackageManagementProjectService ProjectService {
			get { return projectService; }
		}

		internal static IBackgroundPackageActionRunner BackgroundPackageActionRunner {
			get { return backgroundPackageActionRunner; }
		}

		internal static IPackageManagementProgressMonitorFactory ProgressMonitorFactory {
			get { return progressMonitorFactory; }
		}

		internal static IUpdatedNuGetPackagesInWorkspace UpdatedPackagesInWorkspace {
			get { return updatedPackagesInWorkspace; }
		}

		public static IPackageManagementProjectOperations ProjectOperations {
			get { return projectOperations; }
		}

		internal static PackageManagementWorkspace Workspace {
			get { return workspace; }
		}
	}
}
