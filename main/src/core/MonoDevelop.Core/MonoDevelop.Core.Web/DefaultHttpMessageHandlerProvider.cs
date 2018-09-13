// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// Based on: src/NuGet.Core/NuGet.Protocol/HttpSource/HttpHandlerResourceV3Provider.cs
// From: https://github.com/NuGet/NuGet.Client

using System;
using System.Net.Http;

namespace MonoDevelop.Core.Web
{
	public class DefaultHttpMessageHandlerProvider : HttpMessageHandlerProvider
	{
		public override HttpMessageHandler CreateHttpMessageHandler (Uri uri, HttpClientSettings settings)
		{
			var proxy = WebRequestHelper.ProxyCache.GetProxy (uri);
			var clientHandler = new DefaultHttpClientHandler (proxy, settings);

			HttpMessageHandler messageHandler = clientHandler;

			if (proxy != null) {
				messageHandler = new ProxyAuthenticationHandler (clientHandler, HttpClientProvider.CredentialService, WebRequestHelper.ProxyCache);
			}

			if (settings.SourceAuthenticationRequired) {
				var innerHandler = messageHandler;

				messageHandler = new HttpSourceAuthenticationHandler (uri, clientHandler, HttpClientProvider.CredentialService) {
					InnerHandler = innerHandler
				};
			}

			return messageHandler;
		}
	}
}
