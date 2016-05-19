// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Versioning;

namespace NuGet.PackageManagement.UI
{
	// used to manage packages in one project.
	internal class PackageDetailControlModel : DetailControlModel
	{
		public PackageDetailControlModel (NuGetProject nugetProject)
			: this (new [] { nugetProject })
		{
		}

		public PackageDetailControlModel(
			IEnumerable<NuGetProject> nugetProjects)
			: base(nugetProjects)
		{
			Debug.Assert(nugetProjects.Count() == 1);
		}

		//public async override Task SetCurrentPackage(
		//	PackageItemListViewModel searchResultPackage)
		//	ItemFilter filter)
		//{
		//	await base.SetCurrentPackage(searchResultPackage);
		//
		//	UpdateInstalledVersion();
		//}

		public override bool IsSolution
		{
			get { return false; }
		}
/*
		private void UpdateInstalledVersion()
		{
			var installed = InstalledPackageDependencies.Where(p =>
				StringComparer.OrdinalIgnoreCase.Equals(p.Id, Id)).OrderByDescending(p => p.VersionRange?.MinVersion, VersionComparer.Default);

			var dependency = installed.FirstOrDefault(package => package.VersionRange != null && package.VersionRange.HasLowerBound);

			if (dependency != null)
			{
				InstalledVersion = dependency.VersionRange.MinVersion;
			}
			else
			{
				InstalledVersion = null;
			}
		}
*/
		public override void Refresh()
		{
//			UpdateInstalledVersion();
//			CreateVersions();
		}

		private static bool HasId(string id, IEnumerable<PackageIdentity> packages)
		{
			return packages.Any(p =>
				StringComparer.OrdinalIgnoreCase.Equals(p.Id, id));
		}

		protected override void CreateVersions()
		{
/*			_versions = new List<DisplayVersion>();
			var installedDependency = InstalledPackageDependencies.Where(p =>
				StringComparer.OrdinalIgnoreCase.Equals(p.Id, Id) && p.VersionRange != null && p.VersionRange.HasLowerBound)
				.OrderByDescending(p => p.VersionRange.MinVersion)
				.FirstOrDefault();

			// installVersion is null if the package is not installed
			var installedVersion = installedDependency?.VersionRange?.MinVersion;

			var allVersions = _allPackageVersions.OrderByDescending(v => v);
			var latestPrerelease = allVersions.FirstOrDefault(v => v.IsPrerelease);
			var latestStableVersion = allVersions.FirstOrDefault(v => !v.IsPrerelease);

			// Add lastest prerelease if neeeded
			if (latestPrerelease != null
			    && (latestStableVersion == null || latestPrerelease > latestStableVersion) &&
			    !latestPrerelease.Equals(installedVersion))
			{
				_versions.Add(new DisplayVersion(latestPrerelease, Resources.Version_LatestPrerelease));
			}

			// Add latest stable if needed
			if (latestStableVersion != null && 
			    !latestStableVersion.Equals(installedVersion))
			{
				_versions.Add(new DisplayVersion(latestStableVersion, Resources.Version_LatestStable));
			}

			// add a separator
			if (_versions.Count > 0)
			{
				_versions.Add(null);
			}

			foreach (var version in allVersions)
			{
				if (!version.Equals(installedVersion))
				{
					_versions.Add(new DisplayVersion(version, string.Empty));
				}
			}

			SelectVersion();

			OnPropertyChanged(nameof(Versions));
*/		}

		private NuGetVersion _installedVersion;

		public NuGetVersion InstalledVersion
		{
			get { return _installedVersion; }
			private set
			{
				_installedVersion = value;
				OnPropertyChanged(nameof(InstalledVersion));
			}
		}

		//public override IEnumerable<NuGetProject> GetSelectedProjects(UserAction action)
		//{
		//	return _nugetProjects;
		//}

		public IEnumerable<NuGetVersion> AllPackageVersions {
			get { return _allPackageVersions; }
		}
	}
}