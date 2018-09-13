// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// Based on ServerWarningLogHandler.
// From: https://github.com/NuGet/NuGet.Client/From: https://github.com/NuGet/NuGet.Client/

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol;

namespace MonoDevelop.PackageManagement
{
	class MonoDevelopServerWarningLogHandler : DelegatingHandler
	{
		public MonoDevelopServerWarningLogHandler (HttpMessageHandler innerHandler)
			: base (innerHandler)
		{
		}

		protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var configuration = request.GetOrCreateConfiguration ();

			var response = await base.SendAsync (request, cancellationToken);

			response.LogServerWarning (configuration.Logger);

			return response;
		}
	}
}
