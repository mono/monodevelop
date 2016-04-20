// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.VisualStudio;
using NuGet.Versioning;

namespace NuGet.PackageManagement.UI
{
	internal class PackageLoader// : ILoader
	{
		public static readonly int MaxDegreeOfParallelism = 16;

		private readonly SourceRepository _sourceRepository;

		private readonly NuGetProject[] _projects;

		// The list of all installed packages. This variable is used for the package status calculation.
		private readonly HashSet<PackageIdentity> _installedPackages;

		private readonly HashSet<string> _installedPackageIds;

		private readonly NuGetPackageManager _packageManager;

		private readonly PackageLoaderOption _option;

		// Indicates whether the loader is created by solution package manager.
		private readonly bool _isSolution;

		private readonly string _searchText;

		//private readonly string LogEntrySource = "NuGet Package Manager";

		// IEnumerable<IVsPackageManagerProvider> _packageManagerProviders;

		public PackageLoader(PackageLoaderOption option,
		                     bool isSolution,
		                     NuGetPackageManager packageManager,
		                     IEnumerable<NuGetProject> projects,
		//                     IEnumerable<IVsPackageManagerProvider> providers,
		                     SourceRepository sourceRepository,
		                     string searchText)
		{
			_sourceRepository = sourceRepository;
			_isSolution = isSolution;
			_packageManager = packageManager;
			_projects = projects.ToArray();
		//	_packageManagerProviders = providers;
			_option = option;
			_searchText = searchText;

			//LoadingMessage = string.IsNullOrWhiteSpace(searchText) ?
			//                       Resources.Text_Loading :
			//                       string.Format(
			//	                       CultureInfo.CurrentCulture,
			//	                       Resources.Text_Searching,
			//	                       searchText);

			_installedPackages = new HashSet<PackageIdentity>(PackageIdentity.Comparer);
			_installedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		public string LoadingMessage { get; }

		private async Task<SearchResult> SearchAsync(int startIndex, CancellationToken ct)
		{
			//if (_option.Filter == Filter.Installed)
			//{
			//	// show only the installed packages
			//	return await SearchInstalledAsync(startIndex, ct);
			//}

			//if (_option.Filter == Filter.Consolidate)
			//{
			//	// show only the installed packages
			//	return await SearchConsolidateAsync(startIndex, ct);
			//}

			// Search all / updates available cannot work without a source repo
			if (_sourceRepository == null)
			{
				return SearchResult.Empty;
			}

//			if (_option.Filter == Filter.UpdatesAvailable)
//			{
//				return await SearchUpdatesAsync(startIndex, ct);
//			}

			// normal search
			var searchResource = await _sourceRepository.GetResourceAsync<UISearchResource>();

			// search in source
			if (searchResource == null)
			{
				return SearchResult.Empty;
			}
			else
			{
				var searchFilter = new SearchFilter();
				searchFilter.IncludePrerelease = _option.IncludePrerelease;
				searchFilter.SupportedFrameworks = GetSupportedFrameworks();

				var searchResults = await searchResource.Search(
					_searchText,
					searchFilter,
					startIndex,
					_option.PageSize + 1,
					ct);

				var items = searchResults.ToList();

				var hasMoreItems = items.Count > _option.PageSize;

				if (hasMoreItems)
				{
					items.RemoveAt(items.Count - 1);
				}

				return new SearchResult
				{
					Items = items,
					HasMoreItems = hasMoreItems,
				};
			}
		}

		// Returns the list of frameworks that we need to pass to the server during search
		private IEnumerable<string> GetSupportedFrameworks()
		{
			var frameworks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var project in _projects)
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

		/// <summary>
		/// Returns the grouped list of installed packages.
		/// </summary>
		/// <param name="latest">
		/// If true, the latest version is returned. Otherwise, the oldest
		/// version is returned.
		/// </param>
		/// <returns></returns>
		private async Task<IEnumerable<PackageIdentity>> GetInstalledPackagesAsync(bool latest, CancellationToken token)
		{
			var installedPackages = new Dictionary<string, PackageIdentity>(
				StringComparer.OrdinalIgnoreCase);

			foreach (var project in _projects)
			{
				foreach (var package in (await project.GetInstalledPackagesAsync(token)))
				{
					PackageIdentity p;
					if (installedPackages.TryGetValue(package.PackageIdentity.Id, out p))
					{
						if (latest)
						{
							if (p.Version < package.PackageIdentity.Version)
							{
								installedPackages[package.PackageIdentity.Id] = package.PackageIdentity;
							}
						}
						else
						{
							if (p.Version > package.PackageIdentity.Version)
							{
								installedPackages[package.PackageIdentity.Id] = package.PackageIdentity;
							}
						}
					}
					else
					{
						installedPackages[package.PackageIdentity.Id] = package.PackageIdentity;
					}
				}
			}

			return installedPackages.Values;
		}

		private async Task<SearchResult> SearchInstalledAsync(int startIndex, CancellationToken cancellationToken)
		{
			var installedPackages = (await GetInstalledPackagesAsync(latest: true, token: cancellationToken))
				.Where(p => p.Id.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) != -1)
				.OrderBy(p => p.Id)
				.Skip(startIndex)
				.Take(_option.PageSize + 1)
				.ToArray();

			var results = new List<UISearchMetadata>();
			var localResource = await _packageManager.PackagesFolderSourceRepository
			                                         .GetResourceAsync<UIMetadataResource>();

			// UIMetadataResource may not be available
			// Given that this is the 'Installed' filter, we ignore failures in reaching the remote server
			// Instead, we will use the local UIMetadataResource
			UIMetadataResource metadataResource;
			try
			{
				metadataResource =
					_sourceRepository == null ?
					null :
					await _sourceRepository.GetResourceAsync<UIMetadataResource>();
			}
			catch (Exception ex)
			{
				metadataResource = null;
				// Write stack to activity log
				//Mvs.ActivityLog.LogError(LogEntrySource, ex.ToString());
			}

			// create tasks to get metadata in parallel
			var bag = new ConcurrentBag<PackageIdentity>(installedPackages);
			var tasks = new List<Task>();
			var metadataList = new ConcurrentQueue<UISearchMetadata>();
			for (int i = 0; i < MaxDegreeOfParallelism; ++i)
			{
				tasks.Add(Task.Run(async () =>
				{
					PackageIdentity packageIdentity;
					while (bag.TryTake(out packageIdentity))
					{
						var metadata = await GetPackageMetadataAsync(
							localResource,
							metadataResource,
							packageIdentity,
							cancellationToken);
						metadataList.Enqueue(metadata);
					}
				}));
			}

			await Task.WhenAll(tasks);
			results = metadataList.ToList();

			return new SearchResult
			{
				Items = results,
				HasMoreItems = installedPackages.Length > _option.PageSize,
			};
		}

