//
// MonoDevelopNuGetResourceProviderFactory.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
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
using System.Collections.Generic;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.LocalRepositories;

namespace MonoDevelop.PackageManagement
{
	class MonoDevelopNuGetResourceProviderFactory : Repository.ProviderFactory
	{
		static MonoDevelopNuGetResourceProviderFactory ()
		{
			// Some parts of NuGet create new SourceRepository instances which bypasses the custom
			// MonoDevelopHttpHandlerResourceV3Provider used when SourceRepository instances are created by the NuGet
			// addin itself. To prevent this the default provider factory is replaced with a custom provider factory.
			Repository.Provider = new MonoDevelopNuGetResourceProviderFactory ();
		}

		public static IEnumerable<Lazy<INuGetResourceProvider>> GetProviders ()
		{
			return Repository.Provider.GetCoreV3 ();
		}

		/// <summary>
		/// Includes a custom HttpHandlerResourceV3Provider which can use native HttpMessageHandlers defined by MonoDevelop.
		/// </summary>
		public override IEnumerable<Lazy<INuGetResourceProvider>> GetCoreV3 ()
		{
			yield return new Lazy<INuGetResourceProvider> (() => new FeedTypeResourceProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new DependencyInfoResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new DownloadResourcePluginProvider ());
			yield return new Lazy<INuGetResourceProvider> (() => new DownloadResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new MetadataResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new RawSearchResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new RegistrationResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new SymbolPackageUpdateResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new ReportAbuseResourceV3Provider ());
			yield return new Lazy<INuGetResourceProvider> (() => new PackageDetailsUriResourceV3Provider ());
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
			yield return new Lazy<INuGetResourceProvider> (() => new PluginResourceProvider (PackageManagementServices.PluginManager));
			yield return new Lazy<INuGetResourceProvider> (() => new RepositorySignatureResourceProvider ());
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
