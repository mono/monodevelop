// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;

namespace NuGet.PackageManagement.UI
{
	internal class PackageLoadContext
	{
		//private readonly Task<PackageCollection> _installedPackagesTask;

		public IEnumerable<SourceRepository> SourceRepositories { get; private set; }

		public NuGetPackageManager PackageManager { get; private set; }

		public NuGetProject[] Projects { get; private set; }

		// Indicates whether the loader is created by solution package manager.
		public bool IsSolution { get; private set; }

		//public IEnumerable<IVsPackageManagerProvider> PackageManagerProviders { get; private set; }

		//public PackageSearchMetadataCache CachedPackages { get; set; }

		public PackageLoadContext(
			IEnumerable<SourceRepository> sourceRepositories,
			bool isSolution,
			NuGetProject project)
		{
			SourceRepositories = sourceRepositories;
			IsSolution = isSolution;
			//PackageManager = uiContext.PackageManager;
			Projects = new [] { project };
			//PackageManagerProviders = uiContext.PackageManagerProviders;

			//_installedPackagesTask = PackageCollection.FromProjectsAsync(Projects, CancellationToken.None);
		}

		//public Task<PackageCollection> GetInstalledPackagesAsync() =>_installedPackagesTask;

		// Returns the list of frameworks that we need to pass to the server during search
		public IEnumerable<string> GetSupportedFrameworks()
		{
			var frameworks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var project in Projects)
			{
				NuGetFramework framework;
				if (project.TryGetMetadata(NuGetProjectMetadataKeys.TargetFramework,
				                           out framework))
				{
					if (framework != null
					    && framework.IsAny)
					{
						// One of the project's target framework is AnyFramework. In this case,
						// we don't need to pass the framework filter to the server.
						return Enumerable.Empty<string>();
					}

					if (framework != null
					    && framework.IsSpecificFramework)
					{
						frameworks.Add(framework.DotNetFrameworkName);
					}
				}
				else
				{
					// we also need to process SupportedFrameworks
					IEnumerable<NuGetFramework> supportedFrameworks;
					if (project.TryGetMetadata(
						NuGetProjectMetadataKeys.SupportedFrameworks,
						out supportedFrameworks))
					{
						foreach (var f in supportedFrameworks)
						{
							if (f.IsAny)
							{
								return Enumerable.Empty<string>();
							}

							frameworks.Add(f.DotNetFrameworkName);
						}
					}
				}
			}

			return frameworks;
		}
	}
}
