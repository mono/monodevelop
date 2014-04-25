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
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core.Web;

namespace MonoDevelop.Core
{
	/// <summary>
	/// Helper for making web requests with support for authenticated proxies.
	/// </summary>
	public static class WebRequestHelper
	{
		public static IProxyAuthenticationHandler ProxyAuthenticationHandler { get; internal set; }

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
		public static Task<HttpWebResponse> GetResponseAsync (
			Func<HttpWebRequest> createRequest,
			Action<HttpWebRequest> prepareRequest = null,
			CancellationToken token = default(CancellationToken))
		{
			//TODO: make this really async under the covers
			return Task.Factory.StartNew (() => GetResponse (createRequest, prepareRequest, token), token);
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
			var provider = ProxyAuthenticationHandler;
			if (provider == null) {
				var rq = createRequest ();
				if (prepareRequest != null)
					prepareRequest (rq);
				return (HttpWebResponse) rq.GetResponse ();
			}

			HttpWebRequest previousRequest = null;
			IHttpWebResponse previousResponse = null;
			HttpStatusCode? previousStatusCode = null;
			var continueIfFailed = true;
			int proxyCredentialsRetryCount = 0, credentialsRetryCount = 0;

			HttpWebRequest request = null;

			if (token.CanBeCanceled) {
				token.Register (() => {
					var r = request;
					if (r != null)
						r.Abort ();
				});
			}

			while (true) {
				// Create the request
				// NOTE: .NET blocks on DNS here, see http://stackoverflow.com/questions/1232139#1232930
				request = createRequest ();
				request.Proxy = provider.GetCachedProxy (request.RequestUri);

				if (token.IsCancellationRequested) {
					request.Abort ();
					throw new OperationCanceledException (token);
				}

				if (request.Proxy != null && request.Proxy.Credentials == null && request.Proxy is WebProxy) {
					var proxyAddress = ((WebProxy)request.Proxy).Address;
					request.Proxy.Credentials = provider.GetCachedCredentials (proxyAddress, CredentialType.ProxyCredentials) ??
						CredentialCache.DefaultCredentials;
				}

				var retrying = proxyCredentialsRetryCount > 0;
				ICredentials oldCredentials;
				if (!previousStatusCode.HasValue && (previousResponse == null || ShouldKeepAliveBeUsedInRequest (previousRequest, previousResponse))) {
					// Try to use the cached credentials (if any, for the first request)
					request.Credentials = provider.GetCachedCredentials (request.RequestUri, CredentialType.RequestCredentials);

					// If there are no cached credentials, use the default ones
					if (request.Credentials == null)
						request.UseDefaultCredentials = true;
				} else if (previousStatusCode == HttpStatusCode.ProxyAuthenticationRequired) {
					oldCredentials = previousRequest != null && previousRequest.Proxy != null ? previousRequest.Proxy.Credentials : null;
					request.Proxy.Credentials = provider.GetCredentialsFromUser (request.RequestUri, request.Proxy, CredentialType.ProxyCredentials, oldCredentials, retrying);
					continueIfFailed = request.Proxy.Credentials != null;
					proxyCredentialsRetryCount++;
				} else if (previousStatusCode == HttpStatusCode.Unauthorized) {
					oldCredentials = previousRequest != null ? previousRequest.Credentials : null;
					request.Credentials = provider.GetCredentialsFromUser (request.RequestUri, request.Proxy, CredentialType.RequestCredentials, oldCredentials, retrying);
					continueIfFailed = request.Credentials != null;
					credentialsRetryCount++;
				}

				try {
					ICredentials credentials = request.Credentials;

					SetKeepAliveHeaders (request, previousResponse);

					// Wrap the credentials in a CredentialCache in case there is a redirect
					// and credentials need to be kept around.
					request.Credentials = AsCredentialCache (request.Credentials, request.RequestUri);

					// Prepare the request, we do something like write to the request stream
					// which needs to happen last before the request goes out
					if (prepareRequest != null)
						prepareRequest (request);
					var response = (HttpWebResponse) request.GetResponse ();

					// Cache the proxy and credentials
					if (request.Proxy != null) {
						provider.AddProxyToCache (request.Proxy);
						if (request.Proxy is WebProxy)
							provider.AddCredentialsToCache (((WebProxy)request.Proxy).Address, request.Proxy.Credentials, CredentialType.ProxyCredentials);
					}

					provider.AddCredentialsToCache (request.RequestUri, credentials, CredentialType.RequestCredentials);
					provider.AddCredentialsToCache (response.ResponseUri, credentials, CredentialType.RequestCredentials);

					return response;
				} catch (WebException ex) {
					if (ex.Status == WebExceptionStatus.RequestCanceled)
						token.ThrowIfCancellationRequested ();

					using (var response = GetResponse ((HttpWebResponse)ex.Response)) {
						if (response == null && ex.Status != WebExceptionStatus.SecureChannelFailure) {
							// No response, something went wrong so just rethrow
							throw;
						}

						// Special case https connections that might require authentication
						if (ex.Status == WebExceptionStatus.SecureChannelFailure) {
							if (continueIfFailed) {
								if (ex.Message.Contains ("407"))
									previousStatusCode = HttpStatusCode.ProxyAuthenticationRequired;
								else if (ex.Message.Contains ("401"))
									previousStatusCode = HttpStatusCode.Unauthorized;
								else
									previousStatusCode = null;
								continue;
							}

							throw;
						}

						// If we were trying to authenticate the proxy or the request and succeeded, cache the result.
						if (previousStatusCode == HttpStatusCode.ProxyAuthenticationRequired && response.StatusCode != HttpStatusCode.ProxyAuthenticationRequired) {
							provider.AddProxyToCache (request.Proxy);
							provider.AddCredentialsToCache (((WebProxy)request.Proxy).Address, request.Proxy.Credentials, CredentialType.ProxyCredentials);
						} else if (previousStatusCode == HttpStatusCode.Unauthorized && response.StatusCode != HttpStatusCode.Unauthorized) {
							provider.AddCredentialsToCache (request.RequestUri, request.Credentials, CredentialType.RequestCredentials);
							provider.AddCredentialsToCache (response.ResponseUri, request.Credentials, CredentialType.RequestCredentials);
						}

						if (!IsAuthenticationResponse (response) || !continueIfFailed)
							throw;

						previousRequest = request;
						previousResponse = response;
						previousStatusCode = previousResponse.StatusCode;
					}
				}
			}
		}

