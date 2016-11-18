// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using NuGet.Configuration;

namespace NuGet.Credentials
{
	/// <summary>
	/// Wraps a CredentialService to match the older v2 NuGet.ICredentialProvider interface
	/// </summary>
	public class CredentialServiceAdapter : NuGet.ICredentialProvider
	{
		private readonly CredentialService _credentialService;

		public CredentialServiceAdapter (CredentialService service)
		{
			if (service == null) {
				throw new ArgumentNullException(nameof(service));
			}

			_credentialService = service;
		}

		public ICredentials GetCredentials (Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
		{
			if (uri == null) {
				throw new ArgumentNullException(nameof(uri));
			}

			var type = credentialType == CredentialType.ProxyCredentials ?
				CredentialRequestType.Proxy : CredentialRequestType.Unauthorized;

			var task = _credentialService.GetCredentialsAsync (
				uri,
				proxy,
				type,
				message: null,
				cancellationToken: CancellationToken.None);

			return task.Result;
		}
	}
}