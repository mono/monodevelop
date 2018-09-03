// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// From: https://github.com/NuGet/NuGet.Client

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Core.Web
{
	/// <summary>
	/// A message handler responsible for retrying request for authenticated proxies
	/// with missing credentials.
	/// </summary>
	class ProxyAuthenticationHandler : DelegatingHandler
	{
		public static readonly int MaxAuthRetries = 3;
		const string BasicAuthenticationType = "Basic";

		// Only one source may prompt at a time
		static readonly SemaphoreSlim credentialPromptLock = new SemaphoreSlim (1, 1);

		readonly HttpClientHandler clientHandler;
		readonly ICredentialService credentialService;
		readonly IProxyCredentialCache credentialCache;

		int _authRetries;

		public ProxyAuthenticationHandler (
			HttpClientHandler clientHandler,
			ICredentialService credentialService,
			IProxyCredentialCache credentialCache)
			: base (clientHandler)
		{
			if (clientHandler == null) {
				throw new ArgumentNullException (nameof (clientHandler));
			}

			this.clientHandler = clientHandler;

			// credential service is optional
			this.credentialService = credentialService;

			if (credentialCache == null) {
				throw new ArgumentNullException (nameof (credentialCache));
			}

			this.credentialCache = credentialCache;
		}

		protected override async Task<HttpResponseMessage> SendAsync (
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			while (true) {
				// Store the auth start before sending the request
				var cacheVersion = credentialCache.Version;

				try {
					var response = await base.SendAsync (request, cancellationToken);

					if (response.StatusCode != HttpStatusCode.ProxyAuthenticationRequired) {
						return response;
					}

					if (clientHandler.Proxy == null) {
						return response;
					}

					if (credentialService == null) {
						return response;
					}

					if (!await AcquireCredentialsAsync (request.RequestUri, cacheVersion, cancellationToken)) {
						return response;
					}
				} catch (Exception ex)
				  when (ProxyAuthenticationRequired (ex) && clientHandler.Proxy != null && credentialService != null) {
					if (!await AcquireCredentialsAsync (request.RequestUri, cacheVersion, cancellationToken)) {
						throw;
					}
				}
			}
		}

		// Returns true if the cause of the exception is proxy authentication failure
		static bool ProxyAuthenticationRequired (Exception ex)
		{
			// HACK!!! : This is a hack to workaround Xamarin Bug 19594
			var webException = ex as WebException;
			if (!Platform.IsWindows && webException != null) {
				return IsMonoProxyAuthenticationRequiredError (webException);
			}

			var response = ExtractResponse (ex);
			return response?.StatusCode == HttpStatusCode.ProxyAuthenticationRequired;
		}

		static HttpWebResponse ExtractResponse (Exception ex)
		{
			var webException = ex.InnerException as WebException;
			var response = webException?.Response as HttpWebResponse;
			return response;
		}

		static bool IsMonoProxyAuthenticationRequiredError (WebException ex)
		{
			return ex.Status == WebExceptionStatus.SecureChannelFailure &&
				ex.Message != null &&
				ex.Message.Contains ("The remote server returned a 407 status code.");
		}

		async Task<bool> AcquireCredentialsAsync (Uri requestUri, Guid cacheVersion, CancellationToken cancellationToken)
		{
			try {
				await credentialPromptLock.WaitAsync ();

				cancellationToken.ThrowIfCancellationRequested ();

				// Check if the credentials have already changed
				if (cacheVersion != credentialCache.Version) {
					// retry the request with updated credentials
					return true;
				}

				// Limit the number of retries
				_authRetries++;
				if (_authRetries >= MaxAuthRetries) {
					// user prompting no more
					return false;
				}

				var proxyAddress = clientHandler.Proxy.GetProxy (requestUri);

				// prompt user for proxy credentials.
				var credentials = await PromptForProxyCredentialsAsync (proxyAddress, clientHandler.Proxy, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested ();

				if (credentials == null) {
					// user cancelled or error occured
					return false;
				}

				credentialCache.UpdateCredential (proxyAddress, credentials);

				// use the user provided credential to send the request again.
				return true;
			} finally {
				credentialPromptLock.Release ();
			}
		}

		async Task<NetworkCredential> PromptForProxyCredentialsAsync (Uri proxyAddress, IWebProxy proxy, CancellationToken cancellationToken)
		{
			ICredentials promptCredentials;

			try {
				promptCredentials = await credentialService.GetCredentialsAsync (
					proxyAddress,
					proxy,
					type: CredentialType.ProxyCredentials,
					cancellationToken: cancellationToken);
			} catch (OperationCanceledException) {
				throw; // pass-thru
			} catch (Exception ex) {
				// Fatal credential service failure
				LoggingService.LogError ("PromptForProxyCredentialsAsync error", ex);
				promptCredentials = null;
			}

			return promptCredentials?.GetCredential (proxyAddress, BasicAuthenticationType);
		}
	}
}
