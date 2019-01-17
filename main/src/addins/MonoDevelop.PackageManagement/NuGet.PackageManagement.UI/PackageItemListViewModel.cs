// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuGet.PackageManagement.UI
{
	// This is the model class behind the package items in the infinite scroll list.
	// Some of its properties, such as Latest Version, Status, are fetched on-demand in the background.
	internal class PackageItemListViewModel : INotifyPropertyChanged
	{        
		public event PropertyChangedEventHandler PropertyChanged;

		public string Id { get; set; }

		public NuGetVersion Version { get; set; }

		private string _author;
		public string Author
		{
			get
			{
				return _author;
			}
			set
			{
				_author = value;
				OnPropertyChanged(nameof(Author));
			}
		}

		// The installed version of the package.
		private NuGetVersion _installedVersion;        
		public NuGetVersion InstalledVersion
		{
			get
			{
				return _installedVersion;
			}
			set
			{
				if (!VersionEquals(_installedVersion, value))
				{
					_installedVersion = value;
					OnPropertyChanged(nameof(InstalledVersion));
				}
			}
		}        

		// The version that can be installed or updated to. It is null
		// if the installed version is already the latest.
		private NuGetVersion _latestVersion;
		public NuGetVersion LatestVersion
		{
			get
			{
				return _latestVersion;
			}
			set
			{
				if (!VersionEquals(_latestVersion, value))
				{
					_latestVersion = value;
					OnPropertyChanged(nameof(LatestVersion));

					// update tool tip
					if (_latestVersion != null)
					{
						var displayVersion = new DisplayVersion(_latestVersion, string.Empty);
						LatestVersionToolTip = string.Format(
							CultureInfo.CurrentCulture,
							"Latest version: {0}",
							displayVersion);
					}
					else
					{
						LatestVersionToolTip = null;
					}
				}
			}
		}

		private string _latestVersionToolTip;

		public string LatestVersionToolTip
		{
			get
			{
				return _latestVersionToolTip;
			}
			set
			{
				_latestVersionToolTip = value;
				OnPropertyChanged(nameof(LatestVersionToolTip));
			}
		}

		private bool _selected;

		public bool Selected
		{
			get { return _selected; }
			set
			{
				if (_selected != value)
				{
					_selected = value;
					OnPropertyChanged(nameof(Selected));
				}
			}
		}

		private bool VersionEquals(NuGetVersion v1, NuGetVersion v2)
		{
			if (v1 == null && v2 == null)
			{
				return true;
			}

			if (v1 == null)
			{
				return false;
			}

			return v1.Equals(v2, VersionComparison.Default);
		}

		private long? _downloadCount;

		public long? DownloadCount
		{
			get
			{
				return _downloadCount;
			}
			set
			{
				_downloadCount = value;
				OnPropertyChanged(nameof(DownloadCount));
			}
		}

		public string Summary { get; set; }

		private PackageStatus _status = PackageStatus.NotInstalled;
		public PackageStatus Status
		{
			get
			{
				//TriggerStatusLoader();
				return _status;
			}

			private set
			{
				bool refresh = _status != value;
				_status = value;

				if (refresh)
				{
					OnPropertyChanged(nameof(Status));
				}
			}
		}

		/*private bool _providersLoaderStarted;

		private AlternativePackageManagerProviders _providers;
		public AlternativePackageManagerProviders Providers
		{
			get
			{
				if (!_providersLoaderStarted && ProvidersLoader != null)
				{
					_providersLoaderStarted = true;
					Task.Run(async () =>
					{
						var result = await ProvidersLoader.Value;

						await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

						Providers = result;
					});
				}

				return _providers;
			}

			private set
			{
				_providers = value;
				OnPropertyChanged(nameof(Providers));
			}
		}

		private Lazy<Task<AlternativePackageManagerProviders>> _providersLoader;
		internal Lazy<Task<AlternativePackageManagerProviders>> ProvidersLoader
		{
			get
			{
				return _providersLoader;
			}

			set
			{
				if (_providersLoader != value)
				{
					_providersLoaderStarted = false;
				}

				_providersLoader = value;
				OnPropertyChanged(nameof(Providers));
			}
		}*/


		public Uri IconUrl { get; set; }

		public Lazy<Task<IEnumerable<VersionInfo>>> Versions { get; set; }

		public Task<IEnumerable<VersionInfo>> GetVersionsAsync() => Versions.Value;

		/*private Lazy<Task<NuGetVersion>> _backgroundLoader;

		private void TriggerStatusLoader()
		{
			if (!_backgroundLoader.IsValueCreated)
			{
				Task.Run(async () =>
				{
					var result = await _backgroundLoader.Value;

					await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					LatestVersion = result;
					Status = GetPackageStatus(LatestVersion, InstalledVersion);
				});
			}
		}

		public void UpdatePackageStatus(IEnumerable<PackageIdentity> installedPackages)
		{
			// Get the minimum version installed in any target project/solution
			InstalledVersion = installedPackages
				.GetPackageVersions(Id)
				.MinOrDefault();

			_backgroundLoader = AsyncLazy.New(
				async () =>
				{
					var packageVersions = await GetVersionsAsync();
					var latestAvailableVersion = packageVersions
						.Select(p => p.Version)
						.MaxOrDefault();

					return latestAvailableVersion;
				});

			OnPropertyChanged(nameof(Status));
		}

		private static PackageStatus GetPackageStatus(NuGetVersion latestAvailableVersion, NuGetVersion installedVersion)
		{
			var status = PackageStatus.NotInstalled;
			if (installedVersion != null)
			{
				status = PackageStatus.Installed;
				if (VersionComparer.VersionRelease.Compare(installedVersion, latestAvailableVersion) < 0)
				{
					status = PackageStatus.UpdateAvailable;
				}
			}

			return status;
		}*/

		protected void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				var e = new PropertyChangedEventArgs(propertyName);
				PropertyChanged(this, e);
			}
		}

		public override string ToString()
		{
			return Id;
		}

		public string Title { get; set; }
		public Uri LicenseUrl { get; set; }
		public Uri ProjectUrl { get; set; }
		public DateTimeOffset? Published { get; set; }
		public string Description { get; set; }
	}
}
