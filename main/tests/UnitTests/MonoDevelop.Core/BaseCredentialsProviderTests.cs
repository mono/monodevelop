//
// BaseCredentialsProviderTests.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using NUnit.Framework;
using System.IO;

namespace MonoDevelop.Core
{
	[TestFixture]
	public abstract class BaseCredentialsProviderTests
	{
		static readonly string mockUser = "md_test_" + Guid.NewGuid ();
		static readonly Uri mockUrl = new Uri ("ftp://" + Path.GetRandomFileName () + ".randomdomain");
		static readonly string mockPass = "123secure_password!123";

		protected abstract IPasswordProvider GetPasswordProvider ();

		[Test]
		public void InternetPassword_AddGetDelete ()
		{
			var provider = GetPasswordProvider ();

			// Test adding.
			provider.AddWebPassword (mockUrl, mockPass);
			string pass = provider.GetWebPassword (mockUrl);
			Assert.AreEqual (mockPass, pass);

			// Test replacing.
			string newPass = mockPass + "2";
			provider.AddWebPassword (mockUrl, newPass);
			pass = provider.GetWebPassword (mockUrl);
			Assert.AreEqual (newPass, pass);

			// Test removing.
			provider.RemoveWebPassword (mockUrl);
			pass = provider.GetWebPassword (mockUrl);
			Assert.IsNull (pass);
		}

		[Test]
		public void InternetPassword_AddGetDeleteWithUsername ()
		{
			var provider = GetPasswordProvider ();

			// Test adding.
			provider.AddWebUserNameAndPassword (mockUrl, mockUser, mockPass);
			Tuple<string, string> pair = provider.GetWebUserNameAndPassword (mockUrl);
			Assert.AreEqual (mockUser, pair.Item1);
			Assert.AreEqual (mockPass, pair.Item2);

			// Test replacing.
			string newPass = mockPass + "2";
			provider.AddWebUserNameAndPassword (mockUrl, mockUser, newPass);
			pair = provider.GetWebUserNameAndPassword (mockUrl);
			Assert.AreEqual (mockUser, pair.Item1);
			Assert.AreEqual (newPass, pair.Item2);

			// Test removing.
			provider.RemoveWebUserNameAndPassword (mockUrl);
			pair = provider.GetWebUserNameAndPassword (mockUrl);
			Assert.IsNull (pair);
		}
	}
}

