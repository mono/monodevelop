//
// HttpHandlerResourceV3Extensions.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Code taken from NuGet.Client src/NuGet.Clients/VsExtension/NuGetPackage.cs
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
// Copyright (c) .NET Foundation. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using NuGet;
using NuGet.Credentials;

namespace MonoDevelop.PackageManagement
{
	internal static class HttpHandlerResourceV3Extensions
	{
		public static void InitializeHttpHandlerResourceV3 (CredentialService credentialService)
		{
			// Set up proxy handling for v3 sources.
			// We need to sync the v2 proxy cache and v3 proxy cache so that the user will not
			// get prompted twice for the same authenticated proxy.
			var v2ProxyCache = ProxyCache.Instance;
			NuGet.Protocol.Core.v3.HttpHandlerResourceV3.PromptForProxyCredentials = async (uri, proxy, cancellationToken) => {
				var v2Credentials = v2ProxyCache?.GetProxy (uri)?.Credentials;
				if (v2Credentials != null && proxy.Credentials != v2Credentials) {
					// if cached v2 credentials have not been used, try using it first.
					return v2Credentials;
				}

				return await credentialService
					.GetCredentials (uri, proxy, isProxy: true, cancellationToken: cancellationToken);
			};

			NuGet.Protocol.Core.v3.HttpHandlerResourceV3.ProxyPassed = proxy => {
				// add the proxy to v2 proxy cache.
				v2ProxyCache?.Add (proxy);
			};

			NuGet.Protocol.Core.v3.HttpHandlerResourceV3.PromptForCredentials = async (uri, cancellationToken) => {
				// Get the proxy for this URI so we can pass it to the credentialService methods
				// this lets them use the proxy if they have to hit the network.
				var proxyCache = ProxyCache.Instance;
				var proxy = proxyCache?.GetProxy (uri);

				return await credentialService
					.GetCredentials (uri, proxy: proxy, isProxy: false, cancellationToken: cancellationToken);
			};

			NuGet.Protocol.Core.v3.HttpHandlerResourceV3.CredentialsSuccessfullyUsed = (uri, credentials) => {
				NuGet.CredentialStore.Instance.Add (uri, credentials);
				NuGet.Configuration.CredentialStore.Instance.Add (uri, credentials);
			};
		}
	}
}

