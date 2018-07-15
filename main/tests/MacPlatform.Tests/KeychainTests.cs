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
using System.Text;

namespace MacPlatform.Tests
{
	[TestFixture]
	public class KeychainTests
	{
		const string TestKeyChain = "ThisIsMonoDevelopsPrivateKeyChainForTests";
		const string password = "pa55word";

		[SetUp]
		public void Setup ()
		{
			Keychain.TryDeleteKeychain (TestKeyChain);
			Keychain.CurrentKeychain = Keychain.CreateKeychain (TestKeyChain, "mypassword");
		}

		[TearDown]
		public void Teardown ()
		{
			Keychain.DeleteKeychain (Keychain.CurrentKeychain);
			Keychain.CurrentKeychain = IntPtr.Zero;
		}

		const string site = "http://google.com";
		const string siteWithUser = "http://user@google.com";

		[TestCase(site, site, "", null)]
		[TestCase(site, site, "user", "user")]
		[TestCase(site, site, null, null)]
		[TestCase(siteWithUser, siteWithUser, null, "user")]
		[TestCase(siteWithUser, siteWithUser, "user", "user")]
		//[TestCase(siteWithUser, "http://user2@google.com", "user2", "user2")] Need to figure this one out first
		public void InternetPassword_AddFindRemove (string url, string probeUrl, string user, string expectedUsername)
		{
			var uri = new Uri (url);

			if (user != null) {
				Keychain.AddInternetPassword (uri, user, password);
			} else {
				Keychain.AddInternetPassword (uri, password);
			}

			uri = new Uri (probeUrl);

			try {
				var foundPassword = Keychain.FindInternetPassword (uri);
				var passAndUser = Keychain.FindInternetUserNameAndPassword (uri);

				Assert.AreEqual (password, foundPassword, "#1");
				Assert.AreEqual (expectedUsername, passAndUser.Item1, "#2");
				Assert.AreEqual (password, passAndUser.Item2, "#3");
			} finally {
				Keychain.RemoveInternetPassword (uri);
				if (!string.IsNullOrEmpty (uri.UserInfo)) {
					Keychain.RemoveInternetUserNameAndPassword (uri);
				}

				Assert.IsNull (Keychain.FindInternetPassword (uri));
				Assert.IsNull (Keychain.FindInternetUserNameAndPassword (uri));
			}
		}

		[Test]
		public void InternetPassword_Remove ()
		{
			var uriBase = new Uri ("http://google.com");
			var uri1 = new Uri ("http://user1@google.com");
			var uri2 = new Uri ("http://user2@google.com");
			var uri3 = new Uri ("http://user2@google.com");

			Keychain.AddInternetPassword (uriBase, password);
			Keychain.AddInternetPassword (uri1, "user1", password);
			Keychain.AddInternetPassword (uri2, "user2", password);
			Keychain.AddInternetPassword (uri3, "user3", password);

			AssertKeychain (uriBase, null, password, password);
			AssertKeychain (uri1, null, password, password);
			AssertKeychain (uri2, null, password, password);
			AssertKeychain (uri3, null, password, password);

			// We removed the password for null user
			Keychain.RemoveInternetPassword (uriBase);
			AssertKeychain (uriBase, "user1", password, password);
			AssertKeychain (uri1, "user1", password, password);
			AssertKeychain (uri2, "user1", password, password);
			AssertKeychain (uri3, "user1", password, password);

			// We removed user and pass for user1.
			Keychain.RemoveInternetUserNameAndPassword (uri1);
			AssertKeychain (uriBase, "user2", password, password);
			AssertKeychain (uri1, "user2", password, null);
			AssertKeychain (uri2, "user2", password, password);
			AssertKeychain (uri3, "user2", password, password);

			// We removed user and pass for non-user
			Keychain.RemoveInternetPassword (uri2);
			AssertKeychain (uriBase, "user3", password, password);
			AssertKeychain (uri1, "user3", password, null);
			AssertKeychain (uri2, "user3", password, null);
			AssertKeychain (uri2, "user3", password, null);

			Keychain.RemoveInternetUserNameAndPassword (uri3);
			AssertKeychain (uriBase, null, null, null);
			AssertKeychain (uri1, null, null, null);
			AssertKeychain (uri2, null, null, null);
			AssertKeychain (uri2, null, null, null);

			void AssertKeychain (Uri uri, string expectedUser, string expectedUserPassword, string expectedPassword)
			{
				var userPassword = Keychain.FindInternetUserNameAndPassword (uri);
				var foundPassword = Keychain.FindInternetPassword (uri);

				if (expectedUser == null && expectedPassword == null) {
					Assert.IsNull (userPassword);
				} else {
					Assert.AreEqual (expectedUser, userPassword.Item1);
					Assert.AreEqual (expectedUserPassword, userPassword.Item2);

				}
				Assert.AreEqual (expectedPassword, foundPassword);
			}
		}

		[Test]
		public void ToNullTerminatedUtf8IsNullTerminated ()
		{
			var str = "This is a test string";

			var bytes = Encoding.UTF8.GetBytes (str);
			var bytesWithNullTerm = str.ToNullTerminatedUtf8 ();

			Assert.AreEqual (bytes.Length + 1, bytesWithNullTerm.Length);
			Assert.AreEqual (0, bytesWithNullTerm [bytesWithNullTerm.Length - 1]);

			// UTF8Encoding does not handle null terminated strings, so do the check like this.
			Assert.AreEqual (str + char.MinValue, Encoding.UTF8.GetString (bytesWithNullTerm));

			const int offset = 2;
			var bytesWithNullTermOffset = str.ToNullTerminatedUtf8 (offset);
			Assert.AreEqual (str.Substring (offset) + char.MinValue, Encoding.UTF8.GetString (bytesWithNullTermOffset));
		}
	}
}

