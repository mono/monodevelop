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

namespace ICSharpCode.PackageManagement
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
		
		static PackageManagementServices()
		{
			options = new PackageManagementOptions();
			packageRepositoryCache = new PackageRepositoryCache(options.PackageSources, options.RecentPackages);
			userAgentGenerator = new UserAgentGeneratorForRepositoryRequests(packageRepositoryCache);
			registeredPackageRepositories = new RegisteredPackageRepositories(packageRepositoryCache, options);
			//projectTemplatePackageSources = new RegisteredProjectTemplatePackageSources();
			//projectTemplatePackageRepositoryCache = new ProjectTemplatePackageRepositoryCache(packageRepositoryCache, projectTemplatePackageSources);
			
			outputMessagesView = new PackageManagementOutputMessagesView(packageManagementEvents);
			solution = new PackageManagementSolution(registeredPackageRepositories, packageManagementEvents);
			packageActionRunner = new PackageActionRunner(packageManagementEvents);
			
			InitializeCredentialProvider();
		}
		
		static void InitializeCredentialProvider()
		{
			ISettings settings = Settings.LoadDefaultSettings(null, null, null);
			var packageSourceProvider = new PackageSourceProvider(settings);
			var credentialProvider = new SettingsCredentialProvider(new MonoDevelopCredentialProvider(), packageSourceProvider);
			
			HttpClient.DefaultCredentialProvider = credentialProvider;
		}
		
		public static PackageManagementOptions Options {
			get { return options; }
		}
		
		public static IPackageManagementSolution Solution {
			get { return solution; }
		}
		
		public static IRegisteredPackageRepositories RegisteredPackageRepositories {
			get { return registeredPackageRepositories; }
		}
		
		public static IPackageRepositoryCache PackageRepositoryCache {
			get { return packageRepositoryCache; }
		}
		
		public static IPackageManagementEvents PackageManagementEvents {
			get { return packageManagementEvents; }
		}
		
		public static IPackageManagementOutputMessagesView OutputMessagesView {
			get { return outputMessagesView; }
		}
		
		public static IPackageManagementProjectService ProjectService {
			get { return projectService; }
		}
		
		public static IPackageActionRunner PackageActionRunner {
			get { return packageActionRunner; }
		}
		
		public static IPackageRepositoryCache ProjectTemplatePackageRepositoryCache {
			get { return projectTemplatePackageRepositoryCache; }
		}
		
		public static RegisteredPackageSources ProjectTemplatePackageSources {
			get { return projectTemplatePackageSources.PackageSources; }
		}
	}
}
