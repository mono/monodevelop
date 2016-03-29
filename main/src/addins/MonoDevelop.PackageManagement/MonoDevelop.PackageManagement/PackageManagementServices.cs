// 
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
using NuGet;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	public static class PackageManagementServices
	{
		static readonly PackageManagementOptions options;
		static readonly PackageManagementSolution solution;
		static readonly RegisteredPackageRepositories registeredPackageRepositories;
		static readonly PackageManagementEvents packageManagementEvents = new PackageManagementEvents();
		static readonly PackageManagementProjectService projectService = new PackageManagementProjectService();
		static readonly PackageManagementOutputMessagesView outputMessagesView;
		static readonly PackageActionRunner packageActionRunner;
		static readonly IPackageRepositoryCache projectTemplatePackageRepositoryCache;
		static readonly RegisteredProjectTemplatePackageSources projectTemplatePackageSources;
		static readonly PackageRepositoryCache packageRepositoryCache;
		static readonly UserAgentGeneratorForRepositoryRequests userAgentGenerator;
		static readonly BackgroundPackageActionRunner backgroundPackageActionRunner;
		static readonly IPackageManagementProgressMonitorFactory progressMonitorFactory;
		static readonly PackageManagementProgressProvider progressProvider;
		static readonly ProjectTargetFrameworkMonitor projectTargetFrameworkMonitor;
		static readonly PackageCompatibilityHandler packageCompatibilityHandler;
		static readonly UpdatedPackagesInSolution updatedPackagesInSolution;
		static readonly PackageManagementProjectOperations projectOperations;

		static PackageManagementServices()
		{
			options = new PackageManagementOptions();
			packageRepositoryCache = new PackageRepositoryCache (options);
			userAgentGenerator = new UserAgentGeneratorForRepositoryRequests ();
			userAgentGenerator.Register (packageRepositoryCache);
			progressProvider = new PackageManagementProgressProvider (packageRepositoryCache);
			registeredPackageRepositories = new RegisteredPackageRepositories(packageRepositoryCache, options);
			projectTemplatePackageSources = new RegisteredProjectTemplatePackageSources();
			projectTemplatePackageRepositoryCache = new ProjectTemplatePackageRepositoryCache(projectTemplatePackageSources);
			
			outputMessagesView = new PackageManagementOutputMessagesView(packageManagementEvents);
			solution = new PackageManagementSolution (registeredPackageRepositories, projectService, packageManagementEvents);
			packageActionRunner = new PackageActionRunner(packageManagementEvents);

			progressMonitorFactory = new PackageManagementProgressMonitorFactory ();
			backgroundPackageActionRunner = new BackgroundPackageActionRunner (progressMonitorFactory, packageManagementEvents, progressProvider);

			projectTargetFrameworkMonitor = new ProjectTargetFrameworkMonitor (projectService);
			packageCompatibilityHandler = new PackageCompatibilityHandler ();
			packageCompatibilityHandler.MonitorTargetFrameworkChanges (projectTargetFrameworkMonitor);

			updatedPackagesInSolution = new UpdatedPackagesInSolution (solution, registeredPackageRepositories, packageManagementEvents);

			projectOperations = new PackageManagementProjectOperations (solution, registeredPackageRepositories, backgroundPackageActionRunner, packageManagementEvents);

			InitializeCredentialProvider();
			PackageManagementBackgroundDispatcher.Initialize ();
		}
		
		internal static void InitializeCredentialProvider()
		{
			HttpClient.DefaultCredentialProvider = CreateSettingsCredentialProvider (new MonoDevelopCredentialProvider ());
		}

		static SettingsCredentialProvider CreateSettingsCredentialProvider (ICredentialProvider credentialProvider)
		{
			ISettings settings = LoadSettings ();
			var packageSourceProvider = new PackageSourceProvider (settings);
			return new SettingsCredentialProvider(credentialProvider, packageSourceProvider);
		}

		static ISettings LoadSettings ()
		{
			try {
				return Settings.LoadDefaultSettings (null, null, null);
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to load NuGet.Config.", ex);
			}
			return NullSettings.Instance;
		}

		internal static PackageManagementOptions Options {
			get { return options; }
		}
		
		internal static IPackageManagementSolution Solution {
			get { return solution; }
		}
		
		internal static IRegisteredPackageRepositories RegisteredPackageRepositories {
			get { return registeredPackageRepositories; }
		}
		
		internal static IPackageRepositoryCache PackageRepositoryCache {
			get { return packageRepositoryCache; }
		}
		
		internal static IPackageManagementEvents PackageManagementEvents {
			get { return packageManagementEvents; }
		}
		
		internal static IPackageManagementOutputMessagesView OutputMessagesView {
			get { return outputMessagesView; }
		}
		
		internal static IPackageManagementProjectService ProjectService {
			get { return projectService; }
		}
		
		internal static IPackageActionRunner PackageActionRunner {
			get { return packageActionRunner; }
		}
		
		internal static IPackageRepositoryCache ProjectTemplatePackageRepositoryCache {
			get { return projectTemplatePackageRepositoryCache; }
		}
		
		internal static RegisteredPackageSources ProjectTemplatePackageSources {
			get { return projectTemplatePackageSources.PackageSources; }
		}

		internal static IBackgroundPackageActionRunner BackgroundPackageActionRunner {
			get { return backgroundPackageActionRunner; }
		}

		internal static IPackageManagementProgressMonitorFactory ProgressMonitorFactory {
			get { return progressMonitorFactory; }
		}

		internal static IRecentPackageRepository RecentPackageRepository {
			get { return packageRepositoryCache.RecentPackageRepository; }
		}

		internal static IProgressProvider ProgressProvider {
			get { return progressProvider; }
		}

		internal static IUpdatedPackagesInSolution UpdatedPackagesInSolution {
			get { return updatedPackagesInSolution; }
		}

		public static IPackageManagementProjectOperations ProjectOperations {
			get { return projectOperations; }
		}
	}
}
