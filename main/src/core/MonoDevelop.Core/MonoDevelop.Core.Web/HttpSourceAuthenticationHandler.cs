// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// From: https://github.com/NuGet/NuGet.Client

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Core.Web
{
	class HttpSourceAuthenticationHandler : DelegatingHandler
	{
		public static readonly int MaxAuthRetries = AmbientAuthenticationState.MaxAuthRetries;

		// Only one source may prompt at a time
		readonly static SemaphoreSlim credentialPromptLock = new SemaphoreSlim (1, 1);

		readonly Uri source;
		readonly HttpClientHandler clientHandler;
		readonly ICredentialService credentialService;

		readonly SemaphoreSlim httpClientLock = new SemaphoreSlim (1, 1);
		HttpSourceCredentials credentials;

		public HttpSourceAuthenticationHandler (
			Uri source,
			HttpClientHandler clientHandler,
			ICredentialService credentialService)
			: base (clientHandler)
		{
			this.source = source ?? throw new ArgumentNullException (nameof (source));
			this.clientHandler = clientHandler ?? throw new ArgumentNullException (nameof (clientHandler));

			// credential service is optional
			this.credentialService = credentialService;

			// Create a new wrapper for ICredentials that can be modified

			// This is used to match the value of HttpClientHandler.UseDefaultCredentials = true
			credentials = new HttpSourceCredentials (CredentialCache.DefaultNetworkCredentials);

			clientHandler.Credentials = credentials;
			// Always take the credentials from the helper.
			clientHandler.UseDefaultCredentials = false;
		}

		protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
		{
			HttpResponseMessage response = null;
			ICredentials promptCredentials = null;
			var authState = new AmbientAuthenticationState ();

			// Authorizing may take multiple attempts
			while (true) {
				// Clean up any previous responses
				if (response != null) {
					response.Dispose ();
				}

				// store the auth state before sending the request
				var beforeLockVersion = credentials.Version;

				response = await base.SendAsync (request, cancellationToken);

				if (credentialService == null) {
					return response;
				}

				if (response.StatusCode == HttpStatusCode.Unauthorized ||
					response.StatusCode == HttpStatusCode.Forbidden) {
					promptCredentials = await AcquireCredentialsAsync (
						authState,
						beforeLockVersion,
						cancellationToken);

					if (promptCredentials == null) {
						return response;
					}

					continue;
				}

				return response;
			}
		}

		async Task<ICredentials> AcquireCredentialsAsync (AmbientAuthenticationState authState, Guid credentialsVersion, CancellationToken cancellationToken)
		{
			try {
				// Only one request may prompt and attempt to auth at a time
				await httpClientLock.WaitAsync ();

				cancellationToken.ThrowIfCancellationRequested ();

				// Auth may have happened on another thread, if so just continue
				if (credentialsVersion != credentials.Version) {
					return credentials.Credentials;
				}

				if (authState.IsBlocked) {
					cancellationToken.ThrowIfCancellationRequested ();
					return null;
				}

				var promptCredentials = await PromptForCredentialsAsync (
					CredentialType.RequestCredentials,
					authState,
					cancellationToken);

				if (promptCredentials == null) {
					return null;
				}

				credentials.Credentials = promptCredentials;

				return promptCredentials;
			} finally {
				httpClientLock.Release ();
			}
		}

		async Task<ICredentials> PromptForCredentialsAsync (
			CredentialType type,
			AmbientAuthenticationState authState,
			CancellationToken token)
		{
			ICredentials promptCredentials;

			try {
				// Only one prompt may display at a time.
				await credentialPromptLock.WaitAsync ();

				// Get the proxy for this URI so we can pass it to the credentialService methods
				// this lets them use the proxy if they have to hit the network.
				var proxyCache = WebRequestHelper.ProxyCache;
				var proxy = proxyCache?.GetProxy (source);

				promptCredentials = await credentialService
					.GetCredentialsAsync (source, proxy, type, token);

				if (promptCredentials == null) {
					// If this is the case, this means none of the credential providers were able to
					// handle the credential request or no credentials were available for the
					// endpoint.
					authState.Block ();
				} else {
					authState.Increment ();
				}
			} catch (OperationCanceledException) {
				// This indicates a non-human cancellation.
				throw;
			} catch (Exception) {
				// If this is the case, this means there was a fatal exception when interacting
				// with the credential service (or its underlying credential providers). Either way,
				// block asking for credentials for the live of this operation.
				promptCredentials = null;
				authState.Block ();
			} finally {
				credentialPromptLock.Release ();
			}

			return promptCredentials;
		}
	}
}