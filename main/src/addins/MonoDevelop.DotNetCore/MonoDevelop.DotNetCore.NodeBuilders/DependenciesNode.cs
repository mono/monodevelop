//
// DependenciesNode.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using NuGet.Packaging;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class DependenciesNode
	{
		public static readonly string NodeName = "Dependencies";

		readonly IUpdatedNuGetPackagesInWorkspace updatedPackagesInWorkspace;

		public DependenciesNode (DotNetProject project)
			: this (project, PackageManagementServices.UpdatedPackagesInWorkspace)
		{
		}

		public DependenciesNode (DotNetProject project, IUpdatedNuGetPackagesInWorkspace updatedPackagesInWorkspace)
		{
			Project = project;
			this.updatedPackagesInWorkspace = updatedPackagesInWorkspace;
			PackageDependencyCache = new PackageDependencyNodeCache (Project);
		}

		internal DotNetProject Project { get; private set; }
		internal PackageDependencyNodeCache PackageDependencyCache { get; private set; }

		public bool LoadedDependencies {
			get { return PackageDependencyCache.LoadedDependencies; }
		}

		public string GetLabel ()
		{
			return GettextCatalog.GetString ("Dependencies");
		}

		public string GetSecondaryLabel ()
		{
			int count = GetUpdatedPackagesCount ();
			if (count == 0) {
				return string.Empty;
			}

			return GetUpdatedPackagesCountLabel (count);
		}

		string GetUpdatedPackagesCountLabel (int count)
		{
			return GettextCatalog.GetPluralString ("({0} update)", "({0} updates)", count, count);
		}

		int GetUpdatedPackagesCount ()
		{
			UpdatedNuGetPackagesInProject updatedPackages = GetUpdatedPackages ();
			updatedPackages.RemoveUpdatedPackages (GetPackageReferences ());

			return updatedPackages.GetPackages ().Count ();
		}

		public IEnumerable<object> GetChildNodes ()
		{
			if (LoadedDependencies) {
				return GetLoadedDependencyNodes ();
			} else {
				return GetDefaultChildNodes ();
			}
		}

		IEnumerable<object> GetLoadedDependencyNodes ()
		{
			var frameworkNodes = GetTargetFrameworkNodes ().ToList ();
			if (frameworkNodes.Count > 1) {
				return frameworkNodes;
			} else if (frameworkNodes.Any ()) {
				return GetChildNodes (frameworkNodes [0]);
			} else {
				return GetDefaultChildNodes ();
			}
		}

		IEnumerable<object> GetDefaultChildNodes ()
		{
			return GetChildNodes (null);
		}

		internal IEnumerable<object> GetChildNodes (TargetFrameworkNode frameworkNode)
		{
			if (frameworkNode != null) {
				var packagesNode = new PackageDependenciesNode (frameworkNode);
				if (packagesNode.HasChildNodes ())
					yield return packagesNode;

				var sdkNode = new SdkDependenciesNode (frameworkNode);
				if (sdkNode.HasChildNodes ())
					yield return sdkNode;
			} else {
				var packagesNode = new PackageDependenciesNode (this);
				if (packagesNode.HasChildNodes ())
					yield return packagesNode;

				var sdkNode = new SdkDependenciesNode (this);
				if (sdkNode.HasChildNodes ())
					yield return sdkNode;
			}

			var assembliesNode = new AssemblyDependenciesNode (Project);
			if (assembliesNode.HasChildNodes ())
				yield return assembliesNode;

			var projectsNode = new ProjectDependenciesNode (Project);
			if (projectsNode.HasChildNodes ())
				yield return projectsNode;
		}

		public IconId Icon {
			get { return Stock.OpenReferenceFolder; }
		}

		public IconId ClosedIcon {
			get { return Stock.ClosedReferenceFolder; }
		}

		public IEnumerable<TargetFrameworkNode> GetTargetFrameworkNodes ()
		{
			return PackageDependencyCache.GetTargetFrameworkNodes (this);
		}

		public IEnumerable<PackageDependencyNode> GetProjectPackageReferencesAsDependencyNodes ()
		{
			return PackageDependencyCache.GetProjectPackageReferencesAsDependencyNodes (this);
		}

		public UpdatedNuGetPackagesInProject GetUpdatedPackages ()
		{
			return updatedPackagesInWorkspace.GetUpdatedPackages (new DotNetProjectProxy (Project));
		}

		IEnumerable<PackageReference> GetPackageReferences ()
		{
			foreach (ProjectPackageReference packageReference in Project.Items.OfType<ProjectPackageReference> ()) {
				if (packageReference.Metadata.GetValue ("IsImplicitlyDefined", false)) {
					// Ignore. Microsoft.AspNetCore.App package reference gets the version from an MSBuild import
					// so should not show updates. Currently this does not handle the case where the
					// Microsoft.AspNetCore.App package is listed with a version in the project file so it will
					// not show updates.
				} else {
					yield return packageReference.CreatePackageReference ();
				}
			}
		}
	}
}
