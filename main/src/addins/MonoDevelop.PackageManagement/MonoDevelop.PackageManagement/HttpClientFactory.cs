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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

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
			SetAcceptLanguage (httpClient);

			return httpClient;
		}

		static void SetAcceptLanguage (HttpClient httpClient)
		{
			string acceptLanguage = CultureInfo.CurrentUICulture.ToString ();
			if (!string.IsNullOrEmpty (acceptLanguage)) {
				httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd (acceptLanguage);
			}
		}

		static HttpMessageHandler CreateHttpMessageHandler (PackageSource packageSource, ICredentialService credentialService)
		{
			HttpHandlerResourceV3 resource = MonoDevelopHttpHandlerResourceV3Provider.CreateResource (packageSource, credentialService, nonInteractive: true);
			return resource.MessageHandler;
		}

		public static ICredentialService CreateNonInteractiveCredentialService ()
		{
			var existingCredentialService = HttpHandlerResourceV3Extensions.GetCustomCredentialService ();
			if (existingCredentialService != null) {
				return existingCredentialService.CreateNonInteractive ();
			}
			var lazyProvider = AsyncLazy.New (() => Enumerable.Empty<ICredentialProvider> ());
			return new CredentialService (lazyProvider, nonInteractive: true, handlesDefaultCredentials: false);
		}
	}
}
