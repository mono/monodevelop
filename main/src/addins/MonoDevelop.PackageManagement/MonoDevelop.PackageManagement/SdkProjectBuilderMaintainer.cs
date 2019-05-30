//
// SdkProjectBuilderMaintainer.cs
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement
{
	class SdkProjectBuilderMaintainer
	{
		/// <summary>
		/// If the target framework of the project has changed then non .NET Core projects
		/// that reference this project may need their project builders refreshed so
		/// that the correct build result occurs. If the new framework is incompatible
		/// then without the project builder refresh then the build error will not appear
		/// until the solution is reloaded.
		/// </summary>
		public static void OnProjectReload (ProjectReloadedEventArgs reloadEventArgs)
		{
			var reloadedProject = reloadEventArgs.NewProject.DotNetProject;
			foreach (var project in GetAllNonSdkProjectsReferencingProject (reloadedProject)) {
				project.ReloadProjectBuilder ();
			}
		}

		static IEnumerable<DotNetProject> GetAllNonSdkProjects (Solution parentSolution)
		{
			return parentSolution.GetAllDotNetProjects ()
				.Where (project => !project.HasFlavor<SdkProjectExtension> ());
		}

		static IEnumerable<DotNetProject> GetAllNonSdkProjectsReferencingProject (DotNetProject dotNetCoreProject)
		{
			foreach (DotNetProject project in GetAllNonSdkProjects (dotNetCoreProject.ParentSolution)) {
				foreach (ProjectReference projectReference in project.References) {
					if (projectReference.ReferenceType == ReferenceType.Project) {
						Project resolvedProject = projectReference.ResolveProject (project.ParentSolution);
						if (resolvedProject == dotNetCoreProject) {
							yield return project;
						}
					}
				}
			}
		}
	}
}
