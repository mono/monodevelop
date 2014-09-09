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
using System.Runtime.Versioning;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using NuGet;

namespace MonoDevelop.PackageManagement.NodeBuilders
{
	public class PackageReferenceNode
	{
		public PackageReferenceNode (
			PackageReference packageReference,
			bool installed,
			bool pending = false,
			IPackageName updatedPackage = null)
		{
			PackageReference = packageReference;
			Installed = installed;
			IsInstallPending = pending;

			UpdatedVersion = GetUpdatedPackageVersion (updatedPackage);
			IsReinstallNeeded = packageReference.RequireReinstallation;
		}

		SemanticVersion GetUpdatedPackageVersion (IPackageName updatedPackage)
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

		public string Name {
			get { return PackageReference.Id; }
		}

		public string Id {
			get { return PackageReference.Id; }
		}

		public SemanticVersion Version {
			get { return PackageReference.Version; }
		}

		public SemanticVersion UpdatedVersion { get; private set; }

		public bool IsDevelopmentDependency {
			get { return PackageReference.IsDevelopmentDependency; }
		}

		public FrameworkName TargetFramework {
			get { return PackageReference.TargetFramework; }
		}

		public IVersionSpec VersionConstraint {
			get { return PackageReference.VersionConstraint; }
		}

		public string GetLabel ()
		{
			if (UpdatedVersion != null) {
				return Id + GetUpdatedVersionLabelText ();
			}
			if (IsInstallPending) {
				return Id + GetInstallingLabelText ();
			}
			return Id;
		}

		string GetUpdatedVersionLabelText ()
		{
			return String.Format (" <span color='grey'>({0} {1})</span>",
				UpdatedVersion,
				GettextCatalog.GetString ("available"));
		}

		string GetInstallingLabelText ()
		{
			return String.Format (" ({0})", GettextCatalog.GetString ("installing"));
		}

		public IconId GetIconId ()
		{
			return Stock.Reference;
		}

		public string GetPackageVersionLabel ()
		{
			return GettextCatalog.GetString ("Version {0}", Version);
		}

		public TaskSeverity? GetStatusSeverity ()
		{
			if (!Installed || IsReinstallNeeded) {
				if (!IsInstallPending) {
					return TaskSeverity.Warning;
				}
			}

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
			}
			return null;
		}

		public bool IsDisabled ()
		{
			return (!Installed || IsInstallPending);
		}
	}
}

