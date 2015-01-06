//
// CheckForUpdatesProgressMonitorTests.cs
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

using System;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Core;
using ICSharpCode.PackageManagement;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class CheckForUpdatesProgressMonitorTests
	{
		TestableCheckForUpdatesProgressMonitor progressMonitor;
		FakeProgressMonitorFactory progressMonitorFactory;
		FakeProgressMonitor fakeProgressMonitor;
		PackageManagementEvents packageEvents;

		void CreateProgressMonitor ()
		{
			progressMonitorFactory = new FakeProgressMonitorFactory ();
			fakeProgressMonitor = progressMonitorFactory.ProgressMonitor;
			packageEvents = new PackageManagementEvents ();
			progressMonitor = new TestableCheckForUpdatesProgressMonitor (progressMonitorFactory, packageEvents);
		}

		[Test]
		public void Constructor_NewInstance_CreatesProgressMonitor ()
		{
			CreateProgressMonitor ();

			Assert.AreEqual (GettextCatalog.GetString ("Checking for package updates..."), progressMonitorFactory.StatusText);
		}

		[Test]
		public void Dispose_NewInstance_ProgressMonitorIsDisposed ()
		{
			CreateProgressMonitor ();

			progressMonitor.Dispose ();

			Assert.IsTrue (fakeProgressMonitor.IsDisposed);
		}

		[Test]
		public void Constructor_PackageManagementMessageEventFired_MessageLoggedToProgressMonitor ()
		{
			CreateProgressMonitor ();

			packageEvents.OnPackageOperationMessageLogged (MessageLevel.Info, "MyMessage");

			fakeProgressMonitor.AssertMessageIsLogged ("MyMessage");
		}

		[Test]
		public void Dispose_MessageLoggedAfterDispose_MessageNotLoggedToProgressMonitor ()
		{
			CreateProgressMonitor ();
			progressMonitor.Dispose ();

			packageEvents.OnPackageOperationMessageLogged (MessageLevel.Info, "MyMessage");

			fakeProgressMonitor.AssertMessageIsNotLogged ("MyMessage");
		}

		[Test]
		public void ReportError_ReportException_ExceptionReportedToProgressMonitor ()
		{
			CreateProgressMonitor ();
			var exception = new Exception ("Error");

			progressMonitor.ReportError (exception);

			fakeProgressMonitor.AssertMessageIsLogged ("Error");
			Assert.AreEqual (GettextCatalog.GetString ("Could not check for package updates. Please see Package Console for details."), fakeProgressMonitor.ReportedErrorMessage);
			Assert.IsTrue (progressMonitor.IsPackageConsoleShown);
		}

		[Test]
		public void ReportError_ReportDispatchServiceException_UnderlyingExceptionReported ()
		{
			CreateProgressMonitor ();
			var exception = new Exception ("Error");
			string message = "An exception was thrown while dispatching a method call in the UI thread.";
			var dispatchServiceException = new Exception (message, exception);

			progressMonitor.ReportError (dispatchServiceException);

			fakeProgressMonitor.AssertMessageIsLogged ("Error");
		}

		[Test]
		public void ReportSuccess_UpdatesAvailable_UpdatesAvailableMessageShownByProgressMonitor ()
		{
			CreateProgressMonitor ();

			progressMonitor.ReportSuccess (true);

			Assert.AreEqual (GettextCatalog.GetString ("Package updates are available."), fakeProgressMonitor.ReportedSuccessMessage);
		}

		[Test]
		public void ReportSuccess_NoUpdatesAvailable_NoUpdatesAvailableMessageShownByProgressMonitor ()
		{
			CreateProgressMonitor ();

			progressMonitor.ReportSuccess (false);

			Assert.AreEqual (GettextCatalog.GetString ("Packages are up to date."), fakeProgressMonitor.ReportedSuccessMessage);
		}

		[Test]
		public void ReportSuccess_NoUpdatesAvailableButWarningLogged_NoUpdatesAvailableWithWarningMessageShownByProgressMonitor ()
		{
			CreateProgressMonitor ();
			packageEvents.OnPackageOperationMessageLogged (MessageLevel.Warning, "WarningMessage");

			progressMonitor.ReportSuccess (false);

			Assert.AreEqual (GettextCatalog.GetString ("No updates found but warnings were reported. Please see Package Console for details."), fakeProgressMonitor.ReportedWarningMessage);
		}
	}
}
