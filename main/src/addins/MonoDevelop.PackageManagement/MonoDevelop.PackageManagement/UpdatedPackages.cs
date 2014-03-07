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
		IQueryable<IPackage> installedPackages;
		
		public UpdatedPackages(
			IPackageManagementProject project,
			IPackageRepository aggregateRepository)
			: this(
				project.GetPackages(),
				aggregateRepository)
		{
		}
		
		public UpdatedPackages(
			IQueryable<IPackage> installedPackages,
			IPackageRepository aggregrateRepository)
		{
			this.installedPackages = installedPackages;
			this.sourceRepository = aggregrateRepository;
		}
		
		public string SearchTerms { get; set; }
		
		public IEnumerable<IPackage> GetUpdatedPackages(bool includePrerelease = false)
		{
			IQueryable<IPackage> localPackages = installedPackages;
			localPackages = FilterPackages(localPackages);
			IEnumerable<IPackage> distinctLocalPackages = DistinctPackages(localPackages);
			return GetUpdatedPackages(sourceRepository, distinctLocalPackages, includePrerelease);
		}
		
		IQueryable<IPackage> GetInstalledPackages()
		{
			return installedPackages;
		}
		
		IQueryable<IPackage> FilterPackages(IQueryable<IPackage> localPackages)
		{
			return localPackages.Find(SearchTerms);
		}
		
		/// <summary>
		/// If we have jQuery 1.6 and 1.7 then return just jquery 1.6
		/// </summary>
		IEnumerable<IPackage> DistinctPackages(IQueryable<IPackage> localPackages)
		{
			List<IPackage> packages = localPackages.ToList();
			if (packages.Any()) {
				packages.Sort(PackageComparer.Version);
				return packages.Distinct<IPackage>(PackageEqualityComparer.Id).ToList();
			}
			return packages;
		}
		
		IEnumerable<IPackage> GetUpdatedPackages(
			IPackageRepository sourceRepository,
			IEnumerable<IPackage> localPackages,
			bool includePrelease)
		{
			return sourceRepository.GetUpdates(localPackages, includePrelease, false);
		}
	}
}
