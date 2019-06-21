//
// PackageReferenceNode.cs
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
using MonoDevelop.Ide.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.NodeBuilders
{
	internal class PackageReferenceNode
	{
		public PackageReferenceNode (
			ProjectPackagesFolderNode parentNode,
			PackageReference packageReference,
			bool installed,
			bool pending = false,
			PackageIdentity updatedPackage = null)
		{
			ParentNode = parentNode;
			PackageReference = packageReference;
			Installed = installed;
			IsInstallPending = pending;

			UpdatedVersion = GetUpdatedPackageVersion (updatedPackage);
			IsReinstallNeeded = packageReference.RequireReinstallation;
		}

		NuGetVersion GetUpdatedPackageVersion (PackageIdentity updatedPackage)
		{
			if (updatedPackage != null) {
				return updatedPackage.Version;
			}
			return null;
		}

		public PackageReference PackageReference { get; private set; }
		public bool Installed { get; private set; }
		public bool IsInstallPending { get; private set; }
		public bool IsReinstallNeeded { get; private set; }

		ProjectPackagesFolderNode ParentNode { get; set; }

		public IDotNetProject Project {
			get { return ParentNode.Project; }
		}

		public string Name {
			get { return Id; }
		}

		public string Id {
			get { return PackageReference.PackageIdentity.Id; }
		}

		public NuGetVersion Version {
			get { return PackageReference.PackageIdentity.Version; }
		}

		public NuGetVersion UpdatedVersion { get; private set; }

		public bool IsDevelopmentDependency {
			get { return PackageReference.IsDevelopmentDependency; }
		}

		public NuGetFramework TargetFramework {
			get { return PackageReference.TargetFramework; }
		}

		public VersionRange VersionConstraint {
			get { return PackageReference.AllowedVersions; }
		}

		public string GetLabel ()
		{
			return Id;
		}

		public string GetSecondaryLabel ()
		{
			if (IsInstallPending) {
				return GetInstallingLabelText ();
			}

			string version = GetPackageDisplayVersion (displayNone: false);
			if (string.IsNullOrEmpty (version)) {
				return string.Empty;
			}

			return string.Format ("({0})", version);
		}

		string GetInstallingLabelText ()
		{
			string version = GetPackageDisplayVersion (displayNone: false);
			if (string.IsNullOrEmpty (version)) {
				return GettextCatalog.GetString ("(installing)");
			}
			return GettextCatalog.GetString ("({0} installing)", version);
		}

		public IconId GetIconId ()
		{
			return Stock.Reference;
		}

		public IconId GetStatusIconId ()
		{
			if (IsInstallPending || !Installed || IsReinstallNeeded || UpdatedVersion == null)
				return IconId.Null;

			return new IconId ("md-package-update");
		}

		public string GetPackageDisplayVersion (bool displayNone = true)
		{
			if (PackageReference.IsFloating ()) {
				return PackageReference.AllowedVersions.Float.ToString ();
			}
			if (Version == null) {
				if (displayNone) {
					return GettextCatalog.GetString ("None");
				}
				return string.Empty;
			}
			return Version.ToString ();
		}

		public TaskSeverity? GetStatusSeverity ()
		{
			if (!Installed || IsReinstallNeeded) {
				if (!IsInstallPending) {
					return TaskSeverity.Warning;
				}
			}

			if (UpdatedVersion != null)
				return TaskSeverity.Information;

			return null;
		}

		public string GetStatusMessage ()
		{
			if (IsInstallPending) {
				return null;
			} else if (!Installed) {
				return GettextCatalog.GetString ("Package is not restored");
			} else if (IsReinstallNeeded) {
				return GettextCatalog.GetString ("Package needs retargeting");
			} else if (UpdatedVersion != null) {
				return GettextCatalog.GetString ("{0} available", UpdatedVersion);
			}
			return null;
		}

		public bool IsDisabled ()
		{
			return (!Installed || IsInstallPending);
		}

		public bool IsReleaseVersion ()
		{
			return !PackageReference.PackageIdentity.Version.IsPrerelease;
		}

		public bool NeedsRestoreBeforeUninstall ()
		{
			return !ParentNode.IsNuGetIntegratedProject () &&
				!ParentNode.IsPackageInstalled (PackageReference);
		}

		public string GetUpdateLabel ()
		{
			if (UpdatedVersion != null)
				return GettextCatalog.GetString ("Update to {0}", UpdatedVersion);

			return GettextCatalog.GetString ("Update");
		}
	}
}

