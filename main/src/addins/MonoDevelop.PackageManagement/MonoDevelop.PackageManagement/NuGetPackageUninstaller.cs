//
// NuGetPackageUninstaller.cs
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

using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects.MSBuild;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class NuGetPackageUninstaller
	{
		IDotNetProject project;
		PackagePathResolver packagePathResolver;
		PackageIdentity packageIdentity;

		public NuGetPackageUninstaller (IDotNetProject project, PackagePathResolver packagePathResolver)
		{
			this.project = project;
			this.packagePathResolver = packagePathResolver;
		}

		public async Task ForceUninstall (string packageId, NuGetVersion packageVersion)
		{
			packageIdentity = new PackageIdentity (packageId, packageVersion);

			FilePath installPath = packagePathResolver.GetInstallPath (packageIdentity);
			RemoveReferences (installPath);

			using (var importRemover = new NuGetPackageForcedImportsRemover (installPath)) {
				await project.SaveAsync ();
			}
		}

		void RemoveReferences (FilePath installPath)
		{
			var references = project.References
				.Where (reference => reference.HintPath.IsNotNull)
				.Where (reference => reference.HintPath.IsChildPathOf (installPath))
				.ToList ();

			if (references.Any ()) {
				project.References.RemoveRange (references);
			}
		}
	}
}

