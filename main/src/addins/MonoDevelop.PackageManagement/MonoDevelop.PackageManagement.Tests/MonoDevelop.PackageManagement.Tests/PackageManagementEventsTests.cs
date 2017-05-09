//
// PackageManagementEventsTests.cs
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
using NuGet.ProjectManagement;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageManagementEventsTests
	{
		PackageManagementEvents events;

		void CreateEvents ()
		{
			events = new PackageManagementEvents ();
		}

		[Test]
		public void OnPackageOperationsStarting_OneEventSubscriber_PackageOperationsStartingFired ()
		{
			CreateEvents ();
			EventArgs eventArgs = null;
			events.PackageOperationsStarting += (sender, e) => eventArgs = e;
			events.OnPackageOperationsStarting ();

			Assert.IsNotNull (eventArgs);
		}

		[Test]
		public void OnPackageOperationsStarting_OneEventSubscriber_SenderIsPackageManagementEvents ()
		{
			CreateEvents ();
			object eventSender = null;
			events.PackageOperationsStarting += (sender, e) => eventSender = sender;
			events.OnPackageOperationsStarting ();

			Assert.AreEqual (events, eventSender);
		}

		[Test]
		public void OnPackageOperationsStarting_NoEventSubscribers_NullReferenceExceptionNotThrown ()
		{
			CreateEvents ();
			Assert.DoesNotThrow (() => events.OnPackageOperationsStarting ());
		}

		[Test]
		public void OnPackageOperationError_OneEventSubscriber_PackageOperationErrorEventArgsHasException ()
		{
			CreateEvents ();
			Exception exception = null;
			events.PackageOperationError += (sender, e) => exception = e.Exception;

			Exception expectedException = new Exception ("Test");
			events.OnPackageOperationError (expectedException);

			Assert.AreEqual (expectedException, exception);	
		}

		[Test]
		public void OnPackageOperationError_OneEventSubscriber_SenderIsPackageManagementEvents ()
		{
			CreateEvents ();
			object eventSender = null;
			events.PackageOperationError += (sender, e) => eventSender = sender;

			Exception expectedException = new Exception ("Test");
			events.OnPackageOperationError (expectedException);

			Assert.AreEqual (events, eventSender);	
		}

		[Test]
		public void OnPackageOperationError_NoEventSubscribers_NullReferenceExceptionNotThrown ()
		{
			CreateEvents ();
			Exception expectedException = new Exception ("Test");

			Assert.DoesNotThrow (() => events.OnPackageOperationError (expectedException));
		}

		[Test]
		public void OnPackageOperationMessageLogged_OneEventSubscriber_SenderIsPackageManagementEvents ()
		{
			CreateEvents ();
			object eventSender = null;
			events.PackageOperationMessageLogged += (sender, e) => eventSender = sender;

			events.OnPackageOperationMessageLogged (MessageLevel.Info, "Test");

			Assert.AreEqual (events, eventSender);
		}

		[Test]
		public void OnPackageOperationMessageLogged_NoEventSubscribers_NullReferenceExceptionIsNotThrown ()
		{
			CreateEvents ();
			Assert.DoesNotThrow (() => events.OnPackageOperationMessageLogged (MessageLevel.Info, "Test"));
		}

		[Test]
		public void OnPackageOperationMessageLogged_InfoMessageLoggedWithOneEventSubscriber_EventArgsHasInfoMessageLevel ()
		{
			CreateEvents ();
			PackageOperationMessageLoggedEventArgs eventArgs = null;
			events.PackageOperationMessageLogged += (sender, e) => eventArgs = e;

			events.OnPackageOperationMessageLogged (MessageLevel.Info, "Test");

			Assert.AreEqual (MessageLevel.Info, eventArgs.Message.Level);
		}

		[Test]
		public void OnPackageOperationMessageLogged_FormattedInfoMessageLoggedWithOneEventSubscriber_EventArgsHasFormattedMessage ()
		{
			CreateEvents ();
			PackageOperationMessageLoggedEventArgs eventArgs = null;
			events.PackageOperationMessageLogged += (sender, e) => eventArgs = e;

			string format = "Test {0}";
			events.OnPackageOperationMessageLogged (MessageLevel.Info, format, "B");

			string message = eventArgs.Message.ToString ();

			string expectedMessage = "Test B";
			Assert.AreEqual (expectedMessage, message);
		}

		[Test]
		public void OnResolveFileConflict_OneEventSubscriber_SenderIsPackageEvents ()
		{
			CreateEvents ();
			object eventSender = null;
			events.ResolveFileConflict += (sender, e) => eventSender = sender;
			events.OnResolveFileConflict ("message");

			Assert.AreEqual (events, eventSender);
		}

		[Test]
		public void OnResolveFileConflict_OneEventSubscriber_MessageAddedToEventArgs ()
		{
			CreateEvents ();
			ResolveFileConflictEventArgs eventArgs = null;
			events.ResolveFileConflict += (sender, e) => eventArgs = e;
			events.OnResolveFileConflict ("message");

			Assert.AreEqual ("message", eventArgs.Message);
		}

		[Test]
		public void OnResolveFileConflict_OneEventSubscriberWhichDoesNotChangeEventArgs_EventArgsHasFileConflictResolutionOfIgnore ()
		{
			CreateEvents ();
			ResolveFileConflictEventArgs eventArgs = null;
			events.ResolveFileConflict += (sender, e) => eventArgs = e;
			events.OnResolveFileConflict ("message");

			Assert.AreEqual (FileConflictAction.Ignore, eventArgs.Resolution);
		}

		[Test]
		public void OnResolveFileConflict_OneEventSubscriberWhichChangesResolutionToOverwrite_ReturnsOverwrite ()
		{
			CreateEvents ();
			events.ResolveFileConflict += (sender, e) => e.Resolution = FileConflictAction.Overwrite;
			FileConflictAction resolution = events.OnResolveFileConflict ("message");

			Assert.AreEqual (FileConflictAction.Overwrite, resolution);
		}

		[Test]
		public void OnResolveFileConflict_NoEventSubscribers_ReturnsIgnoreAll ()
		{
			CreateEvents ();
			FileConflictAction resolution = events.OnResolveFileConflict ("message");

			Assert.AreEqual (FileConflictAction.IgnoreAll, resolution);
		}
	}
}


