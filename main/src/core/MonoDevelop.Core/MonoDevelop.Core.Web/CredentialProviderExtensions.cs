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
using System.Net;

namespace MonoDevelop.Core.Web
{
	internal static class CredentialProviderExtensions
	{
		private static readonly string[] _authenticationSchemes = new[] { "Basic", "NTLM", "Negotiate" };

		internal static ICredentials GetCredentials(this ICredentialProvider provider, WebRequest request, CredentialType credentialType, bool retrying = false)
		{
			return provider.GetCredentials(request.RequestUri, request.Proxy, credentialType, retrying);
		}

		internal static ICredentials AsCredentialCache(this ICredentials credentials, Uri uri)
		{
			// No credentials then bail
			if (credentials == null)
			{
				return null;
			}

			// Do nothing with default credentials
			if (credentials == CredentialCache.DefaultCredentials ||
				credentials == CredentialCache.DefaultNetworkCredentials)
			{
				return credentials;
			}

			// If this isn't a NetworkCredential then leave it alone
			var networkCredentials = credentials as NetworkCredential;
			if (networkCredentials == null)
			{
				return credentials;
			}

			// Set this up for each authentication scheme we support
			// The reason we're using a credential cache is so that the HttpWebRequest will forward our
			// credentials if there happened to be any redirects in the chain of requests.
			var cache = new CredentialCache();
			foreach (var scheme in _authenticationSchemes)
			{
				cache.Add(uri, scheme, networkCredentials);
			}
			return cache;
		}
	}
}
