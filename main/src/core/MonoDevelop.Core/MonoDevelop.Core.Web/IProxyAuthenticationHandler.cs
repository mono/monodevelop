//
// IProxyAuthenticationHandler.cs
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

namespace MonoDevelop.Core.Web
{
	///<summary>Proxy authentication handler.</summary>
	public interface IProxyAuthenticationHandler
	{
		/// <summary>
		/// Adds a proxy to the cache.
		/// </summary>
		/// <param name="proxy">Proxy.</param>
		void AddProxyToCache (IWebProxy proxy);

		/// <summary>
		/// Gets a cached proxy for the Url, if available.
		/// </summary>
		/// <returns>The cached proxy.</returns>
		/// <param name="uri">URI for which the proxy will be used.</param>
		IWebProxy GetCachedProxy (Uri uri);

		/// <summary>
		/// Adds credentials to the cache.
		/// </summary>
		/// <param name="uri">URI for which the credentials are valid.</param>
		/// <param name="credentials">Credentials.</param>
		/// <param name="credentialType">Type of the credentials.</param>
		void AddCredentialsToCache (Uri uri, ICredentials credentials, CredentialType credentialType);

		/// <summary>
		/// Gets cached credentials, if available.
		/// </summary>
		/// <returns>The cached credentials.</returns>
		/// <param name="uri">URI for which the credentials will be used.</param>
		/// <param name="credentialType">Type of the credentials.</param>
		ICredentials GetCachedCredentials (Uri uri, CredentialType credentialType);

		/// <summary>
		/// Gets credentials from user.
		/// </summary>
		/// <returns>The credentials from user.</returns>
		/// <param name="uri">URI for which the credentials will be used.</param>
		/// <param name="proxy">Proxy.</param>
		/// <param name="credentialType">Type of the credentials.</param>
		/// <param name="existingCredentials">Existing credentials.</param>
		/// <param name="retrying">If set to <c>true</c> retrying.</param>
		ICredentials GetCredentialsFromUser (Uri uri, IWebProxy proxy, CredentialType credentialType, ICredentials existingCredentials, bool retrying);
	}

}