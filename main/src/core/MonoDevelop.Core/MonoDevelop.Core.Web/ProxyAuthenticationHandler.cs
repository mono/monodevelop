//
// ProxyAuthenticationHandler.cs
//
// Author:
//       Bojan Rajkovic <bojan.rajkovic@xamarin.com>
//       Michael Hutchinson <mhutch@xamarin.com>
//
// based on NuGet src/Core/Http
//
// Copyright (c) 2013-2014 Xamarin Inc.
// Copyright (c) 2010-2014 Outercurve Foundation
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

using System;
using System.Linq;

using Mono.Addins;
using System.Net;

namespace MonoDevelop.Core.Web
{
	class ProxyAuthenticationHandler : IProxyAuthenticationHandler
	{
		const string WebCredentialProvidersPath = "/MonoDevelop/Core/WebCredentialProviders";

		public ProxyAuthenticationHandler ()
		{
			credentialStore = new CredentialStore ();
			proxyCache = new ProxyCache ();
			credentialProvider = AddinManager.GetExtensionObjects<ICredentialProvider> (WebCredentialProvidersPath).FirstOrDefault ();

			if (credentialProvider == null) {
				LoggingService.LogWarning ("No proxy credential provider was found");
			}
		}

		readonly ProxyCache proxyCache;
		readonly CredentialStore credentialStore;
		readonly ICredentialProvider credentialProvider;

		public void AddProxyToCache (IWebProxy proxy)
		{
			proxyCache.Add (proxy);
		}

		public IWebProxy GetCachedProxy (Uri uri)
		{
			return proxyCache.GetProxy (uri);
		}

		public void AddCredentialsToCache (Uri uri, ICredentials credentials, CredentialType credentialType)
		{
			credentialStore.Add (uri, credentials, credentialType);
		}

		public ICredentials GetCachedCredentials (Uri uri, CredentialType credentialType)
		{
			return credentialStore.GetCredentials (uri, credentialType);
		}

		public ICredentials GetCredentialsFromUser (Uri uri, IWebProxy proxy, CredentialType credentialType, ICredentials existingCredentials, bool retrying)
		{
			return credentialProvider.GetCredentials (uri, proxy, credentialType, existingCredentials, retrying);
		}
	}
}
