//
// LaunchServicesTests.cs
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
using MonoDevelop.MacInterop;
using MonoDevelop.MacIntegration;
using NUnit.Framework;
using MonoDevelop.Ide;
using AppKit;
using System.IO;
using UnitTests;

namespace MacPlatform.Tests
{
	public class LaunchServicesTests : IdeTestBase
	{
		[Test]
		public void TestLaunchProcessAndTerminate ()
		{
			NSRunningApplication app = LaunchServices.OpenApplicationInternal (new ApplicationStartInfo ("/Applications/Calculator.app"));
			try {
				Assert.IsNotNull (app);
				Assert.That (app.ProcessIdentifier, Is.GreaterThan (-1));
			} finally {
				Assert.IsTrue (app.Terminate (), "Could not kill Calculator app");
			}
		}

		[Test]
		public void TestLaunchInvalidValues ()
		{
			var path = Path.GetTempFileName ();

			Assert.Throws<ArgumentNullException> (() => LaunchServices.OpenApplication ((ApplicationStartInfo)null));
			Assert.Throws<ArgumentException> (() => LaunchServices.OpenApplication (path));
		}

		[Test]
		public void TestLaunchProcessAPIsForInvalidAppBundles ()
		{
			var path = Util.CreateTmpDir ("NonExisting.app");

			Assert.AreEqual (-1, LaunchServices.OpenApplication (path));
			Assert.AreEqual (-1, LaunchServices.OpenApplication (new ApplicationStartInfo (path)));

			NSRunningApplication app = LaunchServices.OpenApplicationInternal (new ApplicationStartInfo (path));
			Assert.IsNull (app);
		}
	}
}