		/// <summary>
		/// Returns the list of packages that are consolidatable, i.e. different versions of the
		/// package are installed.
		/// </summary>
		/// <returns></returns>
		public async Task<ICollection<PackageIdentity>> GetConsolidatablePackagesAsync(
			CancellationToken token)
		{
			if (_projects.Length <= 1)
			{
				return new List<PackageIdentity>();
			}

			// the key of the dictionary is the package id, and the value is
			// versions installed.
			var packages = new Dictionary<string, HashSet<NuGetVersion>>(
				StringComparer.OrdinalIgnoreCase);

			foreach (var project in _projects)
			{
				foreach (var package in (await project.GetInstalledPackagesAsync(token)))
				{
					HashSet<NuGetVersion> versions;
					if (!packages.TryGetValue(package.PackageIdentity.Id, out versions))
					{
						versions = new HashSet<NuGetVersion>();
						packages[package.PackageIdentity.Id] = versions;
					}

					versions.Add(package.PackageIdentity.Version);
				}
			}

			var consolidatablePackages = packages
				.Where(p => p.Value.Count >= 2)
				.Select(p => new PackageIdentity(p.Key, p.Value.Max()))
				.ToList();

			return consolidatablePackages;
		}

		private async Task<SearchResult> SearchConsolidateAsync(
			int startIndex,
			CancellationToken cancellationToken)
		{
			var packagesNeedingConsolidation = (await GetConsolidatablePackagesAsync(token: cancellationToken))
				.Where(p => p.Id.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) != -1)
				.OrderBy(p => p.Id)
				.Skip(startIndex)
				.Take(_option.PageSize + 1)
				.ToArray();

			var results = new List<UISearchMetadata>();
			var localResource = await _packageManager.PackagesFolderSourceRepository
			                                         .GetResourceAsync<UIMetadataResource>();

			// UIMetadataResource may not be available
			// Given that this is the 'Installed' filter, we ignore failures in reaching the remote server
			// Instead, we will use the local UIMetadataResource
			UIMetadataResource metadataResource;
			try
			{
				if (_sourceRepository == null)
				{
					metadataResource = null;
				}
				else
				{
					metadataResource = await _sourceRepository.GetResourceAsync<UIMetadataResource>();
				}
			}
			catch (Exception ex)
			{
				metadataResource = null;
				// Write stack to activity log
				//Mvs.ActivityLog.LogError(LogEntrySource, ex.ToString());
			}

