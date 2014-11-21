// 
// RecentPackageRepository.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
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
	public class RecentPackageRepository : IRecentPackageRepository
	{
		public const int DefaultMaximumPackagesCount = 20;
		
		List<IPackage> packages = new List<IPackage>();
		int maximumPackagesCount = DefaultMaximumPackagesCount;
		IList<RecentPackageInfo> savedRecentPackages;
		IPackageRepository aggregateRepository;

		public RecentPackageRepository(
			IList<RecentPackageInfo> recentPackages,
			IPackageRepository aggregateRepository)
		{
			this.savedRecentPackages = recentPackages;
			this.aggregateRepository = aggregateRepository;

			//UpdatePackages ();
		}
		
		public string Source {
			get { return "RecentPackages"; }
		}
		
		public void AddPackage(IPackage package)
		{
			RemovePackageIfAlreadyAdded(package);
			AddPackageAtBeginning(package);
			RemoveLastPackageIfCurrentPackageCountExceedsMaximum();
			//UpdateRecentPackagesInOptions();
		}
		
		void RemovePackageIfAlreadyAdded(IPackage package)
		{
			int index = FindPackage(package);
			if (index >= 0) {
				packages.RemoveAt(index);
			}
		}
		
		int FindPackage(IPackage package)
		{
			return packages.FindIndex(p => PackageEqualityComparer.IdAndVersion.Equals(package, p));
		}
		
		void AddPackageAtBeginning(IPackage package)
		{
			packages.Insert(0, package);
		}
		
		void RemoveLastPackageIfCurrentPackageCountExceedsMaximum()
		{
			if (packages.Count > maximumPackagesCount) {
				RemoveLastPackage();
			}
		}

		void RemoveLastPackage()
		{
			packages.RemoveAt(packages.Count - 1);
		}
		
		void UpdateRecentPackagesInOptions()
		{
			savedRecentPackages.Clear();
			savedRecentPackages.AddRange(GetRecentPackagesInfo());
		}
		
		List<RecentPackageInfo> GetRecentPackagesInfo()
		{
			List<RecentPackageInfo> allRecentPackages = new List<RecentPackageInfo>();
			foreach (IPackage package in packages) {
				var recentPackageInfo = new RecentPackageInfo(package);
				allRecentPackages.Add(recentPackageInfo);
			}
			return allRecentPackages;
		}
		
		public void RemovePackage(IPackage package)
		{
		}
		
		public IQueryable<IPackage> GetPackages()
		{
			//UpdatePackages();
			RemoveInvalidPackages ();
			return packages.AsQueryable();
		}
		
		void UpdatePackages()
		{
			if (!HasRecentPackagesBeenRead() && HasRecentPackages) {
				IEnumerable<IPackage> recentPackages = GetRecentPackages();
				packages.AddRange(recentPackages);
			}
		}

		void RemoveInvalidPackages ()
		{
			packages.RemoveAll (package => !IsValidPackage (package));
		}

		static bool IsValidPackage (IPackage package)
		{
			var packageFromRepository = package as IPackageFromRepository;
			return (packageFromRepository != null) && packageFromRepository.IsValid;
		}
		
		bool HasRecentPackagesBeenRead()
		{
			return packages.Count > 0;
		}
		
		public bool HasRecentPackages {
			get { return savedRecentPackages.Count > 0; }
		}
		
		IEnumerable<IPackage> GetRecentPackages()
		{
			IEnumerable<IPackage> recentPackages = GetRecentPackagesFilteredById();
			return GetRecentPackagesFilteredByVersion(recentPackages);
		}
		
		IEnumerable<IPackage> GetRecentPackagesFilteredById()
		{
			IEnumerable<string> recentPackageIds = GetRecentPackageIds();
			return aggregateRepository.FindPackages(recentPackageIds);
		}
				
		IEnumerable<string> GetRecentPackageIds()
		{
			foreach (RecentPackageInfo recentPackageInfo in savedRecentPackages) {
				yield return recentPackageInfo.Id;
			}
		}
		
		IEnumerable<IPackage> GetRecentPackagesFilteredByVersion(IEnumerable<IPackage> recentPackages)
		{
			List<IPackage> filteredRecentPackages = new List<IPackage>();
			foreach (IPackage recentPackage in recentPackages) {
				foreach (RecentPackageInfo savedRecentPackageInfo in savedRecentPackages) {
					if (savedRecentPackageInfo.IsMatch(recentPackage)) {
						filteredRecentPackages.Add(recentPackage);
					}
				}
			}
			return filteredRecentPackages;
		}
		
		public int MaximumPackagesCount {
			get { return maximumPackagesCount; }
			set { maximumPackagesCount = value; }
		}
		
		public void Clear()
		{
			packages.Clear();
			UpdateRecentPackagesInOptions();
		}
		
		public bool SupportsPrereleasePackages {
			get { return false; }
		}

		public PackageSaveModes PackageSaveMode { get; set; }
	}
}
