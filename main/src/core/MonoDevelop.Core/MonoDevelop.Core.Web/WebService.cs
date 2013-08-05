//
// WebService.cs
//
// Author:
//       Bojan Rajkovic <bojan.rajkovic@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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

using Mono.Addins;

namespace MonoDevelop.Core.Web
{
	public class WebService
	{
		const string WebCredentialProvidersPath = "/MonoDevelop/Core/WebCredentialProviders";

		static readonly Lazy<WebService> instance = new Lazy<WebService> (() => new WebService ());

		public static WebService Instance {
			get {
				return instance.Value;
			}
		}

		public IProxyCache ProxyCache { get; set; }
		public ICredentialCache CredentialCache { get; set; }
		public ICredentialProvider CredentialProvider { get; set; }

		WebService () {}

		public static void Initialize ()
		{
			// We can access extension points now, so we need to get the credential provider, which is platform-specific.
			// The credential cache and proxy cache are pure managed implementation.
			Instance.CredentialCache = CredentialStore.Instance;
			Instance.ProxyCache = Web.ProxyCache.Instance;

			// Get the first registered credential provider
			Instance.CredentialProvider = AddinManager.GetExtensionObjects<ICredentialProvider> (WebCredentialProvidersPath).FirstOrDefault ();
		}
	}
}