			// create tasks to get metadata in parallel
			var bag = new ConcurrentBag<PackageIdentity>(packagesNeedingConsolidation);
			var tasks = new List<Task>();
			var metadataList = new ConcurrentQueue<UISearchMetadata>();
			for (int i = 0; i < MaxDegreeOfParallelism; ++i)
			{
				tasks.Add(Task.Run(async () =>
				{
					PackageIdentity packageIdentity;
					while (bag.TryTake(out packageIdentity))
					{
						var metadata = await GetPackageMetadataAsync(
							localResource,
							metadataResource,
							packageIdentity,
							cancellationToken);
						metadataList.Enqueue(metadata);
					}
				}));
			}

			await Task.WhenAll(tasks);
			results = metadataList.ToList();

			return new SearchResult
			{
				Items = results,
				HasMoreItems = packagesNeedingConsolidation.Length > _option.PageSize,
			};
		}

		// Gets the package metadata from the local resource when the remote source
		// is not available.
		private static async Task<UISearchMetadata> GetPackageMetadataWhenRemoteSourceUnavailable(
			UIMetadataResource localResource,
			PackageIdentity identity,
			CancellationToken cancellationToken)
		{
			UIPackageMetadata packageMetadata = null;
			if (localResource != null)
			{
				var localMetadata = await localResource.GetMetadata(
					identity.Id,
					includePrerelease: true,
					includeUnlisted: true,
					token: cancellationToken);
				packageMetadata = localMetadata.FirstOrDefault(p => p.Identity.Version == identity.Version);
			}

			string summary = string.Empty;
			string title = identity.Id;
			string author = string.Empty;
			if (packageMetadata != null)
			{
				summary = packageMetadata.Summary;
				if (string.IsNullOrEmpty(summary))
				{
					summary = packageMetadata.Description;
				}
				if (!string.IsNullOrEmpty(packageMetadata.Title))
				{
					title = packageMetadata.Title;
				}

				author = string.Join(", ", packageMetadata.Authors);
			}

			var versions = new List<VersionInfo>
			{
				new VersionInfo(identity.Version, downloadCount: null)
			};

			return new UISearchMetadata(
				identity,
				title: title,
				summary: summary,
				author: author,
				downloadCount: packageMetadata?.DownloadCount,
				iconUrl: packageMetadata?.IconUrl,
				versions: ToLazyTask(versions),
				latestPackageMetadata: null);
		}

		private async Task<UISearchMetadata> GetPackageMetadataFromMetadataResourceAsync(
			UIMetadataResource metadataResource,
			PackageIdentity identity,
			CancellationToken cancellationToken)
		{
			var uiPackageMetadatas = await metadataResource.GetMetadata(
				identity.Id,
				_option.IncludePrerelease,
				includeUnlisted: false,
				token: cancellationToken);
			var packageMetadata = uiPackageMetadatas.FirstOrDefault(p => p.Identity.Version == identity.Version);

			string summary = string.Empty;
			string title = identity.Id;
			string author = string.Empty;
			if (packageMetadata != null)
			{
				summary = packageMetadata.Summary;
				if (string.IsNullOrEmpty(summary))
				{
					summary = packageMetadata.Description;
				}
				if (!string.IsNullOrEmpty(packageMetadata.Title))
				{
					title = packageMetadata.Title;
				}

				author = string.Join(", ", packageMetadata.Authors);
			}

			var versions = uiPackageMetadatas.OrderByDescending(m => m.Identity.Version)
			                                 .Select(m => new VersionInfo(m.Identity.Version, m.DownloadCount));
			return new UISearchMetadata(
				identity,
				title: title,
				summary: summary,
				author: author,
				downloadCount: packageMetadata?.DownloadCount,
				iconUrl: packageMetadata?.IconUrl,
				versions: ToLazyTask(versions),
				latestPackageMetadata: packageMetadata);
		}

