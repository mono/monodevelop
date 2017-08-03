//
// ProjectPackageReference.cs
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

using System;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NuGet.Frameworks;

namespace MonoDevelop.PackageManagement
{
	[ExportProjectItemType ("PackageReference")]
	class ProjectPackageReference : ProjectItem
	{
		public PackageReference CreatePackageReference ()
		{
			var version = GetVersion ();
			var identity = GetPackageIdentity (version);
			var framework = GetFramework ();
			return CreatePackageReference (identity, version, framework);
		}

		PackageReference CreatePackageReference (
			PackageIdentity identity,
			VersionRange version,
			NuGetFramework framework)
		{
			if (!version.IsFloating)
				version = null;

			return new PackageReference (
				identity, framework,
				userInstalled: true,
				developmentDependency: false,
				requireReinstallation: false,
				allowedVersions: version);
		}

		PackageIdentity GetPackageIdentity (VersionRange version)
		{
			return new PackageIdentity (Include, version.MinVersion);
		}

		VersionRange GetVersion ()
		{
			string version = Metadata.GetValue ("Version");
			return VersionRange.Parse (version);
		}

		NuGetFramework GetFramework ()
		{
			string framework = Project.GetDotNetCoreTargetFrameworks ().FirstOrDefault ();
			if (framework != null)
				return NuGetFramework.Parse (framework);

			return NuGetFramework.UnsupportedFramework;
		}

		public bool Equals (PackageIdentity packageIdentity, bool matchVersion = true)
		{
			var packageReference = CreatePackageReference ();
			var currentPackageIdentity = packageReference.PackageIdentity;
			if (matchVersion)
				return packageIdentity.Equals (currentPackageIdentity);

			return StringComparer.OrdinalIgnoreCase.Equals (packageIdentity.Id, currentPackageIdentity.Id);
		}

		public static ProjectPackageReference Create (PackageIdentity packageIdentity)
		{
			return Create (packageIdentity.Id, packageIdentity.Version.ToNormalizedString ());
		}

		internal static ProjectPackageReference Create (string packageId, string version)
		{
			var packageReference = new ProjectPackageReference {
				Include = packageId
			};

			packageReference.Metadata.SetValue ("Version", version);

			return packageReference;
		}

		public static ProjectPackageReference Create (IMSBuildItemEvaluated evaluatedItem)
		{
			var packageReference = Create (
				evaluatedItem.Include,
				evaluatedItem.Metadata.GetValue ("Version")
			);

			foreach (IMSBuildPropertyEvaluated property in evaluatedItem.Metadata.GetProperties ()) {
				packageReference.Metadata.SetValue (property.Name, property.Value);
			}

			packageReference.IsImplicit = evaluatedItem.Metadata.GetValue<bool> ("IsImplicitlyDefined");

			return packageReference;
		}

		public override string ToString ()
		{
			return string.Format ("[PackageReference: {0} {1}]", Include, Metadata.GetValue ("Version"));
		}

		public static void AddKnownItemAttributes (MSBuildProject project)
		{
			project.AddKnownItemAttribute ("PackageReference", "Version");
		}

		public bool IsImplicit { get; internal set; }
	}
}
