// 
// MonoDevelopHttpUserAgent.cs
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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopHttpUserAgent
	{
		public MonoDevelopHttpUserAgent()
		{
			CreateUserAgent();
		}
		
		public string Client { get; private set; }
		public string Host { get; private set; }

		void CreateUserAgent()
		{
			Client = GetClient ();
			Host = GetHost();

			var builder = new UserAgentStringBuilder (Client).WithVisualStudioSKU (Host);
			UserAgent.SetUserAgentString (builder);
		}

		static string GetClient ()
		{
			string client = BrandingService.ApplicationName;
			if (client.StartsWith ("Xamarin Studio", StringComparison.OrdinalIgnoreCase)) {
				return "Xamarin Studio";
			}

			return client;
		}
		
		string GetHost()
		{
			return String.Format ("{0}/{1}", BrandingService.ApplicationName, IdeApp.Version);
		}
	}
}