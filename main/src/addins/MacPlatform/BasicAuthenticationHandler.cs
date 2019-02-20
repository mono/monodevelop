//
// BasicAuthenticationHandler.cs
//
// Based on based on Mono's mono/mcs/class/System/System.Net/BasicClient.cs
//
// Authors:
//       Gonzalo Paniagua Javier (gonzalo@ximian.com)
//       Matt Ward <matt.ward@microsoft.com>
//
// (C) 2003 Ximian, Inc (http://www.ximian.com)
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
using System.Net.Http;
using System.Net.Http.Headers;

namespace MacPlatform
{
	/// <summary>
	/// NSUrlSessionHandler does not handle all WWW-Authenticate basic auth responses. VSTS NuGet packages sources return
	/// WWW-Authenticate: Bearer authorization_uri=https://login.windows.net/,  Basic realm="https://pkg.visualstudio.com/",  TFS-Federated
	/// This basic auth challenge is not delivered to NSUrlSessionHandlerDelegate's DidReceiveChallenge.
	/// WWW-Authenticate headers that start with 'Basic' do seem to be passed to DidReceiveChallenge.
	/// </summary>
	static class BasicAuthenticationHandler
	{
		internal static bool Authenticate (HttpRequestMessage request, HttpResponseMessage response, ICredentials credentials)
		{
			if (credentials == null)
				return false;

			if (!IsBasicAuthentication (response))
				return false;

			return AddBasicAuthenticationHeader (request, credentials);
		}

		static bool IsBasicAuthentication (HttpResponseMessage response)
		{
			foreach (string authHeader in response.Headers.GetValues ("WWW-Authenticate")) {
				if (string.IsNullOrEmpty (authHeader))
					continue;
				if (authHeader.IndexOf ("basic", StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}
			return false;
		}

		static bool AddBasicAuthenticationHeader (HttpRequestMessage request, ICredentials credentials)
		{
			string authHeader = GetBasicAuthenticationHeader (request.RequestUri, credentials);
			if (authHeader == null)
				return false;

			request.Headers.Authorization = new AuthenticationHeaderValue ("Basic", authHeader);

			return true;
		}

		static string GetBasicAuthenticationHeader (Uri requestUri, ICredentials credentials)
		{
			var foundCredential = credentials.GetCredential (requestUri, "basic");
			if (foundCredential == null)
				return null;

			string userName = foundCredential.UserName;
			if (string.IsNullOrEmpty (userName))
				return null;

			string password = foundCredential.Password;
			string domain = foundCredential.Domain;
			byte [] bytes;

			// If domain is set, MS sends "domain\user:password". 
			if (string.IsNullOrEmpty (domain) || domain.Trim () == "")
				bytes = GetBytes (userName + ":" + password);
			else
				bytes = GetBytes (domain + "\\" + userName + ":" + password);

			return Convert.ToBase64String (bytes);
		}

		static byte [] GetBytes (string str)
		{
			int i = str.Length;
			byte [] result = new byte [i];
			for (--i; i >= 0; i--)
				result [i] = (byte)str [i];

			return result;
		}
	}
}
