//
// MacKeychainPasswordProvider.cs
//
// Author:
//       Alan McGovern <alan@xamarin.com>
//       Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) 2012 - 2013, Xamarin Inc.
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
using MonoDevelop.Core;
using MonoDevelop.MacInterop;

namespace MonoDevelop.MacIntegration
{
	public class MacKeychainPasswordProvider : IPasswordProvider
	{
		public void AddWebPassword (Uri uri, string password)
		{
			Keychain.AddInternetPassword (uri, password);
		}

		public string GetWebPassword (Uri uri)
		{
			return Keychain.FindInternetPassword (uri);
		}

		public void AddWebUserNameAndPassword (Uri url, string username, string password)
		{
			Keychain.AddInternetPassword (url, username, password);
		}

		public Tuple<string, string> GetWebUserNameAndPassword (Uri url)
		{
			return Keychain.FindInternetUserNameAndPassword (url);
		}

		public void RemoveWebPassword (Uri uri)
		{
			Keychain.RemoveInternetPassword (uri);
		}

		public void RemoveWebUserNameAndPassword (Uri uri)
		{
			Keychain.RemoveInternetUserNameAndPassword (uri);
		}
	}
}
