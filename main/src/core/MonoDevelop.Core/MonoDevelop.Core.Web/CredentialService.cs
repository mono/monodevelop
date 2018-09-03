// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// From: https://github.com/NuGet/NuGet.Client

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Core.Web
{
	class CredentialService : ICredentialService
	{
		readonly ConcurrentDictionary<string, bool> retryCache
			= new ConcurrentDictionary<string, bool> ();
		readonly ConcurrentDictionary<string, CredentialResponse> providerCredentialCache
			= new ConcurrentDictionary<string, CredentialResponse> ();

		/// <summary>
		/// This semaphore ensures only one provider active per process, in order
		/// to prevent multiple concurrent interactive login dialogues.
		/// Unnamed semaphores are local to the current process.
		/// </summary>
		static readonly Semaphore ProviderSemaphore = new Semaphore (1, 1);

		public CredentialService (IAsyncCredentialProvider provider)
		{
			providers = new [] { provider };
		}

		/// <summary>
		/// Provides credentials for http requests.
		/// </summary>
		/// <param name="uri">
		/// The URI of a web resource for which credentials are needed.
		/// </param>
		/// <param name="proxy">
		/// The currently configured proxy. It may be necessary for CredentialProviders
		/// to use this proxy in order to acquire credentials from their authentication source.
		/// </param>
		/// <param name="type">
		/// The type of credential request that is being made.
		/// </param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <returns>A credential object, or null if no credentials could be acquired.</returns>
		public async Task<ICredentials> GetCredentialsAsync (
			Uri uri,
			IWebProxy proxy,
			CredentialType type,
			CancellationToken cancellationToken)
		{
			if (uri == null) {
				throw new ArgumentNullException (nameof (uri));
			}

			ICredentials creds = null;

			foreach (var provider in providers) {
				cancellationToken.ThrowIfCancellationRequested ();

				var retryKey = RetryCacheKey (uri, type, provider);
				var isRetry = retryCache.ContainsKey (retryKey);

				try {
					// This local semaphore ensures one provider active per process.
					// We can consider other ways to allow more concurrency between providers, but need to
					// ensure that only one interactive dialogue is ever presented at a time, and that
					// providers are not writing shared resources.
					// Since this service is called only when cached credentials are not available to the caller,
					// such an optimization is likely not necessary.
					ProviderSemaphore.WaitOne ();

					CredentialResponse response;
					if (!TryFromCredentialCache (uri, type, isRetry, provider, out response)) {
						response = await provider.GetAsync (
							uri,
							proxy,
							type,
							isRetry,
							cancellationToken);

						// Check that the provider gave us a valid response.
						if (response == null || (response.Status != CredentialStatus.Success &&
												 response.Status != CredentialStatus.ProviderNotApplicable &&
												 response.Status != CredentialStatus.UserCanceled)) {
							throw new InvalidOperationException ("Credential provider gave an invalid response.");
						}

						if (response.Status != CredentialStatus.UserCanceled) {
							AddToCredentialCache (uri, type, provider, response);
						}
					}

					if (response.Status == CredentialStatus.Success) {
						retryCache [retryKey] = true;
						creds = response.Credentials;
						break;
					}
				} finally {
					ProviderSemaphore.Release ();
				}
			}

			return creds;
		}

		/// <summary>
		/// Gets the currently configured providers.
		/// </summary>
		IEnumerable<IAsyncCredentialProvider> providers { get; }

		bool TryFromCredentialCache (Uri uri, CredentialType type, bool isRetry, IAsyncCredentialProvider provider,
			out CredentialResponse credentials)
		{
			credentials = null;

			var key = CredentialCacheKey (uri, type, provider);
			if (isRetry) {
				CredentialResponse removed;
				providerCredentialCache.TryRemove (key, out removed);
				return false;
			}

			return providerCredentialCache.TryGetValue (key, out credentials);
		}

		void AddToCredentialCache (Uri uri, CredentialType type, IAsyncCredentialProvider provider,
			CredentialResponse credentials)
		{
			providerCredentialCache [CredentialCacheKey (uri, type, provider)] = credentials;
		}

		static string RetryCacheKey (Uri uri, CredentialType type, IAsyncCredentialProvider provider)
		{
			return GetUriKey (uri, type, provider);
		}

		static string CredentialCacheKey (Uri uri, CredentialType type, IAsyncCredentialProvider provider)
		{
			var rootUri = GetRootUri (uri);
			return GetUriKey (rootUri, type, provider);
		}

		static Uri GetRootUri (Uri uri)
		{
			return new Uri (uri.GetComponents (UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
		}

		static string GetUriKey (Uri uri, CredentialType type, IAsyncCredentialProvider provider)
		{
			return $"{provider.Id}_{type == CredentialType.ProxyCredentials}_{uri}";
		}
	}
}
