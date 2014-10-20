// 
// UpdatedPackages.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;

using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class UpdatedPackages
	{
		IPackageRepository sourceRepository;
		List<IPackageName> installedPackages;
		IPackageConstraintProvider constraintProvider;

		public UpdatedPackages (
			IPackageManagementProject project,
			IPackageRepository aggregateRepository)
			: this (
				project.GetPackageReferences (),
				aggregateRepository,
				project.ConstraintProvider)
		{
		}

		public UpdatedPackages (
			IEnumerable<PackageReference> packageReferences,
			IPackageRepository aggregrateRepository,
			IPackageConstraintProvider constraintProvider)
		{
			installedPackages = packageReferences
				.Select (packageReference => new PackageName (packageReference.Id, packageReference.Version))
				.Select (packageReference => (IPackageName)packageReference)
				.ToList ();

			this.sourceRepository = aggregrateRepository;
			this.constraintProvider = constraintProvider;
		}

		public UpdatedPackages(
			IQueryable<IPackage> installedPackages,
			IPackageRepository aggregrateRepository)
		{
		}

		public string SearchTerms { get; set; }

		public IEnumerable<IPackage> GetUpdatedPackages (bool includePrerelease = false)
		{
			List<IPackageName> localPackages = installedPackages;
			IEnumerable<IPackageName> distinctLocalPackages = DistinctPackages (localPackages);
			return GetUpdatedPackages (distinctLocalPackages, includePrerelease);
		}

		/// <summary>
		/// If we have jQuery 1.6 and 1.7 then return just jquery 1.6
		/// </summary>
		IEnumerable<IPackageName> DistinctPackages (List<IPackageName> packages)
		{
			if (packages.Any ()) {
				packages.Sort ((x, y) => x.Version.CompareTo (y.Version));
				return packages.Distinct<IPackageName> (PackageEqualityComparer.Id).ToList ();
			}
			return packages;
		}

		IEnumerable<IPackage> GetUpdatedPackages (
			IEnumerable<IPackageName> localPackages,
			bool includePrelease)
		{
			IEnumerable<IVersionSpec> constraints = localPackages
				.Select (package => constraintProvider.GetConstraint (package.Id));

			return sourceRepository.GetUpdates (
				localPackages,
				includePrelease,
				false,
				null,
				constraints);
		}
	}
}
