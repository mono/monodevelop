//
// PackageRestorer.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using ICSharpCode.PackageManagement;
using MonoDevelop.Projects;
using System.Collections.Generic;
using MonoDevelop.Core;
using NuGet;
using MonoDevelop.PackageManagement.Commands;

namespace MonoDevelop.PackageManagement
{
	public class PackageRestorer
	{
		List<ProjectPackageReferenceFile> packageReferenceFiles;
		IDotNetProject singleProject;

		public PackageRestorer (Solution solution)
			: this (solution.GetAllDotNetProjects ())
		{
		}

		public PackageRestorer (DotNetProject project)
			: this (new [] { project })
		{
			singleProject = new DotNetProjectProxy (project);
		}

		public PackageRestorer (IEnumerable<DotNetProject> projects)
		{
			packageReferenceFiles = FindAllPackageReferenceFiles (projects).ToList ();
		}

		IEnumerable<ProjectPackageReferenceFile> FindAllPackageReferenceFiles (IEnumerable<DotNetProject> projects)
		{
			return projects
				.Where (project => project.HasPackages ())
				.Select (project => new ProjectPackageReferenceFile (project));
		}

		public bool RestoreFailed { get; private set; }

		public void Restore ()
		{
			Restore (ProgressMonitorStatusMessageFactory.CreateRestoringPackagesInSolutionMessage ());
		}

		public void Restore (ProgressMonitorStatusMessage progressMessage)
		{
			try {
				if (AnyMissingPackages ()) {
					RestoreWithProgressMonitor (progressMessage);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Package restore failed", ex);
				RestoreFailed = true;
			}
		}

		bool AnyMissingPackages ()
		{
			return packageReferenceFiles.Any (file => file.AnyMissingPackages ());
		}

		void RestoreWithProgressMonitor (ProgressMonitorStatusMessage progressMessage)
		{
			var runner = new PackageRestoreRunner ();
			if (singleProject != null) {
				runner.Run (singleProject, progressMessage);
			} else {
				runner.Run (progressMessage);
			}
			RestoreFailed = runner.RestoreFailed;
		}
	}
}

