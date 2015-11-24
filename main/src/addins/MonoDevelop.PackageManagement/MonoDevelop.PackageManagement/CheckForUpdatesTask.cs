//
// CheckForUpdatesTaskResult.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	public class CheckForUpdatesTask : IDisposable
	{
		UpdatedPackagesInSolution updatedPackagesInSolution;
		List<IPackageManagementProject> projects;
		List<UpdatedPackagesInProject> projectsWithUpdatedPackages = new List<UpdatedPackagesInProject> ();
		bool disposed;

		public CheckForUpdatesTask (
			UpdatedPackagesInSolution updatedPackagesInSolution,
			IEnumerable<IPackageManagementProject> projects)
		{
			this.updatedPackagesInSolution = updatedPackagesInSolution;
			this.projects = projects.ToList ();
		}

		public void CheckForUpdates ()
		{
			foreach (IPackageManagementProject project in projects) {

				UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.CheckForUpdates (project);

				if (disposed) {
					break;
				}

				if (updatedPackages.AnyPackages ()) {
					projectsWithUpdatedPackages.Add (updatedPackages);
				}
			}
		}

		public void CheckForUpdatesCompleted ()
		{
			updatedPackagesInSolution.CheckForUpdatesCompleted (this);
		}

		public IEnumerable<UpdatedPackagesInProject> ProjectsWithUpdatedPackages {
			get { return projectsWithUpdatedPackages; }
		}

		public void Dispose ()
		{
			if (!disposed) {
				disposed = true;
			}
		}
	}
}