		/// <summary>
		/// Get the metadata of an installed package.
		/// </summary>
		/// <param name="localResource">The local resource, i.e. the package folder of the solution.</param>
		/// <param name="metadataResource">The remote metadata resource.</param>
		/// <param name="identity">The installed package.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The metadata of the package.</returns>
		private async Task<UISearchMetadata> GetPackageMetadataAsync(
			UIMetadataResource localResource,
			UIMetadataResource metadataResource,
			PackageIdentity identity,
			CancellationToken cancellationToken)
		{
			if (metadataResource == null)
			{
				return await GetPackageMetadataWhenRemoteSourceUnavailable(
					localResource,
					identity,
					cancellationToken);
			}

			try
			{
				var metadata = await GetPackageMetadataFromMetadataResourceAsync(
					metadataResource,
					identity,
					cancellationToken);

				// if the package does not exist in the remote source, NuGet should
				// try getting metadata from the local resource.
				if (String.IsNullOrEmpty(metadata.Summary) && localResource != null)
				{
					return await GetPackageMetadataWhenRemoteSourceUnavailable(
						localResource,
						identity,
						cancellationToken);
				}
				else
				{
					return metadata;
				}
			}
			catch
			{
				// When a v2 package source throws, it throws an InvalidOperationException or WebException
				// When a v3 package source throws, it throws an HttpRequestException

				// The remote source is not available. NuGet should not fail but
				// should use the local resource instead.
				if (localResource != null)
				{
					return await GetPackageMetadataWhenRemoteSourceUnavailable(
						localResource,
						identity,
						cancellationToken);
				}
				else
				{
					throw;
				}
			}
		}

		// Search in installed packages that have updates available
		private async Task<SearchResult> SearchUpdatesAsync(int startIndex, CancellationToken ct)
		{
			var packagesWithUpdates = await GetPackagesWithUpdatesAsync(ct);
			var items = packagesWithUpdates.Skip(startIndex).ToList();

			return new SearchResult
			{
				Items = items,
				HasMoreItems = false
			};
		}

		// Returns the list of installed packages that have updates available.
		public async Task<List<UISearchMetadata>> GetPackagesWithUpdatesAsync(CancellationToken ct)
		{
			var packagesWithUpdates = new List<UISearchMetadata>();
			var metadataResource = await _sourceRepository.GetResourceAsync<UIMetadataResource>();

			if (metadataResource == null)
			{
				return packagesWithUpdates;
			}

			var installedPackages = (await GetInstalledPackagesAsync(latest: false, token: ct))
				.Where(p => p.Id.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) != -1)
				.OrderBy(p => p.Id);

			foreach (var package in installedPackages)
			{
				// only release packages respect the prerel option
				var includePre = _option.IncludePrerelease;

				var data = await metadataResource.GetMetadata(package.Id, includePre, false, ct);
				var highest = data.OrderByDescending(e => e.Identity.Version, VersionComparer.VersionRelease).FirstOrDefault();

				if (highest != null)
				{
					if (VersionComparer.VersionRelease.Compare(package.Version, highest.Identity.Version) < 0)
					{
						var allVersions = data
							.OrderByDescending(e => e.Identity.Version, VersionComparer.VersionRelease)
							.Select(e => new VersionInfo(e.Identity.Version, e.DownloadCount));

						var lazyVersions = ToLazyTask(allVersions);

						var summary = string.IsNullOrEmpty(highest.Summary) ? highest.Description : highest.Summary;

						var title = string.IsNullOrEmpty(highest.Title) ? highest.Identity.Id : highest.Title;

						var searchMetadata = new UISearchMetadata(
							highest.Identity,
							title,
							summary,
							string.Join(", ", highest.Authors),
							highest.DownloadCount,
							highest.IconUrl,
							lazyVersions,
							highest);
						packagesWithUpdates.Add(searchMetadata);
					}
				}
			}

