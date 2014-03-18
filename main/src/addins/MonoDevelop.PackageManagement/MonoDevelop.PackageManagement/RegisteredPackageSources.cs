// 
// RegisteredPackgaeSources.cs
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
using System.Collections.ObjectModel;
using System.Linq;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class RegisteredPackageSources : ObservableCollection<PackageSource>
	{
		public static readonly string DefaultPackageSourceUrl = "https://www.nuget.org/api/v2/";
		public static readonly string DefaultPackageSourceName = "Official NuGet Gallery";
		
		public static readonly PackageSource DefaultPackageSource = 
			new PackageSource(DefaultPackageSourceUrl, DefaultPackageSourceName);

		public RegisteredPackageSources()
			: this(new PackageSource[0])
		{
		}

		public RegisteredPackageSources(IEnumerable<PackageSource> packageSources)
			: this(packageSources, DefaultPackageSource)
		{
		}
		
		public RegisteredPackageSources(
			IEnumerable<PackageSource> packageSources,
			PackageSource defaultPackageSource)
		{
			AddPackageSources(packageSources);
			AddDefaultPackageSourceIfNoRegisteredPackageSources(defaultPackageSource);
		}
		
		void AddPackageSources(IEnumerable<PackageSource> packageSources)
		{
			foreach (PackageSource source in packageSources) {
				Add(source);
			}
		}
		
		void AddDefaultPackageSourceIfNoRegisteredPackageSources(PackageSource defaultPackageSource)
		{
			if (HasNoRegisteredPackageSources) {
				Add(defaultPackageSource);
			}
		}
		
		bool HasNoRegisteredPackageSources {
			get { return Count == 0; }
		}
		
		public bool HasMultipleEnabledPackageSources {
			get { return GetEnabledPackageSources().Count() > 1; }
		}
		
		public IEnumerable<PackageSource> GetEnabledPackageSources()
		{
			return this.Where(packageSource => packageSource.IsEnabled);
		}
	}
}
