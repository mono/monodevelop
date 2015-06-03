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
using System.Collections.Specialized;
using System.Threading;

namespace MonoDevelop.Core.Web
{
	/// <summary>
	/// This class is used to keep sending requests until a response code that doesn't require
	/// authentication happens or if the request requires authentication and
	/// the user has stopped trying to enter them (i.e. they hit cancel when they are prompted).
	/// </summary>
	class RequestHelper
	{
		private Func<HttpWebRequest> _createRequest;
		private Action<HttpWebRequest> _prepareRequest;
		private IProxyCache _proxyCache;
		private ICredentialCache _credentialCache;
		private ICredentialProvider _credentialProvider;

		HttpWebRequest _previousRequest;
		IHttpWebResponse _previousResponse;
		HttpStatusCode? _previousStatusCode;
		int _credentialsRetryCount;
		bool _usingSTSAuth;
		bool _continueIfFailed;
		int _proxyCredentialsRetryCount;
		bool _basicAuthIsUsedInPreviousRequest;

		public RequestHelper(Func<HttpWebRequest> createRequest,
			Action<HttpWebRequest> prepareRequest,
			IProxyCache proxyCache,
			ICredentialCache credentialCache,
			ICredentialProvider credentialProvider)
		{
			_createRequest = createRequest;
			_prepareRequest = prepareRequest;
			_proxyCache = proxyCache;
			_credentialCache = credentialCache;
			_credentialProvider = credentialProvider;
		}

		static void MakeCancelable (HttpWebRequest request, CancellationToken token)
		{
			if (token.CanBeCanceled)
				token.Register (request.Abort);
		}

		public HttpWebResponse GetResponse(CancellationToken token)
		{
			_previousRequest = null;
			_previousResponse = null;
			_previousStatusCode = null;
			_usingSTSAuth = false;
			_continueIfFailed = true;
			_proxyCredentialsRetryCount = 0;
			_credentialsRetryCount = 0;
			int failureCount = 0;
			const int MaxFailureCount = 10;

			while (true)
			{
				// Create the request
				var request = (HttpWebRequest)_createRequest();
				MakeCancelable (request, token);
				ConfigureRequest(request);

				try
				{
					var auth = request.Headers["Authorization"];
					_basicAuthIsUsedInPreviousRequest = (auth != null)
						&& auth.StartsWith("Basic ", StringComparison.Ordinal);

					// Prepare the request, we do something like write to the request stream
					// which needs to happen last before the request goes out
					_prepareRequest(request);

					HttpWebResponse response = (HttpWebResponse)request.GetResponse();

					// Cache the proxy and credentials
					_proxyCache.Add(request.Proxy);

					ICredentials credentials = request.Credentials;
					_credentialCache.Add(request.RequestUri, credentials);
					_credentialCache.Add(response.ResponseUri, credentials);

					return response;
				}
				catch (WebException ex)
				{
					++failureCount;
					if (failureCount >= MaxFailureCount)
					{
						throw;
					}

					using (IHttpWebResponse response = GetResponse(ex.Response))
					{
						if (response == null &&
							ex.Status != WebExceptionStatus.SecureChannelFailure)
						{
							// No response, something went wrong so just rethrow
							throw;
						}

						// Special case https connections that might require authentication
						if (ex.Status == WebExceptionStatus.SecureChannelFailure)
						{
							if (_continueIfFailed)
							{
								// Act like we got a 401 so that we prompt for credentials on the next request
								_previousStatusCode = HttpStatusCode.Unauthorized;
								continue;
							}
							throw;
						}

						// If we were trying to authenticate the proxy or the request and succeeded, cache the result.
						if (_previousStatusCode == HttpStatusCode.ProxyAuthenticationRequired &&
							response.StatusCode != HttpStatusCode.ProxyAuthenticationRequired)
						{
							_proxyCache.Add(request.Proxy);
						}
						else if (_previousStatusCode == HttpStatusCode.Unauthorized &&
							response.StatusCode != HttpStatusCode.Unauthorized)
						{
							_credentialCache.Add(request.RequestUri, request.Credentials);
							_credentialCache.Add(response.ResponseUri, request.Credentials);
						}

						_usingSTSAuth = STSAuthHelper.TryRetrieveSTSToken(request.RequestUri, response);

						if (!IsAuthenticationResponse(response) || !_continueIfFailed)
						{
							throw;
						}

						_previousRequest = request;
						_previousResponse = response;
						_previousStatusCode = _previousResponse.StatusCode;
					}
				}
			}
		}

