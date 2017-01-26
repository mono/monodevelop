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
			var identity = GetPackageIdentity ();
			var framework = GetFramework ();
			return new PackageReference (identity, framework);
		}

		PackageIdentity GetPackageIdentity ()
		{
			var version = GetVersion ();
			return new PackageIdentity (Include, version);
		}

		NuGetVersion GetVersion ()
		{
			string version = Metadata.GetValue ("Version");
			return new NuGetVersion (version);
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
			var currentPackageIdentity = GetPackageIdentity ();
			if (matchVersion)
				return packageIdentity.Equals (currentPackageIdentity);

			return StringComparer.OrdinalIgnoreCase.Equals (packageIdentity.Id, currentPackageIdentity.Id);
		}

		public static ProjectPackageReference Create (PackageIdentity packageIdentity)
		{
			var packageReference = new ProjectPackageReference {
				Include = packageIdentity.Id
			};

			packageReference.Metadata.SetValue ("Version", packageIdentity.Version.ToNormalizedString ());

			return packageReference;
		}

		internal static ProjectPackageReference Create (string packageId, string version)
		{
			var package = new PackageIdentity (packageId, new NuGetVersion (version));
			return Create (package);
		}

		public static ProjectPackageReference Create (IMSBuildItemEvaluated evaluatedItem)
		{
			return Create (
				evaluatedItem.Include,
				evaluatedItem.Metadata.GetValue ("Version")
			);
		}

		public override string ToString ()
		{
			return string.Format ("[PackageReference: {0} {1}]", Include, Metadata.GetValue ("Version"));
		}
	}
}
