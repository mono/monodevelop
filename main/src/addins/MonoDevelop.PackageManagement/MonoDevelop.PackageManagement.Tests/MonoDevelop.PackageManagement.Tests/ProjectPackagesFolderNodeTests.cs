//
// ProjectPackagesFolderNodeTests.cs
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.PackageManagement.NodeBuilders;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ProjectPackagesFolderNodeTests
	{
		TestableProjectPackagesFolderNode packagesFolderNode;
		FakeUpdatedPackagesInWorkspace updatedPackagesInSolution;
		FakeDotNetProject project;

		void CreateNode ()
		{
			updatedPackagesInSolution = new FakeUpdatedPackagesInWorkspace ();
			project = new FakeDotNetProject ();
			packagesFolderNode = new TestableProjectPackagesFolderNode (project, updatedPackagesInSolution);
		}

		PackageReference AddPackageReferenceToProject (
			string packageId = "Id",
			string version = "1.2.3")
		{
			var semanticVersion = new NuGetVersion (version);
			var identity = new PackageIdentity (packageId, semanticVersion);
			var packageReference = new PackageReference (identity, null);
			packagesFolderNode.PackageReferences.Add (packageReference);
			return packageReference;
		}

		void AddUpdatedPackageForProject (string packageId, string version)
		{
			var packageName = new PackageIdentity (packageId, new NuGetVersion (version));
			updatedPackagesInSolution.AddUpdatedPackages (project, packageName);
		}

		void AddUpdatedPackagesForProject (string packageId1, string version1, string packageId2, string version2)
		{
			var packageName1 = new PackageIdentity (packageId1, new NuGetVersion (version1));
			var packageName2 = new PackageIdentity (packageId2, new NuGetVersion (version2));
			updatedPackagesInSolution.AddUpdatedPackages (project, packageName1, packageName2);
		}

		void NoUpdatedPackages ()
		{
			updatedPackagesInSolution.AddUpdatedPackages (project);
		}

		void PackageIsInstalledInProject (PackageReference packageReference)
		{
			packagesFolderNode.PackageReferencesWithPackageInstalled.Add (packageReference);
		}

		Task RefreshNodePackages ()
		{
			packagesFolderNode.RefreshPackages ();
			return packagesFolderNode.RefreshTaskCompletionSource.Task;
		}

		[Test]
		public async Task GetLabel_NoUpdatedPackages_ReturnsPackages ()
		{
			CreateNode ();
			NoUpdatedPackages ();
			await RefreshNodePackages ();

			string label = packagesFolderNode.GetLabel ();

			Assert.AreEqual ("Packages", label);
		}

		[Test]
		public void Icon_GetIconForDisplay_ReturnsOpenReferenceFolderIcon ()
		{
			CreateNode ();
			NoUpdatedPackages ();

			IconId icon = packagesFolderNode.Icon;

			Assert.AreEqual (Stock.OpenReferenceFolder, icon);
		}

		[Test]
		public void ClosedIcon_GetIconForDisplay_ReturnsClosedReferenceFolderIcon ()
		{
			CreateNode ();
			NoUpdatedPackages ();

			IconId icon = packagesFolderNode.ClosedIcon;

			Assert.AreEqual (Stock.ClosedReferenceFolder, icon);
		}

		[Test]
		public async Task GetLabel_OneUpdatedPackage_ReturnsPackagesWithCount ()
		{
			CreateNode ();
			AddPackageReferenceToProject ("MyPackage", "1.0");
			AddUpdatedPackageForProject ("MyPackage", "1.1");
			await RefreshNodePackages ();

			string label = packagesFolderNode.GetLabel ();

			Assert.AreEqual ("Packages <span color='grey'>(1 update)</span>", label);
		}

		[Test]
		public async Task GetLabel_TwoUpdatedPackages_ReturnsPackagesWithCount ()
		{
			CreateNode ();
			AddPackageReferenceToProject ("One", "1.0");
			AddPackageReferenceToProject ("Two", "1.0");
			AddUpdatedPackagesForProject ("One", "1.1", "Two", "1.3");
			await RefreshNodePackages ();

			string label = packagesFolderNode.GetLabel ();

			Assert.AreEqual ("Packages <span color='grey'>(2 updates)</span>", label);
		}

		[Test]
		public async Task GetPackageReferencesNodes_OnePackageReferenceButNoUpdatedPackages_ReturnsOneNode ()
		{
			CreateNode ();
			PackageReference packageReference = AddPackageReferenceToProject ("MyPackage", "1.0");
			PackageIsInstalledInProject (packageReference);
			NoUpdatedPackages ();
			await RefreshNodePackages ();

			List<PackageReferenceNode> nodes = packagesFolderNode.GetPackageReferencesNodes ().ToList ();

			PackageReferenceNode referenceNode = nodes.FirstOrDefault ();
			Assert.AreEqual (1, nodes.Count);
			Assert.IsTrue (referenceNode.Installed);
			Assert.AreEqual ("MyPackage", referenceNode.GetLabel ());
		}

		[Test]
		public async Task GetPackageReferencesNodes_OnePackageReferenceButPackageNotInstalledAndNoUpdatedPackages_ReturnsOneNode ()
		{
			CreateNode ();
			AddPackageReferenceToProject ("MyPackage", "1.0");
			NoUpdatedPackages ();
			await RefreshNodePackages ();

			List<PackageReferenceNode> nodes = packagesFolderNode.GetPackageReferencesNodes ().ToList ();

			PackageReferenceNode referenceNode = nodes.FirstOrDefault ();
			Assert.AreEqual (1, nodes.Count);
			Assert.IsFalse (referenceNode.Installed);
			Assert.AreEqual ("MyPackage", referenceNode.GetLabel ());
		}

		[Test]
		public async Task GetPackageReferencesNodes_OnePackageReferenceWithUpdatedPackages_ReturnsOneNodeWithUpdatedVersionInformationInLabel ()
		{
			CreateNode ();
			PackageReference packageReference = AddPackageReferenceToProject ("MyPackage", "1.0");
			PackageIsInstalledInProject (packageReference);
			AddUpdatedPackageForProject ("MyPackage", "1.2");
			await RefreshNodePackages ();

			List<PackageReferenceNode> nodes = packagesFolderNode.GetPackageReferencesNodes ().ToList ();

			PackageReferenceNode referenceNode = nodes.FirstOrDefault ();
			Assert.AreEqual (1, nodes.Count);
			Assert.AreEqual ("1.2", referenceNode.UpdatedVersion.ToString ());
			Assert.AreEqual ("MyPackage <span color='grey'>(1.2 available)</span>", referenceNode.GetLabel ());
		}

		[Test]
		public async Task GetPackageReferencesNodes_OnePackageReferenceWithUpdatedPackagesButPackageNotRestored_ReturnsOneNodeWithUpdatedVersionInformationInLabel ()
		{
			CreateNode ();
			AddPackageReferenceToProject ("MyPackage", "1.0");
			AddUpdatedPackageForProject ("MyPackage", "1.2");
			await RefreshNodePackages ();

			List<PackageReferenceNode> nodes = packagesFolderNode.GetPackageReferencesNodes ().ToList ();

			PackageReferenceNode referenceNode = nodes.FirstOrDefault ();
			Assert.AreEqual (1, nodes.Count);
			Assert.AreEqual ("1.2", referenceNode.UpdatedVersion.ToString ());
			Assert.AreEqual ("MyPackage <span color='grey'>(1.2 available)</span>", referenceNode.GetLabel ());
			Assert.AreEqual (Stock.Reference, referenceNode.GetIconId ());
			Assert.IsTrue (referenceNode.IsDisabled ());
		}

		[Test]
		public async Task GetLabel_OneUpdatedPackageButInstalledPackageNotReadFromProjectYet_PackageCountNotShownUntilInstalledPackagesAreRead ()
		{
			CreateNode ();
			AddPackageReferenceToProject ("MyPackage", "1.0");
			AddUpdatedPackageForProject ("MyPackage", "1.1");
			string labelBeforeInstalledPackagesRead = packagesFolderNode.GetLabel ();
			await RefreshNodePackages ();

			string labelAfterInstalledPackagesRead = packagesFolderNode.GetLabel ();

			Assert.AreEqual ("Packages", labelBeforeInstalledPackagesRead);
			Assert.AreEqual ("Packages <span color='grey'>(1 update)</span>", labelAfterInstalledPackagesRead);
		}
	}
}

