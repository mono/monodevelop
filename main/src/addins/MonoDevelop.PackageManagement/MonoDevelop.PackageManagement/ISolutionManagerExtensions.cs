//
// ISolutionManagerExtensions.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.Versioning;
using NuGet.Packaging;

namespace MonoDevelop.PackageManagement
{
	static class ISolutionManagerExtensions
	{
		/// <summary>
		/// Returns all the versions of the NuGet package used in all projects in the solution
		/// ordered by latest version first.
		/// </summary>
		public static async Task<IEnumerable<NuGetVersion>> GetInstalledVersions (
			this ISolutionManager solutionManager,
			string packageId,
			CancellationToken token = default (CancellationToken))
		{
			var versions = new List<NuGetVersion> ();

			foreach (NuGetProject project in solutionManager.GetNuGetProjects ()) {
				var packages = await project.GetInstalledPackagesAsync (token);
				versions.AddRange (packages.Where (p => IsMatch (p, packageId))
					.Select (p => p.PackageIdentity.Version));
			}

			return versions.Distinct ().OrderByDescending (version => version);
		}

		public static async Task<IEnumerable<IDotNetProject>> GetProjectsWithInstalledPackage (
			this ISolutionManager solutionManager,
			string packageId,
			string version,
			CancellationToken token = default (CancellationToken))
		{
			var nugetVersion = new NuGetVersion (version);

			var projects = new List<IDotNetProject> ();

			foreach (NuGetProject project in solutionManager.GetNuGetProjects ()) {
				var packages = await project.GetInstalledPackagesAsync (token);

				if (packages.Any (p => IsMatch (p, packageId, nugetVersion)))
					projects.Add (project.GetDotNetProject ());
			}

			return projects;
		}

		static bool IsMatch (PackageReference package, string packageId, NuGetVersion version = null)
		{
			if (!StringComparer.OrdinalIgnoreCase.Equals (package.PackageIdentity.Id, packageId))
				return false;

			if (version != null)
				return package.PackageIdentity.Version == version;

			return true;
		}
	}
}
