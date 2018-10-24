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
			// Allow redirects. This allows the Web Reference dialog to show information about the
			// the https://www.w3schools.com/xml/tempconvert.asmx web service.
			var httpWebRequest = (HttpWebRequest)request;
			httpWebRequest.AllowAutoRedirect = true;

			return WebRequestHelper.GetResponse (() => httpWebRequest);
		}
	}
}
