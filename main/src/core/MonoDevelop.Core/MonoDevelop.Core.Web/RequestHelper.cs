using System;
using System.Collections.Specialized;
using System.Net;
using MonoDevelop.Core.Web;

namespace Xamarin.Components.Ide.Web
{
	public static class RequestHelper
	{
		/// <summary>
		/// Keeps sending requests until a response code that doesn't require authentication happens or if
		/// the request requires authentication and the user has stopped trying to enter them (i.e. they hit cancel when they are prompted).
		/// </summary>
		internal static WebResponse GetResponse (Func<WebRequest> createRequest, Action<WebRequest> prepareRequest)
		{
			var proxyCache = WebService.Instance.ProxyCache;
			var credentialCache = WebService.Instance.CredentialCache;
			var credentialProvider = WebService.Instance.CredentialProvider;

			HttpWebRequest previousRequest = null;
			IHttpWebResponse previousResponse = null;
			HttpStatusCode? previousStatusCode = null;
			var continueIfFailed = true;
			int proxyCredentialsRetryCount = 0, credentialsRetryCount = 0;

			while (true) {
				// Create the request
				var request = (HttpWebRequest)createRequest ();
				request.Proxy = proxyCache.GetProxy (request.RequestUri);

				if (request.Proxy != null && request.Proxy.Credentials == null) {
					var proxyAddress = ((WebProxy)request.Proxy).Address;
					request.Proxy.Credentials = credentialCache.GetCredentials (proxyAddress, CredentialType.ProxyCredentials) ??
						CredentialCache.DefaultCredentials;
				}

				var retrying = proxyCredentialsRetryCount > 0;
				ICredentials oldCredentials;
				if (!previousStatusCode.HasValue && (previousResponse == null || ShouldKeepAliveBeUsedInRequest (previousRequest, previousResponse))) {
					// Try to use the cached credentials (if any, for the first request)
					request.Credentials = credentialCache.GetCredentials (request.RequestUri, CredentialType.RequestCredentials);

					// If there are no cached credentials, use the default ones
					if (request.Credentials == null)
						request.UseDefaultCredentials = true;
				} else if (previousStatusCode == HttpStatusCode.ProxyAuthenticationRequired) {
					oldCredentials = previousRequest != null && previousRequest.Proxy != null ? previousRequest.Proxy.Credentials : null;
					request.Proxy.Credentials = credentialProvider.GetCredentials (request, oldCredentials, CredentialType.ProxyCredentials, retrying: retrying);
					continueIfFailed = request.Proxy.Credentials != null;
					proxyCredentialsRetryCount++;
				} else if (previousStatusCode == HttpStatusCode.Unauthorized) {
					oldCredentials = previousRequest != null ? previousRequest.Credentials : null;
					request.Credentials = credentialProvider.GetCredentials (request, oldCredentials, CredentialType.RequestCredentials, retrying: retrying);
					continueIfFailed = request.Credentials != null;
					credentialsRetryCount++;
				}

				try {
					ICredentials credentials = request.Credentials;

					SetKeepAliveHeaders (request, previousResponse);

					// Prepare the request, we do something like write to the request stream
					// which needs to happen last before the request goes out
					prepareRequest (request);

					// Wrap the credentials in a CredentialCache in case there is a redirect
					// and credentials need to be kept around.
					request.Credentials = request.Credentials.AsCredentialCache (request.RequestUri);

					var response = request.GetResponse ();

					// Cache the proxy and credentials
					if (request.Proxy != null) {
						proxyCache.Add (request.Proxy);
						credentialCache.Add (((WebProxy)request.Proxy).Address, request.Proxy.Credentials, CredentialType.ProxyCredentials);
					}

					credentialCache.Add (request.RequestUri, credentials, CredentialType.RequestCredentials);
					credentialCache.Add (response.ResponseUri, credentials, CredentialType.RequestCredentials);

					return response;
				} catch (WebException ex) {
					using (var response = GetResponse (ex.Response)) {
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
							proxyCache.Add (request.Proxy);
							credentialCache.Add (((WebProxy)request.Proxy).Address, request.Proxy.Credentials, CredentialType.ProxyCredentials);
						} else if (previousStatusCode == HttpStatusCode.Unauthorized && response.StatusCode != HttpStatusCode.Unauthorized) {
							credentialCache.Add (request.RequestUri, request.Credentials, CredentialType.RequestCredentials);
							credentialCache.Add (response.ResponseUri, request.Credentials, CredentialType.RequestCredentials);
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

		static IHttpWebResponse GetResponse (WebResponse response)
		{
			var httpWebResponse = response as IHttpWebResponse;
			if (httpWebResponse != null)
				return httpWebResponse;

			var webResponse = response as HttpWebResponse;
			return webResponse == null ? null : new HttpWebResponseWrapper (webResponse);
		}

		static bool IsAuthenticationResponse (IHttpWebResponse response)
		{
			return response.StatusCode == HttpStatusCode.Unauthorized ||
				response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired;
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

		private class HttpWebResponseWrapper : IHttpWebResponse
		{
			readonly HttpWebResponse response;

			public HttpWebResponseWrapper (HttpWebResponse response)
			{
				this.response = response;
			}

			public string AuthType
			{
				get
				{
					return response.Headers[HttpResponseHeader.WwwAuthenticate];
				}
			}

			public HttpStatusCode StatusCode
			{
				get
				{
					return response.StatusCode;
				}
			}

			public Uri ResponseUri
			{
				get
				{
					return response.ResponseUri;
				}
			}

			public NameValueCollection Headers
			{
				get
				{
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
	}
}
