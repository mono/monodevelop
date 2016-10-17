//
// DotNetProjectPackagingExtension.cs
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Packaging
{
	class DotNetProjectPackagingExtension : DotNetProjectExtension
	{
		public bool InstallBuildPackagingNuGetAfterWrite { get; set; }

		// Used by unit tests only.
		internal bool GetRequiresMSBuild ()
		{
			return RequiresMicrosoftBuild;
		}

		protected override void OnReadProjectHeader (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnReadProjectHeader (monitor, msproject);

			UpdateRequiresMSBuildSetting (msproject);
		}

		protected override void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnWriteProject (monitor, msproject);

			UpdateRequiresMSBuildSetting (msproject, true);

			if (InstallBuildPackagingNuGetAfterWrite) {
				InstallBuildPackagingNuGetAfterWrite = false;
				Project.InstallBuildPackagingNuGetPackage ();
			}
		}

		void UpdateRequiresMSBuildSetting (MSBuildProject msproject, bool reloadProjectBuilder = false)
		{
			if (!RequiresMicrosoftBuild) {
				RequiresMicrosoftBuild = msproject.HasNuGetMetadata ();
				if (reloadProjectBuilder && RequiresMicrosoftBuild) {
					Project.ReloadProjectBuilder ();
					EnsureReferencedProjectsRequireMSBuild (reloadProjectBuilder);
				}
			}
		}

		protected override void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			base.OnReferenceAddedToProject (e);

			if (Project.Loading)
				return;

			if (RequiresMicrosoftBuild && e.ProjectReference.ReferenceType == ReferenceType.Project) {
				EnsureReferencedProjectsRequireMSBuild (true);
			}
		}

		protected override void OnItemReady ()
		{
			if (RequiresMicrosoftBuild) {
				EnsureReferencedProjectsRequireMSBuild ();
			}
		}

		internal void EnsureReferencedProjectsRequireMSBuild (bool reloadProjectBuilder = false)
		{
			if (Project.ParentSolution == null)
				return;

			try {
				foreach (var reference in Project.References.Where (projectReference => projectReference.ReferenceType == ReferenceType.Project)) {
					var referencedProject = reference.ResolveProject (Project.ParentSolution);
					if (referencedProject != null) {
						var flavor = referencedProject.GetFlavor<DotNetProjectPackagingExtension> ();
						if (flavor?.RequiresMicrosoftBuild == false) {
							flavor.RequiresMicrosoftBuild = true;
							flavor.EnsureReferencedProjectsRequireMSBuild (reloadProjectBuilder);
							if (reloadProjectBuilder)
								referencedProject.ReloadProjectBuilder ();
						}
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to update RequiresMicrosoftBuild.", ex);
			}
		}
	}
}
