//
// MonoDevelopDiscoveryClientProtocol.cs
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
using System.Net;
using System.Web.Services.Discovery;
using MonoDevelop.Core;

namespace MonoDevelop.WebReferences
{
	/// <summary>
	/// Integrates with the MonoDevelop credential provider.
	/// </summary>
	class MonoDevelopDiscoveryClientProtocol : DiscoveryClientProtocol
	{
		protected override WebResponse GetWebResponse (WebRequest request)
		{
#pragma warning disable CS0618 // Type or member is obsolete. Have to use WebRequest with the DiscoveryClientProtocol
			return WebRequestHelper.GetResponse (() => CreateWebRequest (request.RequestUri));
#pragma warning restore CS0618 // Type or member is obsolete.
		}

		/// <summary>
		/// Need to create a new web request just in case the request is not authorized and the
		/// WebRequestHelper is retrying. Otherwise the second attempt will fail since the request
		/// has already been started and will throw an System.InvalidOperationException: request started
		/// when properties such as the Proxy are changed by the WebRequestHelper. The only method used
		/// is the DiscoveryClientProtocol's Download method which creates the web request and then gets
		/// the response immediately afterwards.
		/// </summary>
		HttpWebRequest CreateWebRequest (Uri uri)
		{
			var request = (HttpWebRequest)base.GetWebRequest (uri);
			// Method is set in base.Download so set it here.
			request.Method = "GET";
			// Allow redirects. This allows the Web Reference dialog to show information about the
			// the https://www.w3schools.com/xml/tempconvert.asmx web service.
			request.AllowAutoRedirect = true;
			return request;
		}
	}
}
