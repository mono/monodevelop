// 
// MonoDevelopCredentialProvider.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Web;
using NuGet.Configuration;
using NuGet.Credentials;

namespace MonoDevelop.PackageManagement
{
	class MonoDevelopCredentialProvider : NuGet.Credentials.ICredentialProvider
	{
		public string Id {
			get { return "MonoDevelop.PackageManagement.CredentialProvider"; }
		}

		public Task<CredentialResponse> GetAsync (Uri uri, IWebProxy proxy, CredentialRequestType type, string message, bool isRetry, bool nonInteractive, CancellationToken cancellationToken)
		{
			var cp = WebRequestHelper.CredentialProvider;
			if (cp == null)
				return null;

			CredentialType credentialType = GetCredentialType (type);

			return Task.Run (() => {
				ICredentials credentials = cp.GetCredentials (uri, proxy, credentialType, isRetry);
				if (credentials != null) {
					return new CredentialResponse (credentials);
				}
				return new CredentialResponse (CredentialStatus.UserCanceled);
			});
		}

		CredentialType GetCredentialType (CredentialRequestType type)
		{
			switch (type) {
				case CredentialRequestType.Proxy:
					return CredentialType.ProxyCredentials;

				default:
					return CredentialType.RequestCredentials;
			}
		}
	}
}

