//
// ProjectRetargetingUtility.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// based on NuGet src/VsEvents/ProjectRetargetingUtility.cs
//
// Copyright (c) 2010-2014 Outercurve Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet
{
	internal static class ProjectRetargetingUtility
	{
		/// <remarks>
		/// 1. Using the project instance, check if packages.config file exists. If NOT, RETURN empty list
		/// 2. If it does, get the full path of packages.config and create a PackageReferenceFile instance of packages.config
		/// 3. Get the list of PackageReference objects using PackageReferenceFile. Each PackageReference contains the package ID, version and the targetFramework
		/// 4. Now, for every PackageReference object, the IPackage instance for the INSTALLED PACKAGE can be obtained using LocalRepository.FindPackage(string packageid)
		///    4.a) Note that the LocalRepository is obtained from IVsPackageManager created using the global service of IVsPackageManagerFactory
		/// 5. Before iterating over the list of PackageReferences and obtaining IPackages, Get the NEW targetframework of the project
		/// 6. Now, for every packagereference from the list obtained in Step-3, determine if it needs to be reinstalled using the following information in Step 7
		///    6.a) CURRENT target framework of the project
		///    6.b) target framework of the package
		///    6.c) Files of the package obtained via IPackage.GetFiles()
		/// 7. Get Compatible items for old and new targetframework using IPackage.GetFiles() AND compare the lists. If they are not the same, that package needs to be reinstalled
		/// </remarks>
		internal static bool ShouldPackageBeReinstalled (FrameworkName newProjectFramework, FrameworkName oldProjectFramework, IPackage package)
		{
			Debug.Assert (newProjectFramework != null);
			Debug.Assert (oldProjectFramework != null);
			Debug.Assert (package != null);

			var packageFiles = package.GetFiles ().ToList ();
			IEnumerable<IPackageFile> oldProjectFrameworkCompatibleItems;
			IEnumerable<IPackageFile> newProjectFrameworkCompatibleItems;

			bool result = VersionUtility.TryGetCompatibleItems (oldProjectFramework, packageFiles, out oldProjectFrameworkCompatibleItems);

			if (!result) {
				// If the package is NOT compatible with oldProjectFramework, suggest reinstalling the package
				return true;
			}

			result = VersionUtility.TryGetCompatibleItems (newProjectFramework, packageFiles, out newProjectFrameworkCompatibleItems);

			if (!result) {
				// If the package is compatible with oldProjectFramework and not the newTargetFramework, suggest reinstaling the package
				return true;
			}

			return !Enumerable.SequenceEqual<IPackageFile> (oldProjectFrameworkCompatibleItems, newProjectFrameworkCompatibleItems);
		}
	}
}

