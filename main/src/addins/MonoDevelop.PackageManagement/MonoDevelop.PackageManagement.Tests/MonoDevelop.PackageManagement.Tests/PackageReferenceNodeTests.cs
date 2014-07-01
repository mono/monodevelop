//
// PackageReferenceNodeTests.cs
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
using MonoDevelop.PackageManagement.NodeBuilders;
using NUnit.Framework;
using NuGet;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageReferenceNodeTests
	{
		PackageReferenceNode node;
		PackageReference packageReference;

		void CreatePackageReferenceNode (
			bool installed = true,
			bool installPending = false,
			PackageName updatedPackage = null)
		{
			node = new PackageReferenceNode (packageReference, installed, installPending, updatedPackage);
		}

		void CreatePackageReference (
			string packageId = "Id",
			bool requireReinstallation = false)
		{
			var version = new SemanticVersion ("1.2.3");
			packageReference = new PackageReference (packageId, version, null, null, false, requireReinstallation);
		}

		[Test]
		public void IsReinstallNeeded_PackageReferenceDoesNotRequireReinstall_ReturnsFalse ()
		{
			CreatePackageReference (requireReinstallation: false);
			CreatePackageReferenceNode ();

			Assert.IsFalse (node.IsReinstallNeeded);
		}

		[Test]
		public void IsReinstallNeeded_PackageReferenceDoesRequireReinstall_ReturnsTrue ()
		{
			CreatePackageReference (requireReinstallation: true);
			CreatePackageReferenceNode ();

			Assert.IsTrue (node.IsReinstallNeeded);
		}

		[Test]
		public void GetLabel_PackageReferenceIsInstalled_ReturnsPackageId ()
		{
			CreatePackageReference (packageId: "MyPackage");
			CreatePackageReferenceNode ();

			string label = node.GetLabel ();

			Assert.AreEqual ("MyPackage", label);
		}

		[Test]
		public void GetLabel_PackageReferenceIsNotInstalled_ReturnsPackageIdInsideErrorColouredSpan ()
		{
			CreatePackageReference (packageId: "MyPackage");
			CreatePackageReferenceNode (installed: false);

			string label = node.GetLabel ();

			Assert.AreEqual ("<span color='#c99c00'>MyPackage</span>", label);
		}

		[Test]
		public void GetLabel_PackageReferenceNeedsReinstallation_ReturnsPackageIdInsideErrorColouredSpan ()
		{
			CreatePackageReference (packageId: "MyPackage", requireReinstallation: true);
			CreatePackageReferenceNode (installed: true);

			string label = node.GetLabel ();

			Assert.AreEqual ("<span color='#c99c00'>MyPackage</span>", label);
		}

		[Test]
		public void GetIconId_PackageReferenceIsInstalled_ReturnsReferenceIcon ()
		{
			CreatePackageReference ();
			CreatePackageReferenceNode ();

			IconId icon = node.GetIconId ();

			Assert.AreEqual (Stock.Reference, icon);
		}

		[Test]
		public void GetIconId_PackageReferenceNeedsReinstallation_ReturnsReferenceWarningIcon ()
		{
			CreatePackageReference (requireReinstallation: true);
			CreatePackageReferenceNode ();

			IconId icon = node.GetIconId ();

			Assert.AreEqual (Stock.ReferenceWarning, icon);
		}

		[Test]
		public void GetIconId_PackageReferenceHasInstallPending_ReturnsReferenceIcon ()
		{
			CreatePackageReference ();
			CreatePackageReferenceNode (installed: false, installPending: true);

			IconId icon = node.GetIconId ();

			Assert.AreEqual (Stock.Reference, icon);
		}

		[Test]
		public void GetLabel_PackageReferenceNeedsReinstallationButHasUpdate_ReturnsPackageIdInsideErrorColouredSpanAndUpdatedPackageVersionInGreySpan ()
		{
			CreatePackageReference (
				packageId: "MyPackage",
				requireReinstallation: true);
			CreatePackageReferenceNode (
				installed: true,
				updatedPackage: new PackageName ("MyPackage", new SemanticVersion ("1.2.3.4")));

			string label = node.GetLabel ();

			Assert.AreEqual ("<span color='#c99c00'>MyPackage</span> <span color='grey'>(1.2.3.4 available)</span>", label);
		}
	}
}

