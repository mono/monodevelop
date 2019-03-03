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
using MonoDevelop.Ide;
using MonoDevelop.MacInterop;
using NUnit.Framework;

namespace MacPlatform.Tests
{
	[TestFixture]
	public class KeychainTests : IdeTestBase
	{
		const string password = "pa55word";

		const string testDomain = "test-monodevelop-mac-keychain.com";
		const string site = "http://" + testDomain;
		const string siteWithUser = "http://user@" + testDomain;
		const string siteWithPath = site + "/path";
		const string siteWithUserAndPath = siteWithUser + "/path";

		[TestCase(site, site, "", null)]
		[TestCase(site, site, "user", "user")]
		[TestCase(site, site, null, null)]
		[TestCase(siteWithPath, siteWithPath, "", null)]
		[TestCase(siteWithPath, siteWithPath, "user", "user")]
		[TestCase(siteWithPath, siteWithPath, null, null)]
		[TestCase(siteWithUser, siteWithUser, null, "user")]
		[TestCase(siteWithUser, siteWithUser, "user", "user")]
		[TestCase(siteWithUserAndPath, siteWithUserAndPath, null, "user")]
		[TestCase(siteWithUserAndPath, siteWithUserAndPath, "user", "user")]
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

		[TestCase (site, null, null, null, Description = "No User, Password Only")]
		[TestCase (site, "user", "user", "user", Description = "User unchanged, Password Only")]
		[TestCase (site, null, "user2", "user2", Description = "Add User")]
		[TestCase (site, "user", "user2", "user2", Description = "Update User and Password")]
		[TestCase (siteWithPath, null, null, null, Description = "With Path, No User, Password Only")]
		[TestCase (siteWithPath, "user", "user", "user", Description = "With Path, User unchanged, Password Only")]
		[TestCase (siteWithPath, null, "user2", "user2", Description = "Add User")]
		[TestCase (siteWithPath, "user", "user2", "user2", Description = "With Path, Update User and Password")]
		[TestCase (siteWithUser, null, null, "user", Description = "Fixed User, Password Only")]
		[TestCase (siteWithUser, "user", "user2", "user", Description = "Fixed User, User and Password")]
		public void InternetPassword_AddUpdateRemove (string url, string user, string updateUser, string expectedUsername)
		{
			var uri = new Uri (url);
			var updatePassword = password + "Update";

			if (user != null) {
				Keychain.AddInternetPassword (uri, user, password);
			} else {
				Keychain.AddInternetPassword (uri, password);
			}

			try {
				var foundPassword = Keychain.FindInternetPassword (uri);
				var passAndUser = Keychain.FindInternetUserNameAndPassword (uri);

				Assert.AreEqual (password, foundPassword);
				if (user != null)
					Assert.AreEqual (user, passAndUser.Item1);
				Assert.AreEqual (password, passAndUser.Item2);

				if (updateUser != null) {
					Keychain.AddInternetPassword (uri, updateUser, updatePassword);
				} else {
					Keychain.AddInternetPassword (uri, updatePassword);
				}

				foundPassword = Keychain.FindInternetPassword (uri);
				passAndUser = Keychain.FindInternetUserNameAndPassword (uri);

				Assert.AreEqual (updatePassword, foundPassword);
				Assert.AreEqual (expectedUsername, passAndUser.Item1);
				Assert.AreEqual (updatePassword, passAndUser.Item2);
			} finally {
				Keychain.RemoveInternetPassword (uri);
				if (!string.IsNullOrEmpty (uri.UserInfo)) {
					Keychain.RemoveInternetUserNameAndPassword (uri);
				}

				Assert.IsNull (Keychain.FindInternetPassword (uri));
				Assert.IsNull (Keychain.FindInternetUserNameAndPassword (uri));
			}
		}

