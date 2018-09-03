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

			var handler = new HttpClientHandler {
				AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
				Proxy = proxy
			};

			HttpMessageHandler messageHandler = handler;

			if (proxy != null) {
				messageHandler = new ProxyAuthenticationHandler (handler, HttpClientProvider.CredentialService, WebRequestHelper.ProxyCache);
			}

			return messageHandler;
		}
	}
}