		static IHttpWebResponse GetResponse (HttpWebResponse response)
		{
			if (response == null)
				return null;

			var httpWebResponse = response as IHttpWebResponse;
			if (httpWebResponse != null)
				return httpWebResponse;

			return new HttpWebResponseWrapper (response);
		}

		static bool IsAuthenticationResponse (IHttpWebResponse response)
		{
			return response != null && (
				response.StatusCode == HttpStatusCode.Unauthorized ||
				response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired
			);
		}

		static void SetKeepAliveHeaders (HttpWebRequest request, IHttpWebResponse previousResponse)
		{
			// KeepAlive is required for NTLM and Kerberos authentication. If we've never been authenticated or are
			// using a different auth, we should not require KeepAlive.
			// REVIEW: The WWW-Authenticate header is tricky to parse so a Equals might not be correct.
			if (previousResponse != null && IsNtlmOrKerberos (previousResponse.AuthType))
				return;

			// This is to work around the "The underlying connection was closed: An unexpected error occurred on a receive." exception.
			request.KeepAlive = false;
			request.ProtocolVersion = HttpVersion.Version10;
		}

		static bool ShouldKeepAliveBeUsedInRequest (HttpWebRequest request, IHttpWebResponse response)
		{
			if (request == null)
				throw new ArgumentNullException ("request");

			if (response == null)
				throw new ArgumentNullException ("response");

			return !request.KeepAlive && IsNtlmOrKerberos (response.AuthType);
		}

		static bool IsNtlmOrKerberos (string authType)
		{
			if (String.IsNullOrEmpty (authType)) {
				return false;
			}

			return authType.IndexOf ("NTLM", StringComparison.OrdinalIgnoreCase) != -1
				|| authType.IndexOf ("Kerberos", StringComparison.OrdinalIgnoreCase) != -1;
		}

		// For unit testing
		interface IHttpWebResponse : IDisposable
		{
			HttpStatusCode StatusCode { get; }

			Uri ResponseUri { get; }

			string AuthType { get; }

			NameValueCollection Headers { get; }
		}

		class HttpWebResponseWrapper : IHttpWebResponse
		{
			readonly HttpWebResponse response;

			public HttpWebResponseWrapper (HttpWebResponse response)
			{
				this.response = response;
			}

			public string AuthType {
				get {
					return response.Headers [HttpResponseHeader.WwwAuthenticate];
				}
			}

			public HttpStatusCode StatusCode {
				get {
					return response.StatusCode;
				}
			}

			public Uri ResponseUri {
				get {
					return response.ResponseUri;
				}
			}

			public NameValueCollection Headers {
				get {
					return response.Headers;
				}
			}

			public void Dispose ()
			{
				if (response != null) {
					response.Close ();
				}
			}
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

		static readonly string[] AuthenticationSchemes = { "Basic", "NTLM", "Negotiate" };

		static ICredentials AsCredentialCache (ICredentials credentials, Uri uri)
		{
			// No credentials then bail
			if (credentials == null)
				return null;

			// Do nothing with default credentials
			if (credentials == CredentialCache.DefaultCredentials || credentials == CredentialCache.DefaultNetworkCredentials)
				return credentials;

			// If this isn't a NetworkCredential then leave it alone
			var networkCredentials = credentials as NetworkCredential;
			if (networkCredentials == null)
				return credentials;

			// Set this up for each authentication scheme we support
			// The reason we're using a credential cache is so that the HttpWebRequest will forward our
			// credentials if there happened to be any redirects in the chain of requests.
			var cache = new CredentialCache ();
			foreach (var scheme in AuthenticationSchemes)
				cache.Add (uri, scheme, networkCredentials);
			return cache;
		}
	}
}
