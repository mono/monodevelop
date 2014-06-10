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

using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.PackageManagement.NodeBuilders;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ProjectPackagesFolderNodeTests
	{
		TestableProjectPackagesFolderNode packagesFolderNode;
		FakeUpdatedPackagesInSolution updatedPackagesInSolution;
		FakeDotNetProject project;

		void CreateNode ()
		{
			updatedPackagesInSolution = new FakeUpdatedPackagesInSolution ();
			project = new FakeDotNetProject ();
			packagesFolderNode = new TestableProjectPackagesFolderNode (project, updatedPackagesInSolution);
		}

		PackageReference AddPackageReferenceToProject (
			string packageId = "Id",
			string version = "1.2.3")
		{
			var semanticVersion = new SemanticVersion (version);
			var packageReference = new PackageReference (packageId, semanticVersion, null, null, false, false);
			packagesFolderNode.PackageReferences.Add (packageReference);
			return packageReference;
		}

		void AddUpdatedPackageForProject (string packageId, string version)
		{
			var packageName = new PackageName (packageId, new SemanticVersion (version));
			updatedPackagesInSolution.AddUpdatedPackages (project, packageName);
		}

		void AddUpdatedPackagesForProject (string packageId1, string version1, string packageId2, string version2)
		{
			var packageName1 = new PackageName (packageId1, new SemanticVersion (version1));
			var packageName2 = new PackageName (packageId2, new SemanticVersion (version2));
			updatedPackagesInSolution.AddUpdatedPackages (project, packageName1, packageName2);
		}

		void NoUpdatedPackages ()
		{
			updatedPackagesInSolution.AddUpdatedPackages (project);
		}

		[Test]
		public void GetLabel_NoUpdatedPackages_ReturnsPackages ()
		{
			CreateNode ();
			NoUpdatedPackages ();

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
		public void GetLabel_OneUpdatedPackage_ReturnsPackagesWithCount ()
		{
			CreateNode ();
			AddPackageReferenceToProject ("MyPackage", "1.0");
			AddUpdatedPackageForProject ("MyPackage", "1.1");

			string label = packagesFolderNode.GetLabel ();

			Assert.AreEqual ("Packages <span color='grey'>(1 update)</span>", label);
		}

		[Test]
		public void GetLabel_TwoUpdatedPackages_ReturnsPackagesWithCount ()
		{
			CreateNode ();
			AddPackageReferenceToProject ("One", "1.0");
			AddPackageReferenceToProject ("Two", "1.0");
			AddUpdatedPackagesForProject ("One", "1.1", "Two", "1.3");

			string label = packagesFolderNode.GetLabel ();

			Assert.AreEqual ("Packages <span color='grey'>(2 updates)</span>", label);
		}
	}
}

