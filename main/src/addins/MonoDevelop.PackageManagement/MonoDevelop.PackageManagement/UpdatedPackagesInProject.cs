//
// UpdatedPackagesInProject.cs
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
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class UpdatedPackagesInProject
	{
		List<IPackageName> packages;

		public UpdatedPackagesInProject (IDotNetProject project)
			: this (project, Enumerable.Empty<IPackageName> ())
		{
		}

		public UpdatedPackagesInProject (
			IDotNetProject project,
			IEnumerable<IPackageName> packages)
		{
			Project = project;
			this.packages = packages.ToList ();
		}

		public IDotNetProject Project { get; private set; }

		public IEnumerable<IPackageName> GetPackages ()
		{
			return packages;
		}

		public bool AnyPackages ()
		{
			return packages.Any ();
		}

		public IPackageName GetUpdatedPackage (string packageId)
		{
			return packages
				.FirstOrDefault (package => package.Id == packageId);
		}

		public void RemovePackage (IPackageName package)
		{
			int index = packages.FindIndex (existingPackageName => existingPackageName.Id == package.Id);
			if (index >= 0) {
				packages.RemoveAt (index);
			}
		}

		public void RemoveUpdatedPackages (IEnumerable<PackageReference> packageReferences)
		{
			foreach (PackageReference packageReference in packageReferences) {
				IPackageName package = packages.Find (existingPackageName => existingPackageName.Id == packageReference.Id);
				if ((package != null) && (package.Version <= packageReference.Version)) {
					packages.Remove (package);
				}
			}
		}
	}
}

