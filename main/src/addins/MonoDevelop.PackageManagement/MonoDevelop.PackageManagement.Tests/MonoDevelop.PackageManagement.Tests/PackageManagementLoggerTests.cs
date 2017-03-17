//
// PackageManagementLoggerTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using NuGet.ProjectManagement;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageManagementLoggerTests
	{
		PackageManagementEvents packageManagementEvents;
		PackageManagementLogger logger;
		List<PackageOperationMessageLoggedEventArgs> messagesLoggedEventArgs;

		void CreateLogger ()
		{
			messagesLoggedEventArgs = new List<PackageOperationMessageLoggedEventArgs> ();
			packageManagementEvents = new PackageManagementEvents ();
			packageManagementEvents.PackageOperationMessageLogged += (sender, e) => {
				messagesLoggedEventArgs.Add (e);
			};

			logger = new PackageManagementLogger (packageManagementEvents);
		}

		void AssertOnPackageOperationMessageLoggedCalled (MessageLevel level, string message)
		{
			PackageOperationMessageLoggedEventArgs eventArgs = messagesLoggedEventArgs [0];
			Assert.AreEqual (message, eventArgs.Message.ToString ());
			Assert.AreEqual (level, eventArgs.Message.Level);
		}

		[Test]
		public void Log_WarningMessageLogged_RaisesMessageLoggedEventWithWarningMessageLevel ()
		{
			CreateLogger ();

			logger.Log (MessageLevel.Warning, "test");

			AssertOnPackageOperationMessageLoggedCalled (MessageLevel.Warning, "test");
		}

		[Test]
		public void Log_FormattedInfoMessageLogged_RaisesMessageLoggedEventWithFormattedMessage ()
		{
			CreateLogger ();

			string format = "Test {0}";
			logger.Log (MessageLevel.Info, format, "C");

			AssertOnPackageOperationMessageLoggedCalled (MessageLevel.Info, "Test C");
		}
	}
}

