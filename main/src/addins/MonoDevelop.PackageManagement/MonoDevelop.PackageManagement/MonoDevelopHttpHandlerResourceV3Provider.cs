// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// Based on HttpHandlerResourceV3Provider
// From: https://github.com/NuGet/NuGet.Client/

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core.Web;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	class MonoDevelopHttpHandlerResourceV3Provider : ResourceProvider
	{
		public MonoDevelopHttpHandlerResourceV3Provider ()
			: base (typeof (HttpHandlerResource),
				nameof (MonoDevelopHttpHandlerResourceV3Provider),
				NuGetResourceProviderPositions.Last)
		{
		}

		public override Task<Tuple<bool, INuGetResource>> TryCreate (SourceRepository source, CancellationToken token)
		{
			Debug.Assert (source.PackageSource.IsHttp, "HTTP handler requested for a non-http source.");

			HttpHandlerResourceV3 curResource = null;

			if (source.PackageSource.IsHttp) {
				curResource = CreateResource (source.PackageSource);
			}

			return Task.FromResult (new Tuple<bool, INuGetResource> (curResource != null, curResource));
		}

		static HttpHandlerResourceV3 CreateResource (PackageSource packageSource)
		{
			var sourceUri = packageSource.SourceUri;
			var settings = new HttpClientSettings {
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
				SourceAuthenticationRequired = false
			};
			var rootHandler = HttpClientProvider.CreateHttpMessageHandler (sourceUri, settings);

			// HTTP handler pipeline can be injected here, around the client handler
			HttpMessageHandler messageHandler = new MonoDevelopServerWarningLogHandler (rootHandler);

			var innerHandler = messageHandler;

			messageHandler = new StsAuthenticationHandler (packageSource, TokenStore.Instance) {
				InnerHandler = messageHandler
			};

			innerHandler = messageHandler;
			var credentialsHandler = GetHttpCredentialsHandler (rootHandler);
			messageHandler = CreateHttpSourceAuthenticationHandler (packageSource, credentialsHandler, innerHandler);

			// Have to pass a dummy HttpClientProvider since it may not be used by a native implementation, such as on the Mac.
			// It looks like the only place this is used in NuGet is with the DownloadResourcePluginProvider in order to pass the
			// HttpClientHandler's Proxy to the GetCredentialsRequestHandler. There is no plugin support yet in MonoDevelop but
			// this may be a problem in the future. Possibly a custom DownloadResourcePluginProvider would be required or possibly
			// a HttpClientProvider created with just its Proxy property set based on the current proxy for that url.
			var clientHandler = GetOrCreateHttpClientHandler (rootHandler);

			// Get the proxy from NuGet's ProxyCache which has support for proxy information define in the NuGet.Config file.
			var proxy = ProxyCache.Instance.GetUserConfiguredProxy ();
			if (proxy != null) {
				clientHandler.Proxy = proxy;
			}
			var resource = new HttpHandlerResourceV3 (clientHandler, messageHandler);

			return resource;
		}

		static IHttpCredentialsHandler GetHttpCredentialsHandler (HttpMessageHandler handler)
		{
			do {
				if (handler is IHttpCredentialsHandler credentialsHandler) {
					return credentialsHandler;
				}

				var delegatingHandler = handler as DelegatingHandler;
				handler = delegatingHandler?.InnerHandler;
			} while (handler != null);

			return null;
		}

		static HttpMessageHandler CreateHttpSourceAuthenticationHandler (
			PackageSource packageSource,
			IHttpCredentialsHandler credentialsHandler,
			HttpMessageHandler innerHandler)
		{
			ICredentials credentials = CredentialCache.DefaultNetworkCredentials;
			if (packageSource.Credentials != null && packageSource.Credentials.IsValid ()) {
				credentials = new NetworkCredential (packageSource.Credentials.Username, packageSource.Credentials.Password);
			}

			return new Core.Web.HttpSourceAuthenticationHandler (packageSource.SourceUri, credentialsHandler, innerHandler, credentials, GetCredentialsAsync);
		}

		static HttpClientHandler GetOrCreateHttpClientHandler (HttpMessageHandler handler)
		{
			do {
				if (handler is HttpClientHandler clientHandler) {
					return clientHandler;
				}

				var delegatingHandler = handler as DelegatingHandler;
				handler = delegatingHandler?.InnerHandler;
			} while (handler != null);

			return new HttpClientHandler ();
		}

		static Task<ICredentials> GetCredentialsAsync (
			Uri uri,
			IWebProxy proxy,
			CredentialType type,
			CancellationToken cancellationToken)
		{
			return HttpHandlerResourceV3.CredentialService.GetCredentialsAsync (uri, proxy, GetCredentialType (type), null, cancellationToken);
		}

		static CredentialRequestType GetCredentialType (CredentialType type)
		{
			switch (type) {
				case CredentialType.ProxyCredentials:
					return CredentialRequestType.Proxy;
				default:
					return CredentialRequestType.Unauthorized;
			}
		}
	}
}
