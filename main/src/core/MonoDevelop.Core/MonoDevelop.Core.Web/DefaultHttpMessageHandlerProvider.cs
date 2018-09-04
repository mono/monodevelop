// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// Based on: src/NuGet.Core/NuGet.Protocol/HttpSource/HttpHandlerResourceV3Provider.cs
// From: https://github.com/NuGet/NuGet.Client

using System;
using System.Net;
using System.Net.Http;

namespace MonoDevelop.Core.Web
{
	class DefaultHttpMessageHandlerProvider : HttpMessageHandlerProvider
	{
		public override HttpMessageHandler CreateHttpMessageHandler (Uri uri)
		{
			var proxy = WebRequestHelper.ProxyCache.GetProxy (uri);

			var clientHandler = new HttpClientHandler {
				AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
				Proxy = proxy
			};

			HttpMessageHandler messageHandler = clientHandler;

			if (proxy != null) {
				messageHandler = new ProxyAuthenticationHandler (clientHandler, HttpClientProvider.CredentialService, WebRequestHelper.ProxyCache);
			}

			var innerHandler = messageHandler;

			messageHandler = new HttpSourceAuthenticationHandler (uri, clientHandler, HttpClientProvider.CredentialService) {
				InnerHandler = innerHandler
			};

			return messageHandler;
		}
	}
}
