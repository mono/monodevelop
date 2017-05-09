// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Credentials;

namespace NuGet.CommandLine
{
	class SettingsCredentialProvider : NuGet.Credentials.ICredentialProvider
	{
		readonly Configuration.IPackageSourceProvider _packageSourceProvider;

		public SettingsCredentialProvider (Configuration.IPackageSourceProvider packageSourceProvider)
		{
			if (packageSourceProvider == null) {
				throw new ArgumentNullException (nameof (packageSourceProvider));
			}

			_packageSourceProvider = packageSourceProvider;
		}

		public string Id {
			get { return "SettingsCredentialProvider"; }
		}

		public Task<CredentialResponse> GetAsync (
			Uri uri, 
			IWebProxy proxy, 
			CredentialRequestType type,
			string message,
			bool isRetry,
			bool nonInteractive, 
			CancellationToken cancellationToken)
		{
			if (uri == null) {
				throw new ArgumentNullException (nameof(uri));
			}

			cancellationToken.ThrowIfCancellationRequested ();

			var cred = GetCredentials (
				uri,
				proxy,
				type,
				isRetry);

			var response = cred != null
				? new CredentialResponse (cred)
				: new CredentialResponse (CredentialStatus.ProviderNotApplicable);

			return Task.FromResult (response);
		}

		ICredentials GetCredentials (Uri uri, IWebProxy proxy, CredentialRequestType credentialType, bool retrying)
		{
			NetworkCredential credentials;
			// If we are retrying, the stored credentials must be invalid.
			if (!retrying && (credentialType != CredentialRequestType.Proxy) && TryGetCredentials (uri, out credentials)) {
				return credentials;
			}
			return null;
		}

		bool TryGetCredentials (Uri uri, out NetworkCredential configurationCredentials)
		{
			var source = _packageSourceProvider.LoadPackageSources ().FirstOrDefault (p => {
				Uri sourceUri;
				return p.Credentials != null
					&& p.Credentials.IsValid ()
					&& Uri.TryCreate (p.Source, UriKind.Absolute, out sourceUri)
					&& UriEquals (sourceUri, uri);
			});
			if (source == null) {
				// The source is not in the config file
				configurationCredentials = null;
				return false;
			}
			configurationCredentials = new NetworkCredential (source.Credentials.Username, source.Credentials.Password);
			return true;
		}

		/// <summary>
		/// Determines if the scheme, server and path of two Uris are identical.
		/// </summary>
		static bool UriEquals (Uri uri1, Uri uri2)
		{
			uri1 = CreateODataAgnosticUri (uri1.OriginalString.TrimEnd ('/'));
			uri2 = CreateODataAgnosticUri (uri2.OriginalString.TrimEnd ('/'));

			return Uri.Compare (uri1, uri2, UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;
		}

		// Bug 2379: SettingsCredentialProvider does not work
		static Uri CreateODataAgnosticUri (string uri)
		{
			if (uri.EndsWith ("$metadata", StringComparison.OrdinalIgnoreCase)) {
				uri = uri.Substring (0, uri.Length - 9).TrimEnd ('/');
			}
			return new Uri (uri);
		}
	}
}