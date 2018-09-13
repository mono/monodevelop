//
// HttpClientProvider.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Linq;
using System.Net.Http;
using Mono.Addins;

namespace MonoDevelop.Core.Web
{
	public static class HttpClientProvider
	{
		const string ProvidersPath = "/MonoDevelop/Core/HttpMessageHandlerProviders";

		static HttpMessageHandlerProvider httpMessageHandlerProvider;
		static CredentialService credentialService;
		static readonly HttpClientSettings defaultHttpClientSettings = new HttpClientSettings ();

		internal static void Initialize ()
		{
			httpMessageHandlerProvider = AddinManager.GetExtensionObjects<HttpMessageHandlerProvider> (ProvidersPath).FirstOrDefault ();

			if (httpMessageHandlerProvider == null) {
				httpMessageHandlerProvider = new DefaultHttpMessageHandlerProvider ();
			}

			credentialService = new CredentialService (new AsyncCredentialProvider ());
		}

		public static HttpClient CreateHttpClient (string uri)
		{
			return CreateHttpClient (new Uri (uri));
		}

		public static HttpClient CreateHttpClient (Uri uri)
		{
			return CreateHttpClient (uri, defaultHttpClientSettings);
		}

		public static HttpClient CreateHttpClient (string uri, HttpClientSettings settings)
		{
			return CreateHttpClient (new Uri (uri), settings);
		}

		public static HttpClient CreateHttpClient (Uri uri, HttpClientSettings settings)
		{
			var handler = CreateHttpMessageHandler (uri, settings);
			return new HttpClient (handler);
		}

		public static HttpMessageHandler CreateHttpMessageHandler (Uri uri, HttpClientSettings settings)
		{
			return httpMessageHandlerProvider.CreateHttpMessageHandler (uri, settings ?? defaultHttpClientSettings);
		}

		static internal ICredentialService CredentialService => credentialService;
	}
}
