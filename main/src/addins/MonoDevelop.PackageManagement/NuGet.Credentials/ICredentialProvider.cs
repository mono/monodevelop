// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;

namespace NuGet.Credentials
{
	interface ICredentialProvider
	{
		string Id { get; }

		Task<CredentialResponse> GetAsync (
			Uri uri, 
			IWebProxy proxy, 
			CredentialRequestType type,
			string message,
			bool isRetry,
			bool nonInteractive, 
			CancellationToken cancellationToken);
	}
}