		[TestCase (site, "", "path2")]
		[TestCase (site, "path1", "path2")]
		[TestCase (siteWithUser, "path1", "path2")]
		public void InternetPassword_SameHostDifferentPath (string url, string path1, string path2)
		{
			var uri1 = new Uri (url + "/" + path1);
			var uri2 = new Uri (url + "/" + path2);
			var user1 = !string.IsNullOrEmpty (uri1.UserInfo) ? uri1.UserInfo : "user1";
			var user2 = !string.IsNullOrEmpty (uri2.UserInfo) ? uri2.UserInfo : "user2";
			var password1 = password + "1";
			var password2 = password + "2";

			if (!string.IsNullOrEmpty (uri1.UserInfo))
				Keychain.AddInternetPassword (uri1, password1);
			else
				Keychain.AddInternetPassword (uri1, user1, password1);

			if (!string.IsNullOrEmpty (uri2.UserInfo))
				Keychain.AddInternetPassword (uri2, password2);
			else
				Keychain.AddInternetPassword (uri2, user2, password2);

			try {
				var foundPassword1 = Keychain.FindInternetPassword (uri1);
				var foundPasswordAndUser1 = Keychain.FindInternetUserNameAndPassword (uri1);
				var foundPassword2 = Keychain.FindInternetPassword (uri2);
				var foundPasswordAndUser2 = Keychain.FindInternetUserNameAndPassword (uri2);

				Assert.AreEqual (password1, foundPassword1);
				Assert.AreEqual (user1, foundPasswordAndUser1.Item1);
				Assert.AreEqual (password1, foundPasswordAndUser1.Item2);
				Assert.AreEqual (password2, foundPassword2);
				Assert.AreEqual (user2, foundPasswordAndUser2.Item1);
				Assert.AreEqual (password2, foundPasswordAndUser2.Item2);
			} finally {
				Keychain.RemoveInternetPassword (uri1);
				Keychain.RemoveInternetPassword (uri2);
				if (!string.IsNullOrEmpty (uri1.UserInfo)) {
					Keychain.RemoveInternetUserNameAndPassword (uri1);
				}
				if (!string.IsNullOrEmpty (uri2.UserInfo)) {
					Keychain.RemoveInternetUserNameAndPassword (uri2);
				}

				Assert.IsNull (Keychain.FindInternetPassword (uri1));
				Assert.IsNull (Keychain.FindInternetUserNameAndPassword (uri1));

				Assert.IsNull (Keychain.FindInternetPassword (uri2));
				Assert.IsNull (Keychain.FindInternetUserNameAndPassword (uri2));
			}
		}

		[Test]
		public void InternetPassword_Remove ()
		{
			string passwordNoUser = password + "nouser";

			var uriBase = new Uri ("http://" + testDomain);
			var uri1 = new Uri ("http://user1@" + testDomain);
			var uri2 = new Uri ("http://user2@" + testDomain);
			var uri3 = new Uri ("http://" + testDomain + "/path");

			try {
				Keychain.AddInternetPassword (uriBase, passwordNoUser);
				Keychain.AddInternetPassword (uri1, password);
				Keychain.AddInternetPassword (uri2, password);
				Keychain.AddInternetPassword (uri3, "user2", password);

				AssertKeychain (uriBase, null, passwordNoUser, passwordNoUser);
				AssertKeychain (uri1, "user1", password, password);
				AssertKeychain (uri2, "user2", password, password);
				AssertKeychain (uri3, "user2", password, password);

				Keychain.RemoveInternetPassword (uriBase);
				// We removed the password for the entry without user
				// The next best match for uriBase should be "user1" from uri1
				AssertKeychain (uriBase, "user1", password, password);
				AssertKeychain (uri1, "user1", password, password);
				AssertKeychain (uri2, "user2", password, password);
				AssertKeychain (uri3, "user2", password, password);

				Keychain.RemoveInternetUserNameAndPassword (uri1);
				// We removed user and pass for user1.
				// The next best match for uriBase "user2" from uri2 now
				AssertKeychain (uriBase, "user2", password, password);
				// uri1 has no other match, because the user name is specified in the url
				AssertKeychain (uri1, null, null, null);
				AssertKeychain (uri2, "user2", password, password);
				AssertKeychain (uri3, "user2", password, password);

				Keychain.RemoveInternetUserNameAndPassword (uri2);
				AssertKeychain (uriBase, null, null, null);
				AssertKeychain (uri1, null, null, null);
				AssertKeychain (uri2, null, null, null);
				AssertKeychain (uri3, "user2", password, password);

				Keychain.RemoveInternetUserNameAndPassword (uri3);
				// We removed all entries
				AssertKeychain (uriBase, null, null, null);
				AssertKeychain (uri1, null, null, null);
				AssertKeychain (uri2, null, null, null);
				AssertKeychain (uri3, null, null, null);
			} finally {
				Keychain.RemoveInternetPassword (uriBase);
				Keychain.RemoveInternetPassword (uri1);
				Keychain.RemoveInternetPassword (uri2);
				Keychain.RemoveInternetPassword (uri3);
				Keychain.RemoveInternetUserNameAndPassword (uriBase);
				Keychain.RemoveInternetUserNameAndPassword (uri1);
				Keychain.RemoveInternetUserNameAndPassword (uri2);
				Keychain.RemoveInternetUserNameAndPassword (uri3);

				Assert.IsNull (Keychain.FindInternetPassword (uriBase));
				Assert.IsNull (Keychain.FindInternetUserNameAndPassword (uriBase));
				Assert.IsNull (Keychain.FindInternetPassword (uri1));
				Assert.IsNull (Keychain.FindInternetUserNameAndPassword (uri1));
				Assert.IsNull (Keychain.FindInternetPassword (uri2));
				Assert.IsNull (Keychain.FindInternetUserNameAndPassword (uri2));
				Assert.IsNull (Keychain.FindInternetPassword (uri3));
				Assert.IsNull (Keychain.FindInternetUserNameAndPassword (uri3));
			}

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
	}
}