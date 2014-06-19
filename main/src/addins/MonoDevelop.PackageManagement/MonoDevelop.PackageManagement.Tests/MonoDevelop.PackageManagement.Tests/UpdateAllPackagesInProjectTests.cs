//
// UpdateAllPackagesInProjectTests.cs
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
using System.Linq;
using ICSharpCode.PackageManagement;
using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UpdateAllPackagesInProjectTests
	{
		UpdateAllPackagesInProject updateAllPackagesInProject;
		FakePackageManagementProject fakeProject;
		List<UpdatePackageAction> updateActions;

		void CreateUpdateAllPackagesInProject ()
		{
			fakeProject = new FakePackageManagementProject ();
			updateAllPackagesInProject = new UpdateAllPackagesInProject (fakeProject);
		}

		void AddPackageToProject (string packageId)
		{
			var package = new FakePackage (packageId, "1.0");
			fakeProject.FakePackagesInReverseDependencyOrder.Add (package);
		}

		void CallCreateActions ()
		{
			IEnumerable<UpdatePackageAction> actions = updateAllPackagesInProject.CreateActions ();
			updateActions = actions.ToList ();
		}

		UpdatePackageAction FirstUpdateAction {
			get { return updateActions [0]; }
		}

		[Test]
		public void CreateActions_OnePackageInProject_ReturnsOneAction ()
		{
			CreateUpdateAllPackagesInProject ();
			AddPackageToProject ("Test");
			CallCreateActions ();

			Assert.AreEqual (1, updateActions.Count);
		}

		[Test]
		public void CreateActions_OnePackageInProject_ActionCreatedFromProject ()
		{
			CreateUpdateAllPackagesInProject ();
			AddPackageToProject ("Test");
			CallCreateActions ();

			UpdatePackageAction action = updateActions [0];
			UpdatePackageAction expectedAction = fakeProject.FirstFakeUpdatePackageActionCreated;

			Assert.AreEqual (expectedAction, action);
		}

		[Test]
		public void CreateActions_OnePackageInProject_PackageIdSpecifiedInAction ()
		{
			CreateUpdateAllPackagesInProject ();
			AddPackageToProject ("Test");
			CallCreateActions ();

			string id = FirstUpdateAction.PackageId;

			Assert.AreEqual ("Test", id);
		}

		[Test]
		public void CreateActions_OnePackageInProject_PackageVersionSpecifiedInActionIsNull ()
		{
			CreateUpdateAllPackagesInProject ();
			AddPackageToProject ("Test");
			CallCreateActions ();

			SemanticVersion version = FirstUpdateAction.PackageVersion;

			Assert.IsNull (version);
		}

		[Test]
		public void CreateActions_NoPackagesInProject_ReturnsNoActions ()
		{
			CreateUpdateAllPackagesInProject ();
			CallCreateActions ();

			Assert.AreEqual (0, updateActions.Count);
		}

		[Test]
		public void CreateActions_TwoPackagesInProject_TwoUpdateActionsCreated ()
		{
			CreateUpdateAllPackagesInProject ();
			AddPackageToProject ("Test1");
			AddPackageToProject ("Test2");
			CallCreateActions ();

			Assert.AreEqual (2, updateActions.Count);
		}

		[Test]
		public void CreateActions_TwoPackagesInProject_TwoPackageIdsSpecifiedInActions ()
		{
			CreateUpdateAllPackagesInProject ();
			AddPackageToProject ("Test1");
			AddPackageToProject ("Test2");
			CallCreateActions ();

			Assert.AreEqual ("Test1", updateActions [0].PackageId);
			Assert.AreEqual ("Test2", updateActions [1].PackageId);
		}

		[Test]
		public void CreateActions_OnePackageInProject_UpdateIfPackageDoesNotExistInProjectIsFalse ()
		{
			CreateUpdateAllPackagesInProject ();
			AddPackageToProject ("Test");
			CallCreateActions ();

			bool update = FirstUpdateAction.UpdateIfPackageDoesNotExistInProject;

			Assert.IsFalse (update);
		}

		[Test]
		public void CreateActions_OnePackageInProjectAndUpdateDependenciesSetToFalse_ActionUpdateDependenciesIsFalse ()
		{
			CreateUpdateAllPackagesInProject ();
			AddPackageToProject ("Test");
			updateAllPackagesInProject.UpdateDependencies = false;

			CallCreateActions ();

			bool update = FirstUpdateAction.UpdateDependencies;

			Assert.IsFalse (update);
		}

		[Test]
		public void UpdateDependencies_NewInstance_ReturnsTrue ()
		{
			CreateUpdateAllPackagesInProject ();
			bool update = updateAllPackagesInProject.UpdateDependencies;

			Assert.IsTrue (update);
		}

		[Test]
		public void CreateActions_OnePackageInProjectAndAllowPrereleaseVersionsSetToFalse_ActionPrereleaseVersionsIsFalse ()
		{
			CreateUpdateAllPackagesInProject ();
			AddPackageToProject ("Test");
			updateAllPackagesInProject.AllowPrereleaseVersions = false;

			CallCreateActions ();

			bool allow = FirstUpdateAction.AllowPrereleaseVersions;

			Assert.IsFalse (allow);
		}

		[Test]
		public void CreateActions_OnePackageInProjectAndAllowPrereleaseVersionsSetToTrue_ActionPrereleaseVersionsIsTrue ()
		{
			CreateUpdateAllPackagesInProject ();
			AddPackageToProject ("Test");
			updateAllPackagesInProject.AllowPrereleaseVersions = true;

			CallCreateActions ();

			bool allow = FirstUpdateAction.AllowPrereleaseVersions;

			Assert.IsTrue (allow);
		}

		[Test]
		public void AllowPrereleaseVersions_NewInstance_ReturnsFalse ()
		{
			CreateUpdateAllPackagesInProject ();
			bool allow = updateAllPackagesInProject.AllowPrereleaseVersions;

			Assert.IsFalse (allow);
		}

		[Test]
		public void CreateActions_OnePackageInProjectAndUpdateDependenciesSetToTrue_ActionUpdateDependenciesIsTrue ()
		{
			CreateUpdateAllPackagesInProject ();
			AddPackageToProject ("Test");
			updateAllPackagesInProject.UpdateDependencies = true;

			CallCreateActions ();

			bool update = FirstUpdateAction.UpdateDependencies;

			Assert.IsTrue (update);
		}
	}
}


