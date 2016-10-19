//
// DotNetCoreNuGetProject.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	class DotNetCoreNuGetProject : NuGetProject, INuGetIntegratedProject
	{
		DotNetProject project;

		public DotNetCoreNuGetProject (DotNetProject project)
		{
			this.project = project;
		}

		public static NuGetProject Create (DotNetProject project)
		{
			if (project.HasDotNetCoreTargetFrameworkProperty ())
				return new DotNetCoreNuGetProject (project);

			return null;
		}

		public override Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync (CancellationToken token)
		{
			return Runtime.RunInMainThread<IEnumerable<PackageReference>> (() => {
				return GetPackageReferences ();
			});
		}

		IEnumerable<PackageReference> GetPackageReferences ()
		{
			return project.Items.OfType<ProjectPackageReference> ()
				.Select (projectItem => projectItem.CreatePackageReference ())
				.ToList ();
		}

		public override Task<bool> InstallPackageAsync (PackageIdentity packageIdentity, DownloadResourceResult downloadResourceResult, INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			throw new NotImplementedException ();
		}

		public override Task<bool> UninstallPackageAsync (PackageIdentity packageIdentity, INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			throw new NotImplementedException ();
		}
	}
}
