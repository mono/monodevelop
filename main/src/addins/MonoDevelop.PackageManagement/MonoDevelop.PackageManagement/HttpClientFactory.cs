//
// HttpClientFactory.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Net;
using System.Net.Http;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Credentials;

namespace MonoDevelop.PackageManagement
{
	static class HttpClientFactory
	{
		static public HttpClient CreateHttpClient (Uri uri, ICredentialService credentialService)
		{
			return CreateHttpClient (new PackageSource (uri.ToString ()), credentialService);
		}

		static public HttpClient CreateHttpClient (PackageSource packageSource, ICredentialService credentialService)
		{
			var httpClient = new HttpClient (CreateHttpMessageHandler (packageSource, credentialService));
			UserAgent.SetUserAgent (httpClient);

			return httpClient;
		}

		static HttpMessageHandler CreateHttpMessageHandler (PackageSource packageSource, ICredentialService credentialService)
		{
			var proxy = ProxyCache.Instance.GetProxy (packageSource.SourceUri);

			var clientHandler = new HttpClientHandler {
				Proxy = proxy,
				AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate)
			};

			HttpMessageHandler messageHandler = clientHandler;

			if (proxy != null) {
				messageHandler = new ProxyAuthenticationHandler (clientHandler, credentialService, ProxyCache.Instance);
			}

			HttpMessageHandler innerHandler = messageHandler;
			messageHandler = new StsAuthenticationHandler (packageSource, TokenStore.Instance) {
				InnerHandler = messageHandler
			};

			innerHandler = messageHandler;

			messageHandler = new HttpSourceAuthenticationHandler (packageSource, clientHandler, credentialService) {
				InnerHandler = innerHandler
			};

			return messageHandler;
		}

		public static ICredentialService CreateNonInteractiveCredentialService ()
		{
			var existingCredentialService = HttpHandlerResourceV3.CredentialService as CredentialService;
			if (existingCredentialService != null) {
				return existingCredentialService.CreateNonInteractive ();
			}
			return new CredentialService (new ICredentialProvider[0], nonInteractive: true);
		}
	}
}
