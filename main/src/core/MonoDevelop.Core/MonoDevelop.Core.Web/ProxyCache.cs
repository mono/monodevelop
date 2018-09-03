//
// From NuGet src/Core
//
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

using System;
using System.Collections.Concurrent;
using System.Net;

namespace MonoDevelop.Core.Web
{
	class ProxyCache : IProxyCache, IProxyCredentialCache
	{
		/// <summary>
		/// Capture the default System Proxy so that it can be re-used by the IProxyFinder
		/// because we can't rely on WebRequest.DefaultWebProxy since someone can modify the DefaultWebProxy
		/// property and we can't tell if it was modified and if we are still using System Proxy Settings or not.
		/// </summary>
		static readonly IWebProxy originalSystemProxy = WebRequest.GetSystemWebProxy ();

		readonly ConcurrentDictionary<Uri, ICredentials> cache = new ConcurrentDictionary<Uri, ICredentials> ();

		public Guid Version { get; private set; } = Guid.NewGuid ();

		public IWebProxy GetProxy (Uri uri)
		{
			if (!IsSystemProxySet (uri))
				return null;

			var systemProxy = GetSystemProxy (uri);
			TryAddProxyCredentialsToCache (systemProxy);
			systemProxy.Credentials = this;
			return systemProxy;
		}

		// Adds new proxy credentials to cache if there's not any in there yet
		bool TryAddProxyCredentialsToCache (WebProxy configuredProxy)
		{
			// If a proxy was cached, it means the stored credentials are incorrect. Use the cached one in this case.
			var proxyCredentials = configuredProxy.Credentials ?? CredentialCache.DefaultCredentials;
			return cache.TryAdd (configuredProxy.Address, proxyCredentials);
		}

		public void UpdateCredential (Uri proxyAddress, NetworkCredential credentials)
		{
			if (credentials == null)
				throw new ArgumentNullException (nameof (credentials));

			cache.AddOrUpdate (
				proxyAddress,
				addValueFactory: _ => { Version = Guid.NewGuid (); return credentials; },
				updateValueFactory: (_, __) => { Version = Guid.NewGuid (); return credentials; });
		}

		public NetworkCredential GetCredential (Uri proxyAddress, string authType)
		{
			ICredentials cachedCredentials;
			if (cache.TryGetValue (proxyAddress, out cachedCredentials)) {
				return cachedCredentials.GetCredential (proxyAddress, authType);
			}

			return null;
		}

		[Obsolete ("Retained for backcompat only. Use UpdateCredential instead")]
		public void Add (IWebProxy proxy)
		{
			var webProxy = proxy as WebProxy;
			if (webProxy != null)
				cache.TryAdd (webProxy.Address, webProxy.Credentials);
		}

		static WebProxy GetSystemProxy (Uri uri)
		{
			// WebRequest.DefaultWebProxy seems to be more capable in terms of getting the default
			// proxy settings instead of the WebRequest.GetSystemProxy()
			var proxyUri = originalSystemProxy.GetProxy (uri);
			return new WebProxy (proxyUri);
		}

		/// <summary>
		/// Return true or false if connecting through a proxy server
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		static bool IsSystemProxySet (Uri uri)
		{
			// The reason for not calling the GetSystemProxy is because the object
			// that will be returned is no longer going to be the proxy that is set by the settings
			// on the users machine only the Address is going to be the same.
			// Not sure why the .NET team did not want to expose all of the useful settings like
			// ByPass list and other settings that we can't get because of it.
			// Anyway the reason why we need the DefaultWebProxy is to see if the uri that we are
			// getting the proxy for to should be bypassed or not. If it should be bypassed then
			// return that we don't need a proxy and we should try to connect directly.
			IWebProxy proxy = WebRequest.DefaultWebProxy;
			if (proxy != null) {
				Uri proxyAddress = new Uri (proxy.GetProxy (uri).AbsoluteUri);
				if (String.Equals (proxyAddress.AbsoluteUri, uri.AbsoluteUri))
					return false;
				if (proxy.IsBypassed (uri))
					return false;
			}

			return proxy != null;
		}
	}
}
