//
// MonoDevelopPackageSourceProvider.cs
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
using System.Collections.Generic;
using MonoDevelop.Core;
using NuGet.Configuration;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopPackageSourceProvider : IPackageSourceProvider
	{
		readonly PackageSourceProvider packageSourceProvider;

		public MonoDevelopPackageSourceProvider (ISettings settings)
		{
			packageSourceProvider = new PackageSourceProvider (settings);
		}

		public string ActivePackageSourceName {
			get { return packageSourceProvider.ActivePackageSourceName; }
		}

		public event EventHandler PackageSourcesChanged {
			add {
				packageSourceProvider.PackageSourcesChanged += value;
			}

			remove {
				packageSourceProvider.PackageSourcesChanged -= value;
			}
		}

		public void DisablePackageSource (PackageSource source)
		{
			packageSourceProvider.DisablePackageSource (source);
		}

		public bool IsPackageSourceEnabled (PackageSource source)
		{
			return packageSourceProvider.IsPackageSourceEnabled (source);
		}

		public IEnumerable<PackageSource> LoadPackageSources ()
		{
			bool atLeastOnePackageSource = false;

			foreach (PackageSource source in packageSourceProvider.LoadPackageSources ()) {
				atLeastOnePackageSource = true;
				yield return source;
			}

			if (!atLeastOnePackageSource) {
				yield return GetDefaultPackageSource ();
			}
		}

		public void SaveActivePackageSource (PackageSource source)
		{
			packageSourceProvider.SaveActivePackageSource (source);
		}

		public void SavePackageSources (IEnumerable<PackageSource> sources)
		{
			packageSourceProvider.SavePackageSources (sources);
		}

		static PackageSource GetDefaultPackageSource ()
		{
			return new PackageSource (
				NuGetConstants.V3FeedUrl,
				GettextCatalog.GetString ("Official NuGet Gallery")
			) {
				ProtocolVersion = 3
			};
		}
	}
}

