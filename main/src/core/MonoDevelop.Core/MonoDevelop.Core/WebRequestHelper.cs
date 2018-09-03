//
// WebRequestHelper.cs
//
// Author:
//       Bojan Rajkovic <bojan.rajkovic@xamarin.com>
//       Michael Hutchinson <mhutch@xamarin.com>
//
// based on NuGet src/Core/Http/RequestHelper.cs
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core.Web;
using Mono.Addins;
using System.Linq;

namespace MonoDevelop.Core
{
	/// <summary>
	/// Helper for making web requests with support for authenticated proxies.
	/// </summary>
	public static class WebRequestHelper
	{
		const string WebCredentialProvidersPath = "/MonoDevelop/Core/WebCredentialProviders";

		static ProxyCache proxyCache;
		static ICredentialProvider credentialProvider;

		internal static void Initialize ()
		{
			proxyCache = new ProxyCache ();
			credentialProvider = AddinManager.GetExtensionObjects<ICredentialProvider> (WebCredentialProvidersPath).FirstOrDefault ();

			if (credentialProvider != null) {
				credentialProvider = new CachingCredentialProvider (credentialProvider);
			} else {
				LoggingService.LogWarning ("No proxy credential provider was found");
				credentialProvider = new NullCredentialProvider ();
			}
		}

		public static ICredentialProvider CredentialProvider { get { return credentialProvider; } }

		[Obsolete]
		public static IProxyAuthenticationHandler ProxyAuthenticationHandler { get; internal set; }

		static internal ProxyCache ProxyCache => proxyCache;

		/// <summary>
		/// Gets the web response, using the <see cref="ProxyAuthenticationHandler"/> to handle proxy authentication
		/// if necessary.
		/// </summary>
		/// <returns>The response.</returns>
		/// <param name="createRequest">Callback for creating the request.</param>
		/// <param name="prepareRequest">Callback for preparing the request, e.g. writing the request stream.</param>
		/// <param name="token">Cancellation token.</param>
		/// <remarks>
		/// Keeps sending requests until a response code that doesn't require authentication happens or if the request
		/// requires authentication and the user has stopped trying to enter them (i.e. they hit cancel when they are prompted).
		/// </remarks>
		public static async Task<HttpWebResponse> GetResponseAsync (
			Func<HttpWebRequest> createRequest,
			Action<HttpWebRequest> prepareRequest = null,
			CancellationToken token = default(CancellationToken))
		{
			if (prepareRequest == null) {
				prepareRequest = r => {};
			}

			if (credentialProvider == null) {
				var req = createRequest ();
				req.MakeCancelable (token);
				prepareRequest (req);

				return (HttpWebResponse) await req.GetResponseAsync ().ConfigureAwait (false);
			}

			var handler = new RequestHelper (
				createRequest, prepareRequest, proxyCache, CredentialStore.Instance, credentialProvider
			);

			return await handler.GetResponseAsync (token).ConfigureAwait (false);
		}

		/// <summary>
		/// Gets the web response, using the <see cref="ProxyAuthenticationHandler"/> to handle proxy authentication
		/// if necessary.
		/// </summary>
		/// <returns>The response.</returns>
		/// <param name="createRequest">Callback for creating the request.</param>
		/// <param name="prepareRequest">Callback for preparing the request, e.g. writing the request stream.</param>
		/// <param name="token">Cancellation token.</param>
		/// <remarks>
		/// Keeps sending requests until a response code that doesn't require authentication happens or if the request
		/// requires authentication and the user has stopped trying to enter them (i.e. they hit cancel when they are prompted).
		/// </remarks>
		public static HttpWebResponse GetResponse (
			Func<HttpWebRequest> createRequest,
			Action<HttpWebRequest> prepareRequest = null,
			CancellationToken token = default(CancellationToken))
		{
			if (prepareRequest == null) {
				prepareRequest = r => {};
			}

			if (credentialProvider == null) {
				var req = createRequest ();
				req.MakeCancelable (token);
				prepareRequest (req);
				return (HttpWebResponse) req.GetResponse ();
			}

			var handler = new RequestHelper (
				createRequest, prepareRequest, proxyCache, CredentialStore.Instance, credentialProvider
			);

			return handler.GetResponse (token);
		}

		/// <summary>
		/// Determines whether an error code is likely to have been caused by internet reachability problems.
		/// </summary>
		public static bool IsCannotReachInternetError (this WebExceptionStatus status)
		{
			switch (status) {
			case WebExceptionStatus.NameResolutionFailure:
			case WebExceptionStatus.ConnectFailure:
			case WebExceptionStatus.ConnectionClosed:
			case WebExceptionStatus.ProxyNameResolutionFailure:
			case WebExceptionStatus.SendFailure:
			case WebExceptionStatus.Timeout:
				return true;
			default:
				return false;
			}
		}

		static void MakeCancelable (this HttpWebRequest request, CancellationToken token)
		{
			if (!token.CanBeCanceled)
				return;
			token.Register (() => {
				var r = request;
				if (r != null)
					r.Abort ();
			});
		}

		// RequestHelper doesn't cache proxy credentials, only request credentials. We'll cache proxy credentials
		// to avoid propting auth for the system store multiple times.
		//
		// We implement caching here so we can use the same store without making it public
		// and so that the providers don't have to implement caching too.
		class CachingCredentialProvider : ICredentialProvider
		{
			readonly ICredentialProvider wrapped;
			readonly object locker = new object ();

			public CachingCredentialProvider (ICredentialProvider wrapped)
			{
				this.wrapped = wrapped;
			}

			public ICredentials GetCredentials (Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
			{
				if (credentialType != CredentialType.ProxyCredentials)
					return wrapped.GetCredentials (uri, proxy, credentialType, retrying);

				var proxyUri = proxy.GetProxy (uri);
				if (proxyUri == null)
					return null;

				if (!retrying) {
					var cached = CredentialStore.Instance.GetCredentials (proxyUri);
					if (cached != null)
						return cached;
				}

				lock (locker) {
					if (!retrying) {
						var cached = CredentialStore.Instance.GetCredentials (proxyUri);
						if (cached != null)
							return cached;
					}

					var creds = wrapped.GetCredentials (uri, proxy, credentialType, retrying);
					
					if (creds != null)
						CredentialStore.Instance.Add (proxyUri, creds);
					
					return creds;
				}
			}
		}
	}
}
