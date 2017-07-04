// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using NuGet.Configuration;

namespace NuGet.Credentials
{
	/// <summary>
	/// This service manages orchestrates credential providers and supplies credentials
	/// for use in http requests
	/// </summary>
	class CredentialService : ICredentialService
	{
		readonly ConcurrentDictionary<string, bool> _retryCache
			= new ConcurrentDictionary<string, bool> ();
		readonly ConcurrentDictionary<string, CredentialResponse> _providerCredentialCache
			= new ConcurrentDictionary<string, CredentialResponse> ();
		readonly bool _nonInteractive;

		/// <summary>
		/// This semaphore ensures only one provider active per process, in order
		/// to prevent multiple concurrent interactive login dialogues.
		/// Unnamed semaphores are local to the current process.
		/// </summary>
		static readonly Semaphore ProviderSemaphore = new Semaphore (1, 1);

		Action<string> ErrorDelegate { get; }

		public bool HandlesDefaultCredentials { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="providers">All available credential providers.</param>
		/// <param name="nonInteractive">If true, the nonInteractive flag will be passed to providers.
		/// NonInteractive requests must not promt the user for credentials.</param>
		public CredentialService (IEnumerable<ICredentialProvider> providers, bool nonInteractive)
		{
			if (providers == null) {
				throw new ArgumentNullException (nameof (providers));
			}

			_nonInteractive = nonInteractive;
			Providers = new List<ICredentialProvider> (providers);
			HandlesDefaultCredentials = Providers.Any (provider => provider is DefaultCredentialsCredentialProvider);
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
		/// <param name="message">
		/// A default, user-readable message explaining why they are being prompted for credentials.
		/// The credential provider can choose to ignore this value and write their own message.
		/// </param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <returns>A credential object, or null if no credentials could be acquired.</returns>
		public async Task<ICredentials> GetCredentialsAsync (
			Uri uri,
			IWebProxy proxy,
			CredentialRequestType type,
			string message,
			CancellationToken cancellationToken)
		{
			if (uri == null) {
				throw new ArgumentNullException (nameof (uri));
			}

			ICredentials creds = null;

			foreach (var provider in Providers) {
				cancellationToken.ThrowIfCancellationRequested ();

				var retryKey = RetryCacheKey (uri, type, provider);
				var isRetry = _retryCache.ContainsKey (retryKey);

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
							message,
							isRetry,
							_nonInteractive,
							cancellationToken);

						// Check that the provider gave us a valid response.
						if (response == null || (response.Status != CredentialStatus.Success &&
							response.Status != CredentialStatus.ProviderNotApplicable &&
							response.Status != CredentialStatus.UserCanceled)) {
							throw new ProviderException (GettextCatalog.GetString ("Credential provider gave an invalid response."));
						}

						if (response.Status != CredentialStatus.UserCanceled) {
							AddToCredentialCache (uri, type, provider, response);
						}
					}

					if (response.Status == CredentialStatus.Success) {
						_retryCache [retryKey] = true;
						creds = response.Credentials;
						break;
					}
				} finally {
					ProviderSemaphore.Release ();
				}
			}

			return creds;
		}

		public CredentialService CreateNonInteractive ()
		{
			return new CredentialService (Providers, nonInteractive: true);
		}

		/// <summary>
		/// Attempts to retrieve last known good credentials for a URI from a credentials cache.
		/// </summary>
		/// <remarks>
		/// When the return value is <c>true</c>, <paramref name="credentials" /> will have last known
		/// good credentials from the credentials cache.  These credentials may have become invalid
		/// since their last use, so there is no guarantee that the credentials are currently valid.
		/// </remarks>
		/// <param name="uri">The URI for which cached credentials should be retrieved.</param>
		/// <param name="isProxy"><c>true</c> for proxy credentials; otherwise, <c>false</c>.</param>
		/// <param name="credentials">Cached credentials or <c>null</c>.</param>
		/// <returns><c>true</c> if a result is returned from the cache; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="uri" /> is <c>null</c>.</exception>
		public bool TryGetLastKnownGoodCredentialsFromCache (
			Uri uri,
			bool isProxy,
			out ICredentials credentials)
		{
			if (uri == null) {
				throw new ArgumentNullException (nameof (uri));
			}

			credentials = null;

			var rootUri = GetRootUri (uri);
			var ending = $"_{isProxy}_{rootUri}";

			foreach (var entry in _providerCredentialCache) {
				if (entry.Value.Status == CredentialStatus.Success && entry.Key.EndsWith (ending)) {
					credentials = entry.Value.Credentials;

					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets the currently configured providers.
		/// </summary>
		IEnumerable<ICredentialProvider> Providers { get; }

		bool TryFromCredentialCache (Uri uri, CredentialRequestType type, bool isRetry, ICredentialProvider provider,
			out CredentialResponse credentials)
		{
			credentials = null;

			var key = CredentialCacheKey (uri, type, provider);
			if (isRetry) {
				CredentialResponse removed;
				_providerCredentialCache.TryRemove (key, out removed);
				return false;
			}

			return _providerCredentialCache.TryGetValue (key, out credentials);
		}

		void AddToCredentialCache (Uri uri, CredentialRequestType type, ICredentialProvider provider,
			CredentialResponse credentials)
		{
			_providerCredentialCache [CredentialCacheKey (uri, type, provider)] = credentials;
		}

		static string RetryCacheKey (Uri uri, CredentialRequestType type, ICredentialProvider provider)
		{
			return GetUriKey (uri, type, provider);
		}

		static string CredentialCacheKey(Uri uri, CredentialRequestType type, ICredentialProvider provider)
		{
			var rootUri = GetRootUri (uri);
			return GetUriKey (rootUri, type, provider);
		}

		static Uri GetRootUri (Uri uri)
		{
			return new Uri (uri.GetComponents (UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
		}

		static string GetUriKey (Uri uri, CredentialRequestType type, ICredentialProvider provider)
		{
			return $"{provider.Id}_{type == CredentialRequestType.Proxy}_{uri}";
		}

		/// <summary>
		/// Clears the retry cache so credentials will be tried again from the key chain
		/// instead of defaulting to showing the credential dialog.
		/// </summary>
		public void Reset ()
		{
			_retryCache.Clear ();
		}
	}
}