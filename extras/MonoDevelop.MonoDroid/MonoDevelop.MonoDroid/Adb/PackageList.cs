// 
// PackageList.cs
//  
// Author:
//       Jonathan Pobst (monkey@jpobst.com)
// 
// Copyright (c) 2011 Novell, Inc.
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

// COPIED FROM ANDROIDVS AND MODIFIED

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoDevelop.MonoDroid
{
	public class PackageList
	{
		private string runtimeName = "Mono.Android.DebugRuntime";
		private string oldRuntimeName = "com.novell.monodroid.runtimeservice";
		private string platformName = "Mono.Android.Platform.ApiLevel_{0}";

		public List<InstalledPackage> Packages { get; private set; }

		public PackageList ()
		{
			Packages = new List<InstalledPackage> ();
		}

		public bool IsCurrentRuntimeInstalled (int version)
		{
			var packages = Packages.Where (p => p.Name == runtimeName && p.Version == version);

			return packages.Count () > 0;
		}

		public bool IsUnkownRuntimeInstalled ()
		{
			var packages = Packages.Where (p => p.Name == runtimeName && p.Version == int.MaxValue);

			return packages.Count () > 0;
		}

		public List<InstalledPackage> GetOldRuntimes (int current)
		{
			var packages = Packages.Where (p =>
				(p.Name == runtimeName && p.Version < current) ||
				p.Name == oldRuntimeName
			);

			return packages.ToList ();
		}

		public bool IsCurrentPlatformInstalled (int platform, int version)
		{
			string name = string.Format (platformName, platform);

			var packages = Packages.Where (p => p.Name == name && p.Version == version);

			return packages.Count () > 0;
		}

		public bool IsUnkownPlatformInstalled (int platform)
		{
			string name = string.Format (platformName, platform);

			var packages = Packages.Where (p => p.Name == name && p.Version == int.MaxValue);

			return packages.Count () > 0;
		}

		// Hopefully they don't have multiple old
		// platforms installed, but just in case...
		public List<InstalledPackage> GetOldPlatforms (int platform, int current)
		{
			string name = string.Format (platformName, platform);

			var packages = Packages.Where (p => p.Name == name && p.Version < current);

			return packages.ToList ();
		}

		public List<InstalledPackage> GetOldRuntimesAndPlatforms (int platform, int current)
		{
			var runtimes = GetOldRuntimes (current);

			runtimes.AddRange (GetOldPlatforms (platform, current));

			return runtimes;
		}

		public bool ContainsPackage (string packageName)
		{
			var packages = Packages.Where (p => p.Name == packageName);

			return packages.Count () > 0;
		}
	}
}
