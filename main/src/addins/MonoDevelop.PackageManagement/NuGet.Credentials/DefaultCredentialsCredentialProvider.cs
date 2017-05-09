// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;

namespace NuGet.Credentials
{
	/// <summary>
	/// A credential provider which supplies <see cref="CredentialCache.DefaultNetworkCredentials"/>
	/// </summary>
	class DefaultCredentialsCredentialProvider : ICredentialProvider
	{
		/// <summary>
		/// Unique identifier of this credential provider
		/// </summary>
		public string Id { get; } = $"{nameof(DefaultCredentialsCredentialProvider)}_{Guid.NewGuid ()}";

		/// <summary>
		/// Returns <see cref="CredentialCache.DefaultNetworkCredentials"/>, or 
		/// <see cref="CredentialStatus.ProviderNotApplicable"/> if this is a retry
		/// </summary>
		/// <remarks>
		/// If the flag to delay using default credentials until after plugin credential provicers run is not enabled,
		/// this always returns <see cref="CredentialStatus.ProviderNotApplicable"/>.
		/// </remarks>
		/// <param name="uri">Ignored.</param>
		/// <param name="proxy">Ignored.</param>
		/// <param name="type">Ignored.</param>
		/// <param name="message">Ignored.</param>
		/// <param name="isRetry">
		/// If true, returns <see cref="CredentialStatus.ProviderNotApplicable"/> instead of default credentials
		/// </param>
		/// <param name="nonInteractive">Ignored.</param>
		/// <param name="cancellationToken">Ignored.</param>
		public Task<CredentialResponse> GetAsync (
			Uri uri,
			IWebProxy proxy,
			CredentialRequestType type,
			string message,
			bool isRetry,
			bool nonInteractive,
			CancellationToken cancellationToken)
		{
			if (isRetry) {
				return Task.FromResult (new CredentialResponse (CredentialStatus.ProviderNotApplicable));
			}

			return Task.FromResult (
				new CredentialResponse (CredentialCache.DefaultNetworkCredentials));
		}
	}
}