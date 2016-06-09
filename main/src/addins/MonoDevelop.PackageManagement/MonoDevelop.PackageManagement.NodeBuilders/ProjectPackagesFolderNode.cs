//
// ProjectPackagesFolderNode.cs
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using NuGet;

namespace MonoDevelop.PackageManagement.NodeBuilders
{
	internal class ProjectPackagesFolderNode
	{
		IDotNetProject project;
		IUpdatedPackagesInSolution updatedPackagesInSolution;
		List<PackageReference> packageReferences;

		public ProjectPackagesFolderNode (DotNetProject project)
			: this (new DotNetProjectProxy (project), PackageManagementServices.UpdatedPackagesInSolution)
		{
		}

		public ProjectPackagesFolderNode (
			IDotNetProject project,
			IUpdatedPackagesInSolution updatedPackagesInSolution)
		{
			this.project = project;
			this.updatedPackagesInSolution = updatedPackagesInSolution;
		}

		public DotNetProject DotNetProject {
			get { return project.DotNetProject; }
		}

		internal IDotNetProject Project {
			get { return project; }
		}

		public IconId Icon {
			get { return Stock.OpenReferenceFolder; }
		}

		public IconId ClosedIcon {
			get { return Stock.ClosedReferenceFolder; }
		}

		public string GetLabel ()
		{
			return GettextCatalog.GetString ("Packages");
		}

		public string GetSecondaryLabel ()
		{
			int count = GetUpdatedPackagesCount ();
			if (count == 0) {
				return String.Empty;
			}

			return GetUpdatedPackagesCountLabel (count);
		}

		string GetUpdatedPackagesCountLabel (int count)
		{
			return string.Format ("({0} {1})", count, GetUpdateText (count));
		}

		string GetUpdateText (int count)
		{
			if (count > 1) {
				return GettextCatalog.GetString ("updates");
			}
			return GettextCatalog.GetString ("update");
		}

		int GetUpdatedPackagesCount ()
		{
			return updatedPackagesInSolution
				.GetUpdatedPackages (project)
				.GetPackages ()
				.Count ();
		}

		IEnumerable<PackageReference> PackageReferences {
			get {
				if (packageReferences == null) {
					packageReferences = GetPackageReferences ().ToList ();
				}
				return packageReferences;
			}
		}

		protected virtual IEnumerable<PackageReference> GetPackageReferences ()
		{
			if (project.HasPackages ()) {
				var packageReferenceFile = new PackageReferenceFile (project.GetPackagesConfigFilePath ());
				return packageReferenceFile.GetPackageReferences ();
			}
			return new PackageReference [0];
		}

		public IEnumerable<PackageReferenceNode> GetPackageReferencesNodes ()
		{
			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project);
			return PackageReferences.Select (reference => CreatePackageReferenceNode (reference, updatedPackages));
		}

		PackageReferenceNode CreatePackageReferenceNode (PackageReference reference, UpdatedPackagesInProject updatedPackages)
		{
			return new PackageReferenceNode (
				this,
				reference,
				IsPackageInstalled (reference),
				false,
				updatedPackages.GetUpdatedPackage (reference.Id));
		}

		protected virtual bool IsPackageInstalled (PackageReference reference)
		{
			return reference.IsPackageInstalled (project.DotNetProject);
		}

		public void ClearPackageReferences ()
		{
			packageReferences = null;
		}
	}
}

