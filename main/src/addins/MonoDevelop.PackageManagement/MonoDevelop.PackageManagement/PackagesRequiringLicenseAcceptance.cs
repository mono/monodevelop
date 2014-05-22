//
// PackagesRequiringLicenseAcceptance.cs
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
using ICSharpCode.PackageManagement;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class PackagesRequiringLicenseAcceptance
	{
		IPackageManagementProject project;

		public PackagesRequiringLicenseAcceptance (IPackageManagementProject project)
		{
			this.project = project;
		}

		public IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance (IEnumerable<IPackageAction> actions)
		{
			var packages = new List <IPackage> ();
			foreach (IPackageAction action in actions) {
				packages.AddRange (GetPackagesRequiringLicenseAcceptance (action));
			}
			return packages.Distinct<IPackage> (PackageEqualityComparer.IdAndVersion);
		}

		IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance (IPackageAction action)
		{
			var processPackageActions = action as ProcessPackageOperationsAction;
			if (processPackageActions == null) {
				return new IPackage [0];
			}

			return processPackageActions.GetInstallOperations ()
				.Select (operation => operation.Package)
				.Where (package => PackageRequiresLicenseAcceptance (package))
				.ToList ();
		}

		bool PackageRequiresLicenseAcceptance (IPackage package)
		{
			return package.RequireLicenseAcceptance && !IsPackageInstalled (package);
		}

		bool IsPackageInstalled(IPackage package)
		{
			return project.IsPackageInstalled (package);
		}
	}
}