			return packagesWithUpdates;
		}

		public async Task<LoadResult> LoadItemsAsync(int startIndex, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			NuGetEventTrigger.Instance.TriggerEvent(NuGetEvent.PackageLoadBegin);

			List<PackageItemListViewModel> packages = new List<PackageItemListViewModel>();

			var results = await SearchAsync(startIndex, cancellationToken);

			int resultCount = 0;

			foreach (var package in results.Items)
			{
				cancellationToken.ThrowIfCancellationRequested();
				resultCount++;

				var searchResultPackage = new PackageItemListViewModel();
				searchResultPackage.Id = package.Identity.Id;
				searchResultPackage.Version = package.Identity.Version;
				searchResultPackage.IconUrl = package.IconUrl;
				searchResultPackage.Author = package.Author;
				searchResultPackage.DownloadCount = package.DownloadCount;
				searchResultPackage.Title = package.Title;
				searchResultPackage.LicenseUrl = package.LatestPackageMetadata?.LicenseUrl;
				searchResultPackage.ProjectUrl = package.LatestPackageMetadata?.ProjectUrl;
				searchResultPackage.Published = package.LatestPackageMetadata?.Published;
				/*
				if (!_isSolution && _installedPackageIds.Contains(searchResultPackage.Id))
				{
					var installedVersions = _installedPackages.Where(
						p => StringComparer.OrdinalIgnoreCase.Equals(p.Id, searchResultPackage.Id))
					                                          .Select(p => p.Version)
					                                          .ToList();

					if (installedVersions.Count == 1)
					{
						searchResultPackage.InstalledVersion = installedVersions[0];
					}
				}
*/
				var versionList = new Lazy<Task<IEnumerable<VersionInfo>>>(async () =>
				{
					var versions = await package.Versions.Value;

					var filteredVersions = versions
						.Where(v => !v.Version.IsPrerelease || _option.IncludePrerelease)
						.ToList();

					if (!filteredVersions.Any(v => v.Version == searchResultPackage.Version))
					{
						filteredVersions.Add(new VersionInfo(searchResultPackage.Version, downloadCount: null));
					}

					return filteredVersions;
				});

				searchResultPackage.Versions = versionList;
/*
				searchResultPackage.BackgroundLoader = new Lazy<Task<BackgroundLoaderResult>>(
					() => BackgroundLoad(searchResultPackage, versionList));

				if (!_isSolution && _packageManagerProviders.Any())
				{
					searchResultPackage.ProvidersLoader = new Lazy<Task<AlternativePackageManagerProviders>>(
						() => AlternativePackageManagerProviders.CalculateAlternativePackageManagersAsync(
							_packageManagerProviders,
							searchResultPackage.Id,
							_projects[0]));
				}

				// filter out prerelease version when needed.
				if (searchResultPackage.Version.IsPrerelease &&
				    !_option.IncludePrerelease)
				{
					var value = await searchResultPackage.BackgroundLoader.Value;

					if (value.Status == PackageStatus.NotInstalled)
					{
						continue;
					}
				}

				if (_option.Filter == Filter.UpdatesAvailable)
				{
					var value = await searchResultPackage.BackgroundLoader.Value;

					if (value.Status != PackageStatus.UpdateAvailable)
					{
						continue;
					}
				}
*/
				searchResultPackage.Summary = package.Summary;
				packages.Add(searchResultPackage);
			}

			cancellationToken.ThrowIfCancellationRequested();
			NuGetEventTrigger.Instance.TriggerEvent(NuGetEvent.PackageLoadEnd);
			return new LoadResult()
			{
				Items = packages,
				HasMoreItems = results.HasMoreItems,
				NextStartIndex = startIndex + resultCount
			};
		}
/*
		// Load info in the background
		private async Task<BackgroundLoaderResult> BackgroundLoad(
			PackageItemListViewModel package, Lazy<Task<IEnumerable<VersionInfo>>> versions)
		{
			if (_installedPackageIds.Contains(package.Id))
			{
				var versionsUnwrapped = await versions.Value;

				var highestAvailableVersion = versionsUnwrapped
					.Select(v => v.Version)
					.Max();

				var lowestInstalled = _installedPackages
					.Where(p => StringComparer.OrdinalIgnoreCase.Equals(p.Id, package.Id))
					.OrderBy(p => p.Version, VersionComparer.Default)
					.First();

				if (VersionComparer.VersionRelease.Compare(lowestInstalled.Version, highestAvailableVersion) < 0)
				{
					return new BackgroundLoaderResult()
					{
						LatestVersion = highestAvailableVersion,
						InstalledVersion = lowestInstalled.Version,
						Status = PackageStatus.UpdateAvailable
					};
				}

				return new BackgroundLoaderResult()
				{
					LatestVersion = null,
					InstalledVersion = lowestInstalled.Version,
					Status = PackageStatus.Installed
				};
			}

			// the package is not installed. In this case, the latest version is the version
			// of the search result.
			return new BackgroundLoaderResult()
			{
				LatestVersion = package.Version,
				InstalledVersion = null,
				Status = PackageStatus.NotInstalled
			};
		}
*/
		public async Task InitializeAsync()
		{
			// create _installedPackages and _installedPackageIds
			foreach (var project in _projects)
			{
				var installedPackagesInProject = await project.GetInstalledPackagesAsync(CancellationToken.None);
				foreach (var package in installedPackagesInProject)
				{
					_installedPackages.Add(package.PackageIdentity);
					_installedPackageIds.Add(package.PackageIdentity.Id);
				}
			}
		}

		private static Lazy<Task<IEnumerable<VersionInfo>>> ToLazyTask(IEnumerable<VersionInfo> versions)
		{
			return new Lazy<Task<IEnumerable<VersionInfo>>>(() => Task.FromResult(versions));
		}
	}
}
