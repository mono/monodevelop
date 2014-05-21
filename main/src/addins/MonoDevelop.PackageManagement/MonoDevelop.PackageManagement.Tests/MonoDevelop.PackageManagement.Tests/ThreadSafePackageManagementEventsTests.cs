//
// ThreadSafePackageManagementEventsTests.cs
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;
using MonoDevelop.Ide;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ThreadSafePackageManagementEventsTests
	{
		ThreadSafePackageManagementEvents threadSafeEvents;
		PackageManagementEvents unsafeEvents;
		bool eventHandlerFired;
		bool isGuiSyncDispatchCalled;

		void CreateEvents ()
		{
			isGuiSyncDispatchCalled = false;
			unsafeEvents = new PackageManagementEvents ();
			threadSafeEvents = new ThreadSafePackageManagementEvents (unsafeEvents, RunGuiSyncDispatch);
		}

		void RunGuiSyncDispatch (MessageHandler messageHandler)
		{
			isGuiSyncDispatchCalled = true;
			messageHandler.Invoke ();
		}

		void OnEventHandlerFired (object sender, EventArgs e)
		{
			eventHandlerFired = true;
		}

		[Test]
		public void OnPackageOperationsStarting_NoInvokeRequired_NonThreadSafePackageOperationsStartingMethodCalled ()
		{
			CreateEvents ();
			bool called = false;
			unsafeEvents.PackageOperationsStarting += (sender, e) => called = true;
			threadSafeEvents.OnPackageOperationsStarting ();

			Assert.IsTrue (called);
		}

		[Test]
		public void OnPackageOperationError_NoInvokeRequired_NonThreadSafeOnPackageOperationErrorMethodCalled ()
		{
			CreateEvents ();
			Exception exception = null;
			unsafeEvents.PackageOperationError += (sender, e) => exception = e.Exception;
			var expectedException = new Exception ("test");

			threadSafeEvents.OnPackageOperationError (expectedException);

			Assert.AreEqual (expectedException, exception);
		}

		[Test]
		public void OnAcceptLicenses_NoInvokeRequired_NonThreadSafeOnAcceptLicensesMethodCalled ()
		{
			CreateEvents ();
			IEnumerable<IPackage> packages = null;
			unsafeEvents.AcceptLicenses += (sender, e) => packages = e.Packages;
			var expectedPackages = new List<IPackage> ();

			threadSafeEvents.OnAcceptLicenses (expectedPackages);

			Assert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void OnAcceptLicenses_NoInvokeRequired_NonThreadSafeOnAcceptLicensesMethodCalledAndReturnsResult ()
		{
			CreateEvents ();
			unsafeEvents.AcceptLicenses += (sender, e) => e.IsAccepted = false;

			bool result = threadSafeEvents.OnAcceptLicenses (null);

			Assert.IsFalse (result);
		}

		[Test]
		public void OnParentPackageInstalled_NoInvokeRequired_NonThreadSafeOnParentPackageInstalledMethodCalled ()
		{
			CreateEvents ();
			IPackage package = null;
			IPackageManagementProject project = null;
			unsafeEvents.ParentPackageInstalled += (sender, e) => {
				package = e.Package;
				project = e.Project;
			};
			var expectedPackage = new FakePackage ();
			var expectedProject = new FakePackageManagementProject ();

			threadSafeEvents.OnParentPackageInstalled (expectedPackage, expectedProject);

			Assert.AreEqual (expectedPackage, package);
		}

		[Test]
		public void OnParentPackageUninstalled_NoInvokeRequired_NonThreadSafeOnParentPackageUninstalledMethodCalled ()
		{
			CreateEvents ();
			IPackage package = null;
			IPackageManagementProject project = null;
			unsafeEvents.ParentPackageUninstalled += (sender, e) => {
				package = e.Package;
				project = e.Project;
			};
			var expectedPackage = new FakePackage ();
			var expectedProject = new FakePackageManagementProject ();

			threadSafeEvents.OnParentPackageUninstalled (expectedPackage, expectedProject);

			Assert.AreEqual (expectedPackage, package);
		}

		[Test]
		public void OnPackageOperationMessageLogged_NoInvokeRequired_NonThreadSafeOnPackageOperationMessageLoggedMethodCalled ()
		{
			CreateEvents ();
			MessageLevel actualLevel = MessageLevel.Debug;
			string actualMessage = null;
			unsafeEvents.PackageOperationMessageLogged += (sender, e) => {
				actualLevel = e.Message.Level;
				actualMessage = e.Message.ToString ();
			};

			var messageLevel = MessageLevel.Warning;
			string message = "abc {0}";
			string arg = "test";
			threadSafeEvents.OnPackageOperationMessageLogged (messageLevel, message, arg);

			Assert.AreEqual (messageLevel, actualLevel);
			Assert.AreEqual ("abc test", actualMessage);
		}

		[Test]
		public void AcceptLicenses_UnsafeEventFired_ThreadSafeEventFired ()
		{
			CreateEvents ();
			bool fired = false;
			threadSafeEvents.AcceptLicenses += (sender, e) => fired = true;
			unsafeEvents.OnAcceptLicenses (null);

			Assert.IsTrue (fired);
		}

		[Test]
		public void AcceptLicenses_UnsafeEventFiredAfterEventHandlerRemoved_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.AcceptLicenses += OnEventHandlerFired;
			threadSafeEvents.AcceptLicenses -= OnEventHandlerFired;
			unsafeEvents.OnAcceptLicenses (null);

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void PackageOperationsStarting_UnsafeEventFired_ThreadSafeEventFired ()
		{
			CreateEvents ();
			bool fired = false;
			threadSafeEvents.PackageOperationsStarting += (sender, e) => fired = true;
			unsafeEvents.OnPackageOperationsStarting ();

			Assert.IsTrue (fired);
		}

		[Test]
		public void PackageOperationsStarting_UnsafeEventFiredAndInvokeRequired_ThreadSafeEventIsSafelyInvoked ()
		{
			CreateEvents ();
			threadSafeEvents.PackageOperationsStarting += OnEventHandlerFired;
			unsafeEvents.OnPackageOperationsStarting ();

			Assert.IsTrue (isGuiSyncDispatchCalled);
		}

		[Test]
		public void PackageOperationsStarting_UnsafeEventFiredAndInvokeRequiredButNoEventHandlerRegistered_ThreadSafeEventIsNotInvoked ()
		{
			CreateEvents ();
			unsafeEvents.OnPackageOperationsStarting ();

			Assert.IsFalse (isGuiSyncDispatchCalled);
		}

		[Test]
		public void PackageOperationsStarting_UnsafeEventFiredAfterEventHandlerRemoved_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.PackageOperationsStarting += OnEventHandlerFired;
			threadSafeEvents.PackageOperationsStarting -= OnEventHandlerFired;
			unsafeEvents.OnPackageOperationsStarting ();

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void PackageOperationError_UnsafeEventFired_ThreadSafeEventFired ()
		{
			CreateEvents ();
			bool fired = false;
			threadSafeEvents.PackageOperationError += (sender, e) => fired = true;
			unsafeEvents.OnPackageOperationError (new Exception ());

			Assert.IsTrue (fired);
		}

		[Test]
		public void PackageOperationError_UnsafeEventFiredAndInvokeRequired_ThreadSafeEventIsSafelyInvoked ()
		{
			CreateEvents ();
			threadSafeEvents.PackageOperationError += OnEventHandlerFired;
			var expectedException = new Exception ("Test");

			unsafeEvents.OnPackageOperationError (expectedException);

			Assert.IsTrue (isGuiSyncDispatchCalled);
		}

		[Test]
		public void PackageOperationError_UnsafeEventFiredAndInvokeRequiredButNoEventHandlerRegistered_ThreadSafeEventIsNotInvoked ()
		{
			CreateEvents ();
			unsafeEvents.OnPackageOperationError (new Exception ());

			Assert.IsFalse (isGuiSyncDispatchCalled);
		}

		[Test]
		public void PackageOperationError_UnsafeEventFiredAfterEventHandlerRemoved_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.PackageOperationError += OnEventHandlerFired;
			threadSafeEvents.PackageOperationError -= OnEventHandlerFired;
			unsafeEvents.OnPackageOperationError (new Exception ());

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void ParentPackageInstalled_UnsafeEventFired_ThreadSafeEventFired ()
		{
			CreateEvents ();
			bool fired = false;
			threadSafeEvents.ParentPackageInstalled += (sender, e) => fired = true;
			unsafeEvents.OnParentPackageInstalled (null, null);

			Assert.IsTrue (fired);
		}

		[Test]
		public void ParentPackageInstalled_UnsafeEventFiredAndInvokeRequired_ThreadSafeEventIsSafelyInvoked ()
		{
			CreateEvents ();
			threadSafeEvents.ParentPackageInstalled += OnEventHandlerFired;
			var expectedPackage = new FakePackage ();

			unsafeEvents.OnParentPackageInstalled (expectedPackage, null);

			Assert.IsTrue (isGuiSyncDispatchCalled);
		}

		[Test]
		public void ParentPackageInstalled_UnsafeEventFiredAndInvokeRequiredButNoEventHandlerRegistered_ThreadSafeEventIsNotInvoked ()
		{
			CreateEvents ();
			unsafeEvents.OnParentPackageInstalled (new FakePackage (), null);

			Assert.IsFalse (isGuiSyncDispatchCalled);
		}

		[Test]
		public void ParentPackageInstalled_UnsafeEventFiredAfterEventHandlerRemoved_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.ParentPackageInstalled += OnEventHandlerFired;
			threadSafeEvents.ParentPackageInstalled -= OnEventHandlerFired;
			unsafeEvents.OnParentPackageInstalled (null, null);

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void ParentPackageUninstalled_UnsafeEventFired_ThreadSafeEventFired ()
		{
			CreateEvents ();
			bool fired = false;
			threadSafeEvents.ParentPackageUninstalled += (sender, e) => fired = true;
			unsafeEvents.OnParentPackageUninstalled (null, null);

			Assert.IsTrue (fired);
		}

		[Test]
		public void ParentPackageUninstalled_UnsafeEventFiredAndInvokeRequired_ThreadSafeEventIsSafelyInvoked ()
		{
			CreateEvents ();
			threadSafeEvents.ParentPackageUninstalled += OnEventHandlerFired;
			var expectedPackage = new FakePackage ();

			unsafeEvents.OnParentPackageUninstalled (expectedPackage, null);

			Assert.IsTrue (isGuiSyncDispatchCalled);
		}

		[Test]
		public void ParentPackageUninstalled_UnsafeEventFiredAndInvokeRequiredButNoEventHandlerRegistered_ThreadSafeEventIsNotInvoked ()
		{
			CreateEvents ();
			unsafeEvents.OnParentPackageUninstalled (new FakePackage (), null);

			Assert.IsFalse (isGuiSyncDispatchCalled);
		}

		[Test]
		public void ParentPackageUninstalled_UnsafeEventFiredAfterEventHandlerRemoved_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.ParentPackageUninstalled += OnEventHandlerFired;
			threadSafeEvents.ParentPackageUninstalled -= OnEventHandlerFired;
			unsafeEvents.OnParentPackageUninstalled (null, null);

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void PackageOperationMessageLogged_UnsafeEventFired_ThreadSafeEventFired ()
		{
			CreateEvents ();
			bool fired = false;
			threadSafeEvents.PackageOperationMessageLogged += (sender, e) => fired = true;
			unsafeEvents.OnPackageOperationMessageLogged (MessageLevel.Info, String.Empty, new object[0]);

			Assert.IsTrue (fired);
		}

		[Test]
		public void PackageOperationMessageLogged_UnsafeEventFiredAfterEventHandlerRemoved_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.PackageOperationMessageLogged += OnEventHandlerFired;
			threadSafeEvents.PackageOperationMessageLogged -= OnEventHandlerFired;
			unsafeEvents.OnPackageOperationMessageLogged (MessageLevel.Info, String.Empty, new object[0]);

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void Dispose_PackageOperationsStartingHandlerExistsAndThreadUnsafeEventFiredAfterDispose_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.PackageOperationsStarting += OnEventHandlerFired;

			threadSafeEvents.Dispose ();
			unsafeEvents.OnPackageOperationsStarting ();

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void Dispose_PackageOperationErrorHandlerExistsAndThreadUnsafeEventFiredAfterDispose_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.PackageOperationError += OnEventHandlerFired;

			threadSafeEvents.Dispose ();
			unsafeEvents.OnPackageOperationError (new Exception ());

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void Dispose_ParentPackageInstalledHandlerExistsAndThreadUnsafeEventFiredAfterDispose_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.ParentPackageInstalled += OnEventHandlerFired;

			threadSafeEvents.Dispose ();
			unsafeEvents.OnParentPackageInstalled (new FakePackage (), null);

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void Dispose_ParentParentPackageUninstalledHandlerExistsAndThreadUnsafeEventFiredAfterDispose_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.ParentPackageUninstalled += OnEventHandlerFired;

			threadSafeEvents.Dispose ();
			unsafeEvents.OnParentPackageUninstalled (new FakePackage (), null);

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void OnSelectProjects_NoInvokeRequired_NonThreadSafeOnSelectProjectsMethodCalled ()
		{
			CreateEvents ();
			IEnumerable<IPackageManagementSelectedProject> selectedProjects = null;
			unsafeEvents.SelectProjects += (sender, e) => selectedProjects = e.SelectedProjects;
			var expectedSelectedProjects = new List<IPackageManagementSelectedProject> ();

			threadSafeEvents.OnSelectProjects (expectedSelectedProjects);

			Assert.AreEqual (expectedSelectedProjects, selectedProjects);
		}

		[Test]
		public void OnSelectLicenses_NoInvokeRequired_NonThreadSafeOnSelectProjectsMethodCalledAndReturnsResult ()
		{
			CreateEvents ();
			unsafeEvents.SelectProjects += (sender, e) => e.IsAccepted = true;
			var projects = new List<IPackageManagementSelectedProject> ();
			bool result = threadSafeEvents.OnSelectProjects (projects);

			Assert.IsTrue (result);
		}

		[Test]
		public void SelectProjects_UnsafeEventFired_ThreadSafeEventFired ()
		{
			CreateEvents ();
			bool fired = false;
			threadSafeEvents.SelectProjects += (sender, e) => fired = true;
			var projects = new List<IPackageManagementSelectedProject> ();
			unsafeEvents.OnSelectProjects (projects);

			Assert.IsTrue (fired);
		}

		[Test]
		public void SelectProjects_UnsafeEventFiredAfterEventHandlerRemoved_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.SelectProjects += OnEventHandlerFired;
			threadSafeEvents.SelectProjects -= OnEventHandlerFired;
			var projects = new List<IPackageManagementSelectedProject> ();
			unsafeEvents.OnSelectProjects (projects);

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void OnResolveFileConflict_NoInvokeRequired_NonThreadSafeOnResolveFileConflictMethodCalledWithMessage ()
		{
			CreateEvents ();
			string message = null;
			unsafeEvents.ResolveFileConflict += (sender, e) => message = e.Message.ToString ();

			threadSafeEvents.OnResolveFileConflict ("message");

			Assert.AreEqual ("message", message);
		}

		[Test]
		public void OnResolveFileConflict_NoInvokeRequired_ValueReturnedFromNonThreadSafeOnResolveFileConflict ()
		{
			CreateEvents ();
			unsafeEvents.ResolveFileConflict += (sender, e) => {
				e.Resolution = FileConflictResolution.OverwriteAll;
			};

			FileConflictResolution result = threadSafeEvents.OnResolveFileConflict ("message");

			Assert.AreEqual (FileConflictResolution.OverwriteAll, result);
		}

		[Test]
		public void OnResolveFileConflict_UnsafeEventFired_ThreadSafeEventFired ()
		{
			CreateEvents ();
			bool fired = false;
			threadSafeEvents.ResolveFileConflict += (sender, e) => fired = true;
			unsafeEvents.OnResolveFileConflict ("message");

			Assert.IsTrue (fired);
		}

		[Test]
		public void ResolveFileConflict_UnsafeEventFired_ThreadSafeEventFired ()
		{
			CreateEvents ();
			bool fired = false;
			threadSafeEvents.ResolveFileConflict += (sender, e) => fired = true;
			unsafeEvents.OnResolveFileConflict ("message");

			Assert.IsTrue (fired);
		}

		[Test]
		public void ResolveFileConflict_UnsafeEventFiredAfterEventHandlerRemoved_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.ResolveFileConflict += OnEventHandlerFired;
			threadSafeEvents.ResolveFileConflict -= OnEventHandlerFired;
			unsafeEvents.OnResolveFileConflict ("message");

			Assert.IsFalse (eventHandlerFired);
		}

		[Test]
		public void OnParentPackagesUpdated_NoInvokeRequired_NonThreadSafeOnParentPackagesUpdatedMethodCalled ()
		{
			CreateEvents ();
			IEnumerable<IPackage> packages = null;
			unsafeEvents.ParentPackagesUpdated += (sender, e) => packages = e.Packages;
			var expectedPackages = new FakePackage[] { new FakePackage () };

			threadSafeEvents.OnParentPackagesUpdated (expectedPackages);

			Assert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void ParentPackagesUpdated_UnsafeEventFired_ThreadSafeEventFired ()
		{
			CreateEvents ();
			bool fired = false;
			threadSafeEvents.ParentPackagesUpdated += (sender, e) => fired = true;
			unsafeEvents.OnParentPackagesUpdated (null);

			Assert.IsTrue (fired);
		}

		[Test]
		public void ParentPackagesUpdated_UnsafeEventFiredAndInvokeRequired_ThreadSafeEventIsSafelyInvoked ()
		{
			CreateEvents ();
			threadSafeEvents.ParentPackagesUpdated += OnEventHandlerFired;
			var expectedPackages = new FakePackage[] { new FakePackage () };

			unsafeEvents.OnParentPackagesUpdated (expectedPackages);

			Assert.IsTrue (isGuiSyncDispatchCalled);
		}

		[Test]
		public void ParentPackagesUpdated_UnsafeEventFiredAndInvokeRequiredButNoEventHandlerRegistered_ThreadSafeEventIsNotInvoked ()
		{
			CreateEvents ();
			var packages = new FakePackage[] { new FakePackage () };
			unsafeEvents.OnParentPackagesUpdated (packages);

			Assert.IsFalse (isGuiSyncDispatchCalled);
		}

		[Test]
		public void Dispose_ParentPackagesUpdatedHandlerExistsAndThreadUnsafeEventFiredAfterDispose_ThreadSafeEventIsNotFired ()
		{
			CreateEvents ();
			eventHandlerFired = false;
			threadSafeEvents.ParentPackagesUpdated += OnEventHandlerFired;
			threadSafeEvents.Dispose ();

			unsafeEvents.OnParentPackagesUpdated (new FakePackage[0]);

			Assert.IsFalse (eventHandlerFired);
		}
	}
}

