//
// CoreFoundation.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
using System.IO;
using MonoDevelop.MacInterop;
using NUnit.Framework;

namespace MacPlatform.Tests
{
	public class CoreFoundationTests
	{
		[Test]
		public static void TestString ()
		{
			string test = "Test string";
			IntPtr testString = MonoDevelop.MacInterop.CoreFoundation.CreateString ("Test string");

			Assert.AreEqual (MonoDevelop.MacInterop.CoreFoundation.FetchString (testString), test);

			MonoDevelop.MacInterop.CoreFoundation.Release (testString);
		}

		[Test]
		public static void TestURL ()
		{
			string test = "http://www.monodevelop.org";
			IntPtr testUrl = MonoDevelop.MacInterop.CoreFoundation.CreatePathUrl (test);

			Assert.AreEqual (MonoDevelop.MacInterop.CoreFoundation.UrlToPath (testUrl), test);

			MonoDevelop.MacInterop.CoreFoundation.Release (testUrl);
		}

		static string plistFile = Path.GetFullPath (
			Path.Combine (
				Path.GetDirectoryName (typeof (PListFile).Assembly.Location),
				"..",
				"MacOSX",
				"Info.plist.in"
			)
		);

		[Test]
		public void TestApplicationUrls ()
		{
			using (var helper = new PListFile ()) {
				string [] results = MonoDevelop.MacInterop.CoreFoundation.GetApplicationUrls (helper.FilePath, MonoDevelop.MacInterop.CoreFoundation.LSRolesMask.All);

				Assert.Greater (results.Length, 0);
			}
		}

		[Test]
		public void TestApplicationUrl ()
		{
			using (var helper = new PListFile ()) {
				string result = MonoDevelop.MacInterop.CoreFoundation.GetApplicationUrl (helper.FilePath, MonoDevelop.MacInterop.CoreFoundation.LSRolesMask.All);

				Assert.NotNull (result);
			}
		}

		[Test]
		public void TestApplicationUrlOnPListIn ()
		{
			string [] results = MonoDevelop.MacInterop.CoreFoundation.GetApplicationUrls (plistFile, MonoDevelop.MacInterop.CoreFoundation.LSRolesMask.All);

			Assert.AreEqual (results.Length, 0);

			string result = MonoDevelop.MacInterop.CoreFoundation.GetApplicationUrl (plistFile, MonoDevelop.MacInterop.CoreFoundation.LSRolesMask.All);

			Assert.Null (result);
		}

		class PListFile : IDisposable
		{
			readonly string dir = MonoDevelop.Core.FileService.CreateTempDirectory ();
			public string FilePath { get; }

			public PListFile ()
			{
				FilePath = Path.Combine (dir, "Info.plist");
				File.Copy (plistFile, FilePath, true);
			}

			public void Dispose ()
			{
				if (Directory.Exists (dir))
					Directory.Delete (dir, true);
			}
		}
	}
}

