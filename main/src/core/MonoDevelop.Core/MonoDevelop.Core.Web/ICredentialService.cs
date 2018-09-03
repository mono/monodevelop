// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// From: https://github.com/NuGet/NuGet.Client

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Core.Web
{
	interface ICredentialService
	{
		/// <summary>
		/// Asynchronously gets credentials.
		/// </summary>
		/// <param name="uri">The URI for which credentials should be retrieved.</param>
		/// <param name="proxy">A web proxy.</param>
		/// <param name="type">The credential request type.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result (<see cref="Task{TResult}.Result" />) returns a <see cref="ICredentials" />.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="uri" /> is <c>null</c>.</exception>
		/// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken" />
		/// is cancelled.</exception>
		Task<ICredentials> GetCredentialsAsync (
			Uri uri,
			IWebProxy proxy,
			CredentialType type,
			CancellationToken cancellationToken);
	}
}
