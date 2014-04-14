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
using System.Net;

namespace MonoDevelop.Core.Web
{
	[Obsolete ("Use WebRequestHelper.ProxyAuthenticationHandler")]
	public static class WebService
	{
		static IProxyCache proxyCache = new ForwardingProxyCache ();
		static ICredentialCache credentialCache = new ForwardingCredentialCache ();
		static ICredentialProvider credentialProvider = new ForwardingCredentialProvider ();

		public static IProxyCache ProxyCache { get { return proxyCache; } }
		public static ICredentialCache CredentialCache { get { return credentialCache; } }
		public static ICredentialProvider CredentialProvider { get { return credentialProvider; } }

		class ForwardingCredentialProvider : ICredentialProvider
		{
			public ICredentials GetCredentials (
				Uri uri, IWebProxy proxy, CredentialType credentialType, ICredentials existingCredentials, bool retrying)
			{
				var pah = WebRequestHelper.ProxyAuthenticationHandler;
				return pah != null ? pah.GetCredentialsFromUser (uri, proxy, credentialType, existingCredentials, retrying) : null;
			}
		}

		class ForwardingProxyCache : IProxyCache
		{
			public void Add (IWebProxy proxy)
			{
				var pah = WebRequestHelper.ProxyAuthenticationHandler;
				if (pah != null)
					pah.AddProxyToCache (proxy);
			}

			public IWebProxy GetProxy (Uri uri)
			{
				var pah = WebRequestHelper.ProxyAuthenticationHandler;
				return pah != null ? pah.GetCachedProxy (uri) : null;
			}
		}

		class ForwardingCredentialCache : ICredentialCache
		{
			public void Add (Uri uri, ICredentials credentials, CredentialType credentialType)
			{
				var pah = WebRequestHelper.ProxyAuthenticationHandler;
				if (pah != null)
					pah.AddCredentialsToCache (uri, credentials, credentialType);
			}

			public ICredentials GetCredentials (Uri uri, CredentialType credentialType)
			{
				var pah = WebRequestHelper.ProxyAuthenticationHandler;
				return pah != null ? pah.GetCachedCredentials (uri, credentialType) : null;
			}
		}
	}

	[Obsolete ("Use WebRequestHelper.ProxyAuthenticationHandler")]
	public interface IProxyCache
	{
		void Add (IWebProxy proxy);

		IWebProxy GetProxy (Uri uri);
	}

	[Obsolete ("Use WebRequestHelper.ProxyAuthenticationHandler")]
	public interface ICredentialCache
	{
		void Add (Uri uri, ICredentials credentials, CredentialType credentialType);

		ICredentials GetCredentials (Uri uri, CredentialType credentialType);
	}
}

