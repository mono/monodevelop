// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// Based on FactoryExtensionsV3 and FactoryExtensionsVS
// From: https://github.com/NuGet/NuGet.Client/

using System;
using System.Collections.Generic;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.LocalRepositories;

namespace MonoDevelop.PackageManagement
{
	static class RepositoryProviderFactoryExtensions
	{
		public static IEnumerable<Lazy<INuGetResourceProvider>> GetMonoDevelop (this Repository.ProviderFactory factory)
		{
			foreach (Lazy<INuGetResourceProvider> item in Repository.Provider.GetMonoDevelopCoreV3 ()) {
				yield return item;
			}
		}

		/// <summary>
		/// Includes a custom HttpHandlerResourceV3Provider which can use native HttpMessageHandlers defined by MonoDevelop.
		/// </summary>
		public static IEnumerable<Lazy<INuGetResourceProvider>> GetMonoDevelopCoreV3 (this Repository.ProviderFactory factory)
		{
			yield return new Lazy<INuGetResourceProvider> (() => new FeedTypeResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new DependencyInfoResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new DownloadResourcePluginProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new DownloadResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new MetadataResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new RawSearchResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new RegistrationResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new ReportAbuseResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new ServiceIndexResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new ODataServiceDocumentResourceV2Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new MonoDevelopHttpHandlerResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new HttpSourceResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new PluginFindPackageByIdResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new HttpFileSystemBasedFindPackageByIdResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new RemoteV3FindPackageByIdResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new RemoteV2FindPackageByIdResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new LocalV3FindPackageByIdResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new LocalV2FindPackageByIdResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new PackageUpdateResourceV2Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new PackageUpdateResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new DependencyInfoResourceV2FeedProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new DownloadResourceV2FeedProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new MetadataResourceV2FeedProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new V3FeedListResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new V2FeedListResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new LocalPackageListResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new PackageSearchResourceV2FeedProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new PackageSearchResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new PackageMetadataResourceV2FeedProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new PackageMetadataResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new AutoCompleteResourceV2FeedProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new AutoCompleteResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new PluginResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new FindLocalPackagesResourceUnzippedProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new FindLocalPackagesResourceV2Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new FindLocalPackagesResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new FindLocalPackagesResourcePackagesConfigProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new LocalAutoCompleteResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new LocalDependencyInfoResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new LocalDownloadResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new LocalMetadataResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new LocalPackageMetadataResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new LocalPackageSearchResourceProvider ());
		}
	}
}
