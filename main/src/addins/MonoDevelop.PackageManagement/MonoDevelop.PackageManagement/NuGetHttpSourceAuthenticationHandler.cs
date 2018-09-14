// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// Based on HttpSourceAuthenticationHandler
// From: https://github.com/NuGet/NuGet.Client/

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core.Web;
using NuGet.Common;
using NuGet.Configuration;

namespace NuGet.Protocol
{
	class NuGetHttpSourceAuthenticationHandler : DelegatingHandler
	{
		public static readonly int MaxAuthRetries = 4;

		// Only one source may prompt at a time
		readonly static SemaphoreSlim credentialPromptLock = new SemaphoreSlim (1, 1);

		readonly PackageSource packageSource;
		readonly IHttpCredentialsHandler credentialsHandler;
		readonly ICredentialService credentialService;

		readonly SemaphoreSlim httpClientLock = new SemaphoreSlim (1, 1);
		Dictionary<string, AmbientAuthenticationState> authStates = new Dictionary<string, AmbientAuthenticationState> ();
		HttpSourceCredentials credentials;

		public NuGetHttpSourceAuthenticationHandler (
			PackageSource packageSource,
			IHttpCredentialsHandler credentialsHandler,
			ICredentialService credentialService)
		{
			if (packageSource == null) {
				throw new ArgumentNullException (nameof (packageSource));
			}

			this.packageSource = packageSource;

			if (credentialsHandler == null) {
				throw new ArgumentNullException (nameof (credentialsHandler));
			}

			this.credentialsHandler = credentialsHandler;

			// credential service is optional as credentials may be attached to a package source
			this.credentialService = credentialService;

			// Create a new wrapper for ICredentials that can be modified

			if (credentialService == null || !credentialService.HandlesDefaultCredentials) {
				// This is used to match the value of HttpClientHandler.UseDefaultCredentials = true
				credentials = new HttpSourceCredentials (CredentialCache.DefaultNetworkCredentials);
			} else {
				credentials = new HttpSourceCredentials ();
			}

			if (packageSource.Credentials != null &&
				packageSource.Credentials.IsValid ()) {
				var sourceCredentials = new NetworkCredential (packageSource.Credentials.Username, packageSource.Credentials.Password);
				credentials.Credentials = sourceCredentials;
			}

			this.credentialsHandler.Credentials = credentials;
			// Always take the credentials from the helper.
			this.credentialsHandler.UseDefaultCredentials = false;
		}

		protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
		{
			HttpResponseMessage response = null;
			ICredentials promptCredentials = null;

			var configuration = request.GetOrCreateConfiguration ();

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
					(configuration.PromptOn403 && response.StatusCode == HttpStatusCode.Forbidden)) {
					promptCredentials = await AcquireCredentialsAsync (
						response.StatusCode,
						beforeLockVersion,
						configuration.Logger,
						cancellationToken);

					if (promptCredentials == null) {
						return response;
					}

					continue;
				}

				if (promptCredentials != null) {
					CredentialsSuccessfullyUsed (packageSource.SourceUri, promptCredentials);
				}

				return response;
			}
		}

		private async Task<ICredentials> AcquireCredentialsAsync (HttpStatusCode statusCode, Guid credentialsVersion, ILogger log, CancellationToken cancellationToken)
		{
			try {
				// Only one request may prompt and attempt to auth at a time
				await httpClientLock.WaitAsync ();

				cancellationToken.ThrowIfCancellationRequested ();

				// Auth may have happened on another thread, if so just continue
				if (credentialsVersion != credentials.Version) {
					return credentials.Credentials;
				}

				var authState = GetAuthenticationState ();

				if (authState.IsBlocked) {
					cancellationToken.ThrowIfCancellationRequested ();

					return null;
				}

				// Construct a reasonable message for the prompt to use.
				CredentialRequestType type;
				if (statusCode == HttpStatusCode.Unauthorized) {
					type = CredentialRequestType.Unauthorized;
				} else {
					type = CredentialRequestType.Forbidden;
				}

				var promptCredentials = await PromptForCredentialsAsync (
					type,
					authState,
					log,
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

		AmbientAuthenticationState GetAuthenticationState ()
		{
			var correlationId = ActivityCorrelationId.Current;

			AmbientAuthenticationState authState;
			if (!authStates.TryGetValue (correlationId, out authState)) {
				authState = new AmbientAuthenticationState ();
				authStates [correlationId] = authState;
			}

			return authState;
		}

		async Task<ICredentials> PromptForCredentialsAsync (
			CredentialRequestType type,
			AmbientAuthenticationState authState,
			ILogger log,
			CancellationToken token)
		{
			ICredentials promptCredentials;

			try {
				// Only one prompt may display at a time.
				await credentialPromptLock.WaitAsync ();

				// Get the proxy for this URI so we can pass it to the credentialService methods
				// this lets them use the proxy if they have to hit the network.
				var proxyCache = ProxyCache.Instance;
				var proxy = proxyCache?.GetProxy (packageSource.SourceUri);

				promptCredentials = await credentialService
					.GetCredentialsAsync (packageSource.SourceUri, proxy, type, string.Empty, token);

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
			} catch (Exception e) {
				// If this is the case, this means there was a fatal exception when interacting
				// with the credential service (or its underlying credential providers). Either way,
				// block asking for credentials for the live of this operation.
				log.LogError (ExceptionUtilities.DisplayMessage (e));
				promptCredentials = null;
				authState.Block ();
			} finally {
				credentialPromptLock.Release ();
			}

			return promptCredentials;
		}

		void CredentialsSuccessfullyUsed (Uri uri, ICredentials usedCredentials)
		{
			HttpHandlerResourceV3.CredentialsSuccessfullyUsed?.Invoke (uri, usedCredentials);
		}
	}
}