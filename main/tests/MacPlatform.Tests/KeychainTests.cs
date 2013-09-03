//
// KeychainTests.cs
//
// Author:
//       Alan McGovern <alan@xamarin.com>
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
using MonoDevelop.MacInterop;
using NUnit.Framework;
using System.IO;

namespace MacPlatform.Tests
{
	[TestFixture]
	public class KeychainTests
	{
		static string TestKeyChain = "ThisIsMonoDevelopsPrivateKeyChainForTests";

		[TestFixtureSetUp]
		public void FixtureSetup ()
		{
			Keychain.CurrentKeychain = Keychain.CreateKeychain (TestKeyChain, "mypassword");
		}

		[TestFixtureTearDown]
		public void FixtureTeardown ()
		{
			Keychain.DeleteKeychain (Keychain.CurrentKeychain);
			Keychain.CurrentKeychain = IntPtr.Zero;
		}

		[Test]
		public void InternetPassword_EmptyUsername ()
		{
			Keychain.AddInternetPassword (new Uri ("http://google.com"), "", "pa55word");
			var password = Keychain.FindInternetPassword (new Uri ("http://google.com"));
			Assert.AreEqual ("pa55word", password, "#1");

			var passAndUser = Keychain.FindInternetUserNameAndPassword (new Uri ("http://google.com"));
			Assert.AreEqual (null, passAndUser.Item1, "#2");
			Assert.AreEqual ("pa55word", passAndUser.Item2, "#3");
		}
	}
}

