//
// PackageCompatibility.cs
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
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace MonoDevelop.PackageManagement
{
	internal class PackageCompatibility
	{
		PackageIdentity package;
		string packageFileName;
		IEnumerable<FrameworkSpecificGroup> libItemGroups;
		IEnumerable<FrameworkSpecificGroup> referenceItemGroups;
		IEnumerable<FrameworkSpecificGroup> frameworkReferenceGroups;		IEnumerable<FrameworkSpecificGroup> contentFileGroups;
		IEnumerable<FrameworkSpecificGroup> buildFileGroups;
		IEnumerable<FrameworkSpecificGroup> toolItemGroups;
		NuGetFramework newProjectTargetFramework;
		NuGetFramework oldProjectTargetFramework;

		public PackageCompatibility (
			NuGetFramework projectTargetFramework,
			PackageReference packageReference,
			string packageFileName)
		{
			oldProjectTargetFramework = packageReference.TargetFramework;
			newProjectTargetFramework = projectTargetFramework;
			package = packageReference.PackageIdentity;
			this.packageFileName = packageFileName;
		}

		public void CheckCompatibility ()
		{
			using (var packageArchiveReader = GetPackageFiles ()) {

				IsCompatibleWithNewProjectTargetFramework = true;

				CheckCompatibilityWithNewProjectTargetFramework ();

				ClearUp ();
			}
		}

		public PackageIdentity Package {
			get { return package; }
		}

		public bool ShouldReinstallPackage { get; private set; }
		public bool IsCompatibleWithNewProjectTargetFramework { get; private set; }

		IPackageFilesReader GetPackageFiles ()
		{
			var packageReader = CreatePackageFilesReader (packageFileName);
			libItemGroups = packageReader.GetLibItems ();
			referenceItemGroups = packageReader.GetReferenceItems ();
			frameworkReferenceGroups = packageReader.GetFrameworkItems ();
			contentFileGroups = packageReader.GetContentItems ();
			buildFileGroups = packageReader.GetBuildItems ();
			toolItemGroups = packageReader.GetToolItems ();
			return packageReader;
		}

		protected virtual IPackageFilesReader CreatePackageFilesReader (string fileName)
		{
			return new PackageFilesReader (fileName);
		}

		void CheckCompatibilityWithNewProjectTargetFramework ()
		{
			CheckCompatibility (libItemGroups);

			if (!ShouldReinstallPackage)
				CheckCompatibility (referenceItemGroups);

			if (!ShouldReinstallPackage)
				CheckCompatibility (frameworkReferenceGroups);

			if (!ShouldReinstallPackage)
				CheckCompatibility (contentFileGroups);

			if (!ShouldReinstallPackage)
				CheckCompatibility (buildFileGroups);

			if (!ShouldReinstallPackage)
				CheckCompatibility (toolItemGroups);
		}

		void CheckCompatibility (IEnumerable<FrameworkSpecificGroup> items)
		{
			if (!items.Any ())
				return;

			var newNearestFramework = NuGetFrameworkUtility.GetNearest (items, newProjectTargetFramework);
			var oldNearestFramework = NuGetFrameworkUtility.GetNearest (items, oldProjectTargetFramework);

			if (newNearestFramework != null && oldNearestFramework != null) {
				ShouldReinstallPackage = !newNearestFramework.Equals (oldNearestFramework);
			} else if (newNearestFramework == null && oldNearestFramework == null) {
				// Compatible.
			} else {
				ShouldReinstallPackage = true;
				IsCompatibleWithNewProjectTargetFramework = false;
			}
		}

		void ClearUp ()
		{
			libItemGroups = null;
			referenceItemGroups = null;
			frameworkReferenceGroups = null;
			contentFileGroups = null;
			buildFileGroups = null;
			toolItemGroups = null;
		}
	}
}

