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
using System.Collections.Generic;
using ICSharpCode.PackageManagement;
using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageManagementEventsTests
	{
		PackageManagementEvents events;
		List<FakePackage> packages;

		void CreateEvents ()
		{
			packages = new List<FakePackage> ();
			events = new PackageManagementEvents ();
		}

		PackageManagementSelectedProjects CreateSelectedProjects ()
		{
			var solution = new FakePackageManagementSolution ();
			return new PackageManagementSelectedProjects (solution);
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
		public void OnAcceptLicenses_OneEventSubscriber_EventArgsHasPackages ()
		{
			CreateEvents ();
			IEnumerable<IPackage> packages = null;
			events.AcceptLicenses += (sender, e) => packages = e.Packages;

			var expectedPackages = new FakePackage[] {
				new FakePackage ("A"),
				new FakePackage ("B")
			};
			events.OnAcceptLicenses (expectedPackages);

			Assert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void OnAcceptLicenses_OneEventSubscriber_SenderIsPackageEvents ()
		{
			CreateEvents ();
			object eventSender = null;
			events.AcceptLicenses += (sender, e) => eventSender = sender;
			events.OnAcceptLicenses (packages);

			Assert.AreEqual (events, eventSender);
		}

		[Test]
		public void OnAcceptLicenses_NoEventSubscribers_NullReferenceExceptionIsNotThrown ()
		{
			CreateEvents ();
			Assert.DoesNotThrow (() => events.OnAcceptLicenses (packages));
		}

		[Test]
		public void OnAcceptLicenses_NoEventSubscribers_ReturnsTrue ()
		{
			CreateEvents ();
			bool result = events.OnAcceptLicenses (packages);

			Assert.IsTrue (result);
		}

		[Test]
		public void OnAcceptLicenses_EventArgIsAcceptedIsSetToFalse_ReturnsFalse ()
		{
			CreateEvents ();
			events.AcceptLicenses += (sender, e) => e.IsAccepted = false;
			bool result = events.OnAcceptLicenses (packages);

			Assert.IsFalse (result);
		}

		[Test]
		public void OnAcceptLicenses_EventArgIsAcceptedIsSetToTrue_ReturnsTrue ()
		{
			CreateEvents ();
			events.AcceptLicenses += (sender, e) => e.IsAccepted = true;
			bool result = events.OnAcceptLicenses (packages);

			Assert.IsTrue (result);
		}

		[Test]
		public void OnParentPackageInstalled_OneEventSubscriber_EventArgsHasPackage ()
		{
			CreateEvents ();
			IPackage package = null;
			IPackageManagementProject project = null;
			events.ParentPackageInstalled += (sender, e) => {
				package = e.Package;
				project = e.Project;
			};

			var expectedPackage = new FakePackage ("Test");
			var expectedProject = new FakePackageManagementProject ();
			events.OnParentPackageInstalled (expectedPackage, expectedProject);

			Assert.AreEqual (expectedPackage, package);
			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void OnParentPackageInstalled_OneEventSubscriber_SenderIsPackageManagementEvents ()
		{
			CreateEvents ();
			object eventSender = null;
			events.ParentPackageInstalled += (sender, e) => eventSender = sender;

			var package = new FakePackage ("Test");
			events.OnParentPackageInstalled (package, null);

			Assert.AreEqual (events, eventSender);
		}

		[Test]
		public void  OnParentPackageInstalled_NoEventSubscribers_NullReferenceExceptionIsNotThrown ()
		{
			CreateEvents ();
			var package = new FakePackage ("Test");
			Assert.DoesNotThrow (() => events.OnParentPackageInstalled (package, null));
		}

		[Test]
		public void OnParentPackageUninstalled_OneEventSubscriber_EventArgsHasPackage ()
		{
			CreateEvents ();
			IPackage package = null;
			IPackageManagementProject project = null;
			events.ParentPackageUninstalled += (sender, e) => {
				package = e.Package;
				project = e.Project;
			};

			var expectedPackage = new FakePackage ("Test");
			var expectedProject = new FakePackageManagementProject ();
			events.OnParentPackageUninstalled (expectedPackage, expectedProject);

			Assert.AreEqual (expectedPackage, package);
			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void OnParentPackageUninstalled_OneEventSubscriber_SenderIsPackageManagementEvents ()
		{
			CreateEvents ();
			object eventSender = null;
			events.ParentPackageUninstalled += (sender, e) => eventSender = sender;

			var package = new FakePackage ("Test");
			events.OnParentPackageUninstalled (package, null);

			Assert.AreEqual (events, eventSender);
		}

		[Test]
		public void  OnParentPackageUninstalled_NoEventSubscribers_NullReferenceExceptionIsNotThrown ()
		{
			CreateEvents ();
			var package = new FakePackage ("Test");
			Assert.DoesNotThrow (() => events.OnParentPackageUninstalled (package, null));
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
		public void OnSelectProjects_OneEventSubscriber_EventArgsHasSelectedProjects ()
		{
			CreateEvents ();
			IEnumerable<IPackageManagementSelectedProject> selectedProjects = null;
			events.SelectProjects += (sender, e) => selectedProjects = e.SelectedProjects;

			var expectedSelectedProjects = new List<IPackageManagementSelectedProject> ();
			events.OnSelectProjects (expectedSelectedProjects);

			Assert.AreEqual (expectedSelectedProjects, selectedProjects);
		}

		[Test]
		public void OnSelectProjects_OneEventSubscriber_SenderIsPackageEvents ()
		{
			CreateEvents ();
			object eventSender = null;
			events.SelectProjects += (sender, e) => eventSender = sender;
			var selectedProjects = new List<IPackageManagementSelectedProject> ();
			events.OnSelectProjects (selectedProjects);

			Assert.AreEqual (events, eventSender);
		}

		[Test]
		public void OnSelectProjects_NoEventSubscribers_NullReferenceExceptionIsNotThrown ()
		{
			CreateEvents ();
			var selectedProjects = new List<IPackageManagementSelectedProject> ();

			Assert.DoesNotThrow (() => events.OnSelectProjects (selectedProjects));
		}

		[Test]
		public void OnSelectProjects_NoEventSubscribers_ReturnsTrue ()
		{
			CreateEvents ();
			var selectedProjects = new List<IPackageManagementSelectedProject> ();
			bool result = events.OnSelectProjects (selectedProjects);

			Assert.IsTrue (result);
		}

		[Test]
		public void OnSelectProjects_EventArgIsAcceptedIsSetToFalse_ReturnsFalse ()
		{
			CreateEvents ();
			events.SelectProjects += (sender, e) => e.IsAccepted = false;
			var selectedProjects = new List<IPackageManagementSelectedProject> ();
			bool result = events.OnSelectProjects (selectedProjects);

			Assert.IsFalse (result);
		}

		[Test]
		public void OnSelectProjects_EventArgIsAcceptedIsSetToTrue_ReturnsTrue ()
		{
			CreateEvents ();
			events.SelectProjects += (sender, e) => e.IsAccepted = true;
			var selectedProjects = new List<IPackageManagementSelectedProject> ();
			bool result = events.OnSelectProjects (selectedProjects);

			Assert.IsTrue (result);
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

			Assert.AreEqual (FileConflictResolution.Ignore, eventArgs.Resolution);
		}

		[Test]
		public void OnResolveFileConflict_OneEventSubscriberWhichChangesResolutionToOverwrite_ReturnsOverwrite ()
		{
			CreateEvents ();
			events.ResolveFileConflict += (sender, e) => e.Resolution = FileConflictResolution.Overwrite;
			FileConflictResolution resolution = events.OnResolveFileConflict ("message");

			Assert.AreEqual (FileConflictResolution.Overwrite, resolution);
		}

		[Test]
		public void OnResolveFileConflict_NoEventSubscribers_ReturnsIgnoreAll ()
		{
			CreateEvents ();
			FileConflictResolution resolution = events.OnResolveFileConflict ("message");

			Assert.AreEqual (FileConflictResolution.IgnoreAll, resolution);
		}

		[Test]
		public void OnParentPackagesUpdated_OneEventSubscriber_PackagesUpdatedEventFired ()
		{
			CreateEvents ();
			ParentPackagesOperationEventArgs eventArgs = null;
			events.ParentPackagesUpdated += (sender, e) => eventArgs = e;
			var packages = new FakePackage[] { new FakePackage ("Test") };

			events.OnParentPackagesUpdated (packages);

			Assert.AreEqual (packages, eventArgs.Packages);
		}

		[Test]
		public void OnParentPackagesUpdated_OneEventSubscriber_SenderIsPackageManagementEvents ()
		{
			CreateEvents ();
			object eventSender = null;
			events.ParentPackagesUpdated += (sender, e) => eventSender = sender;
			var packages = new FakePackage[] { new FakePackage ("Test") };

			events.OnParentPackagesUpdated (packages);

			Assert.AreEqual (events, eventSender);
		}

		[Test]
		public void OnParentPackagesUpdated_NoEventSubscribers_NullReferenceExceptionNotThrown ()
		{
			CreateEvents ();
			var packages = new FakePackage[] { new FakePackage ("Test") };

			Assert.DoesNotThrow (() => events.OnParentPackagesUpdated (packages));
		}
	}
}


