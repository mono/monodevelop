// 
// PasswordService.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2012, Xamarin Inc.
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

namespace MonoDevelop.Core
{
	public static class PasswordService
	{
		static IPasswordProvider GetPasswordProvider ()
		{
			const string PasswordProvidersPath = "/MonoDevelop/Core/PasswordProvider";
			return AddinManager.GetExtensionObjects <IPasswordProvider> (PasswordProvidersPath).FirstOrDefault ();
		}

		public static void AddWebPassword (Uri url, string password)
		{
			var provider = GetPasswordProvider ();
			if (provider != null)
				provider.AddWebPassword (url, password);
		}

		public static void AddWebUserNameAndPassword (Uri url, string username, string password)
		{
			var provider = GetPasswordProvider ();
			if (provider != null)
				provider.AddWebUserNameAndPassword (url, username, password);
		}

		public static string GetWebPassword (Uri url)
		{
			var provider = GetPasswordProvider ();
			return provider != null ? provider.GetWebPassword (url) : null;
		}

		public static Tuple<string, string> GetWebUserNameAndPassword (Uri url)
		{
			var provider = GetPasswordProvider ();
			return provider != null ? provider.GetWebUserNameAndPassword (url) : null;
		}

		public static void RemoveWebPassword (Uri url)
		{
			var provider = GetPasswordProvider ();
			if (provider != null)
				provider.RemoveWebPassword (url);
		}

		public static void RemoveWebUsernameAndPassword (Uri url)
		{
			var provider = GetPasswordProvider ();
			if (provider != null)
				provider.RemoveWebUserNameAndPassword (url);
		}
	}
}

