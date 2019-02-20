// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// From: https://github.com/NuGet/NuGet.Client

using System;
using System.Net;

namespace MonoDevelop.Core.Web
{
	/// <summary>
	/// A mutable CredentialCache wrapper. This allows the underlying ICredentials to
	/// be changed to work around HttpClientHandler not allowing Credentials to change.
	/// This class intentionally inherits from CredentialCache to support authentication on redirects.
	/// According to System.Net implementation any other ICredentials implementation is dropped for security reasons.
	/// </summary>
	class HttpSourceCredentials : CredentialCache, ICredentials
	{
		public HttpSourceCredentials ()
		{
			Credentials = null;
		}

		/// <summary>
		/// Credentials can be changed by other threads, for this reason volatile
		/// is added below so that the value is not cached anywhere.
		/// </summary>
		volatile VersionedCredentials credentials;

		/// <summary>
		/// The latest credentials to be used.
		/// </summary>
		public ICredentials Credentials {
			get {
				return credentials.Credentials;
			}

			set {
				// We must update the credentials and it's associated version GUID atomically. This
				// can be achieved with a reference assignment. It is important that credentials and
				// version always match. In other words, if the credentials are updated, it should
				// at no instant be possible to get old version GUID and the new credentials.
				credentials = new VersionedCredentials (value);
			}
		}

		/// <summary>
		/// The latest version ID of the <see cref="Credentials"/>.
		/// </summary>
		public Guid Version {
			get {
				return credentials.Version;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpSourceCredentials"/> class
		/// </summary>
		/// <param name="credentials">
		/// Optional initial credentials. May be null.
		/// </param>
		public HttpSourceCredentials (ICredentials credentials = null)
		{
			Credentials = credentials;
		}

		/// <summary>
		/// Used by the HttpClientHandler to retrieve the current credentials.
		/// </summary>
		NetworkCredential ICredentials.GetCredential (Uri uri, string authType)
		{
			// Get credentials from the current credential store, if any
			return Credentials?.GetCredential (uri, authType);
		}

		class VersionedCredentials
		{
			public VersionedCredentials (ICredentials credentials)
			{
				Version = Guid.NewGuid ();
				Credentials = credentials;
			}

			public ICredentials Credentials { get; }
			public Guid Version { get; }
		}
	}
}