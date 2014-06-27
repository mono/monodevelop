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
using System.Collections.Concurrent;

namespace MonoDevelop.Core.Web
{
	class CredentialStore : ICredentialCache
	{
		private readonly ConcurrentDictionary<Uri, ICredentials> _credentialCache = new ConcurrentDictionary<Uri, ICredentials>();

		private static readonly CredentialStore _instance = new CredentialStore();

		public static CredentialStore Instance
		{
			get
			{
				return _instance;
			}
		}

		public ICredentials GetCredentials(Uri uri)
		{
			Uri rootUri = GetRootUri(uri);

			ICredentials credentials;
			if (_credentialCache.TryGetValue(uri, out credentials) ||
				_credentialCache.TryGetValue(rootUri, out credentials))
			{
				return credentials;
			}

			return null;
		}

		public void Add(Uri uri, ICredentials credentials)
		{
			Uri rootUri = GetRootUri(uri);
			_credentialCache.TryAdd(uri, credentials);
			_credentialCache.AddOrUpdate(rootUri, credentials, (u, c) => credentials);
		}

		internal static Uri GetRootUri(Uri uri)
		{
			return new Uri(uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
		}
	}

}
