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
using MonoDevelop.Ide.Tasks;

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
			node = new PackageReferenceNode (null, packageReference, installed, installPending, updatedPackage);
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
			string secondaryLabel = node.GetSecondaryLabel ();

			Assert.AreEqual ("MyPackage", label);
			Assert.AreEqual (String.Empty, secondaryLabel);
		}

		[Test]
		public void GetLabel_PackageReferenceIsNotInstalled_ReturnsPackageId ()
		{
			CreatePackageReference (packageId: "MyPackage");
			CreatePackageReferenceNode (installed: false);

			string label = node.GetLabel ();
			string secondaryLabel = node.GetSecondaryLabel ();

			Assert.AreEqual ("MyPackage", label);
			Assert.AreEqual (String.Empty, secondaryLabel);
		}

		[Test]
		public void GetLabel_PackageReferenceNeedsReinstallation_ReturnsPackageId ()
		{
			CreatePackageReference (packageId: "MyPackage", requireReinstallation: true);
			CreatePackageReferenceNode (installed: true);

			string label = node.GetLabel ();
			string secondaryLabel = node.GetSecondaryLabel ();

			Assert.AreEqual ("MyPackage", label);
			Assert.AreEqual (String.Empty, secondaryLabel);
		}

		[Test]
		public void GetLabel_PackageReferenceIsPendingInstall_ReturnsPackageIdFollowedByInstallingText ()
		{
			CreatePackageReference (packageId: "MyPackage");
			CreatePackageReferenceNode (installed: false, installPending: true);

			string label = node.GetLabel ();
			string secondaryLabel = node.GetSecondaryLabel ();

			Assert.AreEqual ("MyPackage", label);
			Assert.AreEqual ("(installing)", secondaryLabel);
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
		public void GetIconId_PackageReferenceNeedsReinstallation_ReturnsReferenceIcon ()
		{
			CreatePackageReference (requireReinstallation: true);
			CreatePackageReferenceNode ();

			IconId icon = node.GetIconId ();

			Assert.AreEqual (Stock.Reference, icon);
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
		public void GetLabel_PackageReferenceNeedsReinstallationButHasUpdate_ReturnsPackageIdInBlackTextAndUpdatedPackageVersionInGreySpan ()
		{
			CreatePackageReference (
				packageId: "MyPackage",
				requireReinstallation: true);
			CreatePackageReferenceNode (
				installed: true,
				updatedPackage: new PackageName ("MyPackage", new SemanticVersion ("1.2.3.4")));

			string label = node.GetLabel ();
			string secondaryLabel = node.GetSecondaryLabel ();

			Assert.AreEqual ("MyPackage", label);
			Assert.AreEqual ("(1.2.3.4 available)", secondaryLabel);
		}

		[Test]
		public void IsDisabled_PackageReferenceHasInstallPending_ReturnsTrue ()
		{
			CreatePackageReference ();
			CreatePackageReferenceNode (installed: false, installPending: true);

			bool result = node.IsDisabled ();

			Assert.IsTrue (result);
		}

		[Test]
		public void IsDisabled_PackageReferenceIsInstalled_ReturnsFalse ()
		{
			CreatePackageReference ();
			CreatePackageReferenceNode ();

			bool result = node.IsDisabled ();

			Assert.IsFalse (result);
		}

		[Test]
		public void IsDisabled_PackageReferenceIsNotInstalled_ReturnsTrue ()
		{
			CreatePackageReference (packageId: "MyPackage");
			CreatePackageReferenceNode (installed: false);

			bool result = node.IsDisabled ();

			Assert.IsTrue (result);
		}

		[Test]
		public void GetStatusSeverity_PackageReferenceIsInstalled_ReturnsNull ()
		{
			CreatePackageReference ();
			CreatePackageReferenceNode ();

			TaskSeverity? status = node.GetStatusSeverity ();

			Assert.IsNull (status);
		}

		[Test]
		public void GetStatusSeverity_PackageReferenceIsNotInstalled_ReturnsWarning ()
		{
			CreatePackageReference (packageId: "MyPackage");
			CreatePackageReferenceNode (installed: false);

			TaskSeverity? status = node.GetStatusSeverity ();

			Assert.AreEqual (TaskSeverity.Warning, status);
		}

		[Test]
		public void GetStatusSeverity_PackageReferenceNeedsReinstallation_ReturnsWarning ()
		{
			CreatePackageReference (requireReinstallation: true);
			CreatePackageReferenceNode ();

			TaskSeverity? status = node.GetStatusSeverity ();

			Assert.AreEqual (TaskSeverity.Warning, status);
		}

		[Test]
		public void GetStatusSeverity_PackageReferenceHasInstallPending_ReturnsNull ()
		{
			CreatePackageReference ();
			CreatePackageReferenceNode (installed: false, installPending: true);

			TaskSeverity? status = node.GetStatusSeverity ();

			Assert.IsNull (status);
		}

		[Test]
		public void GetStatusMessage_PackageReferenceIsInstalled_ReturnsNull ()
		{
			CreatePackageReference ();
			CreatePackageReferenceNode ();

			string message = node.GetStatusMessage ();

			Assert.IsNull (message);
		}

		[Test]
		public void GetStatusMessage_PackageReferenceIsNotInstalled_ReturnsPackageNotRestoredMessage ()
		{
			CreatePackageReference (packageId: "MyPackage");
			CreatePackageReferenceNode (installed: false);

			string message = node.GetStatusMessage ();

			Assert.AreEqual ("Package is not restored", message);
		}

		[Test]
		public void GetStatusMessage_PackageReferenceNeedsReinstallation_ReturnsPackageNeedsRetargetingMessage ()
		{
			CreatePackageReference (requireReinstallation: true);
			CreatePackageReferenceNode ();

			string message = node.GetStatusMessage ();

			Assert.AreEqual ("Package needs retargeting", message);
		}

		[Test]
		public void GetStatusMessage_PackageReferenceHasInstallPending_ReturnsNull ()
		{
			CreatePackageReference ();
			CreatePackageReferenceNode (installed: false, installPending: true);

			string message = node.GetStatusMessage ();

			Assert.IsNull (message);
		}
	}
}