		private void ConfigureRequest(HttpWebRequest request)
		{
			request.Proxy = _proxyCache.GetProxy(request.RequestUri);
			if (request.Proxy != null && request.Proxy.Credentials == null)
			{
				request.Proxy.Credentials = CredentialCache.DefaultCredentials;
			}

			if (_previousResponse == null || ShouldKeepAliveBeUsedInRequest(_previousRequest, _previousResponse))
			{
				// Try to use the cached credentials (if any, for the first request)
				request.Credentials = _credentialCache.GetCredentials(request.RequestUri);

				// If there are no cached credentials, use the default ones
				if (request.Credentials == null)
				{
					request.UseDefaultCredentials = true;
				}
			}
			else if (_previousStatusCode == HttpStatusCode.ProxyAuthenticationRequired)
			{
				request.Proxy.Credentials = _credentialProvider.GetCredentials(
					request, CredentialType.ProxyCredentials, retrying: _proxyCredentialsRetryCount > 0);
				_continueIfFailed = request.Proxy.Credentials != null;
				_proxyCredentialsRetryCount++;
			}
			else if (_previousStatusCode == HttpStatusCode.Unauthorized)
			{
				SetCredentialsOnAuthorizationError(request);
			}

			SetKeepAliveHeaders(request, _previousResponse);
			if (_usingSTSAuth)
			{
				// Add request headers if the server requires STS based auth.
				STSAuthHelper.PrepareSTSRequest(request);
			}

			// Wrap the credentials in a CredentialCache in case there is a redirect
			// and credentials need to be kept around.
			request.Credentials = request.Credentials.AsCredentialCache(request.RequestUri);
		}

		private void SetCredentialsOnAuthorizationError(HttpWebRequest request)
		{
			if (_usingSTSAuth)
			{
				// If we are using STS, the auth's being performed by a request header.
				// We do not need to ask the user for credentials at this point.
				return;
			}

			bool basicAuth = _previousResponse.AuthType != null &&
				_previousResponse.AuthType.IndexOf("Basic", StringComparison.OrdinalIgnoreCase) != -1;
			if (basicAuth && !_basicAuthIsUsedInPreviousRequest)
			{
				// The basic auth credentials were not sent in the last request.
				// We need to try with cached credentials in this request.
				request.Credentials = _credentialCache.GetCredentials(request.RequestUri);
			}

			if (request.Credentials == null)
			{
				request.Credentials = _credentialProvider.GetCredentials(
					request, CredentialType.RequestCredentials, retrying: _credentialsRetryCount > 0);
			}

			if (basicAuth)
			{
				// Add the Authorization header for basic authentication.
				NetworkCredential networkCredentials = (request.Credentials != null) ? request.Credentials.GetCredential(request.RequestUri, "Basic") : null;
				if (networkCredentials != null)
				{
					string authInfo = networkCredentials.UserName + ":" + networkCredentials.Password;
					authInfo = Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(authInfo));
					request.Headers["Authorization"] = "Basic " + authInfo;
				}
			}

			_continueIfFailed = request.Credentials != null;
			_credentialsRetryCount++;
		}

		private static IHttpWebResponse GetResponse(WebResponse response)
		{
			var httpWebResponse = response as IHttpWebResponse;
			if (httpWebResponse == null)
			{
				var webResponse = response as HttpWebResponse;
				if (webResponse == null)
				{
					return null;
				}
				return new HttpWebResponseWrapper(webResponse);
			}

			return httpWebResponse;
		}

		private static bool IsAuthenticationResponse(IHttpWebResponse response)
		{
			return response.StatusCode == HttpStatusCode.Unauthorized ||
				response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired;
		}

		private static void SetKeepAliveHeaders(HttpWebRequest request, IHttpWebResponse previousResponse)
		{
			// KeepAlive is required for NTLM and Kerberos authentication. If we've never been authenticated or are using a different auth, we
			// should not require KeepAlive.
			// REVIEW: The WWW-Authenticate header is tricky to parse so a Equals might not be correct.
			if (previousResponse == null ||
				!IsNtlmOrKerberos(previousResponse.AuthType))
			{
				// This is to work around the "The underlying connection was closed: An unexpected error occurred on a receive."
				// exception.
				request.KeepAlive = false;
				request.ProtocolVersion = HttpVersion.Version10;
			}
		}

		private static bool ShouldKeepAliveBeUsedInRequest(HttpWebRequest request, IHttpWebResponse response)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}

			if (response == null)
			{
				throw new ArgumentNullException("response");
			}

			return !request.KeepAlive && IsNtlmOrKerberos(response.AuthType);
		}

		private static bool IsNtlmOrKerberos(string authType)
		{
			if (String.IsNullOrEmpty(authType))
			{
				return false;
			}

			return authType.IndexOf("NTLM", StringComparison.OrdinalIgnoreCase) != -1
				|| authType.IndexOf("Kerberos", StringComparison.OrdinalIgnoreCase) != -1;
		}

		private class HttpWebResponseWrapper : IHttpWebResponse
		{
			private readonly HttpWebResponse _response;
			public HttpWebResponseWrapper(HttpWebResponse response)
			{
				_response = response;
			}

			public string AuthType
			{
				get
				{
					return _response.Headers[HttpResponseHeader.WwwAuthenticate];
				}
			}

			public HttpStatusCode StatusCode
			{
				get
				{
					return _response.StatusCode;
				}
			}

			public Uri ResponseUri
			{
				get
				{
					return _response.ResponseUri;
				}
			}

			public NameValueCollection Headers
			{
				get
				{
					return _response.Headers;
				}
			}

			public void Dispose()
			{
				if (_response != null)
				{
					_response.Close();
				}
			}
		}
	}
}
