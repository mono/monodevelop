//
// CredentialStore.cs
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
using System.Net;
using System.Collections.Concurrent;

namespace MonoDevelop.Core.Web
{
	class CredentialStore
	{
		readonly ConcurrentDictionary<Uri, ICredentials> credentialCache = new ConcurrentDictionary<Uri, ICredentials> ();

		public ICredentials GetCredentials (Uri uri, CredentialType credentialType)
		{
			ICredentials credentials;
			if (!credentialCache.TryGetValue (uri, out credentials)) {
				if (credentialType == CredentialType.RequestCredentials &&
					credentialCache.TryGetValue (GetRootUri (uri), out credentials))
					return credentials;
			} else {
				return credentials;
			}

			// Then go to the keychain
			var creds = PasswordService.GetWebUserNameAndPassword (uri);
			return creds != null ? new NetworkCredential (creds.Item1, creds.Item2).AsCredentialCache (uri) : null;
		}

		public void Add (Uri requestUri, ICredentials credentials, CredentialType credentialType)
		{
			credentialCache.TryAdd (requestUri, credentials);
			if (credentialType == CredentialType.RequestCredentials) {
				var rootUri = GetRootUri (requestUri);
				credentialCache.AddOrUpdate (rootUri, credentials, (u, c) => credentials);
			}

			var cred = CredentialsUtility.GetCredentialsForUriFromICredentials (requestUri, credentials);
			if (cred != null && !string.IsNullOrWhiteSpace (cred.UserName) && !string.IsNullOrWhiteSpace (cred.Password))
				PasswordService.AddWebUserNameAndPassword (requestUri, cred.UserName, cred.Password);
		}

		static Uri GetRootUri (Uri uri)
		{
			return new Uri (uri.GetComponents (UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
		}
	}
}
