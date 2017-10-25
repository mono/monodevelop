//
// ProjectSystemReferencesReader.cs
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
//
// Based on src/NuGet.Clients/NuGet.PackageManagement.VisualStudio/ProjectServices/
// VsCoreProjectSystemReferenceReader.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;

namespace MonoDevelop.PackageManagement
{
	class ProjectSystemReferencesReader : IProjectSystemReferencesReader
	{
		DotNetProject project;

		public ProjectSystemReferencesReader (DotNetProject project)
		{
			this.project = project;
		}

		public Task<IEnumerable<LibraryDependency>> GetPackageReferencesAsync (
			NuGetFramework targetFramework,
			CancellationToken token)
		{
			throw new NotImplementedException ();
		}

		public Task<IEnumerable<ProjectRestoreReference>> GetProjectReferencesAsync (
			ILogger logger,
			CancellationToken token)
		{
			return Runtime.RunInMainThread (() => GetProjectReferences (logger));
		}

		internal IEnumerable<ProjectRestoreReference> GetProjectReferences (ILogger logger)
		{
			var results = new List<ProjectRestoreReference> ();

			// Verify ReferenceOutputAssembly
			var excludedProjects = GetExcludedReferences (project);
			bool hasMissingReferences = false;

			// find all references in the project
			foreach (var childReference in project.References) {
				try {
					if (childReference.ReferenceType != ReferenceType.Project) {
						continue;
					}

					if (!childReference.IsValid) {
						// Skip missing references and show a warning
						hasMissingReferences = true;
						continue;
					}

					// Skip missing references
					var sourceProject = childReference.ResolveProject (project.ParentSolution) as DotNetProject;

					// Skip missing references
					if (sourceProject != null) {
						string childProjectPath = sourceProject.FileName;

						// Skip projects which have ReferenceOutputAssembly=false
						if (!string.IsNullOrEmpty (childProjectPath) &&
							!excludedProjects.Contains (childProjectPath, StringComparer.OrdinalIgnoreCase)) {
							var restoreReference = new ProjectRestoreReference () {
								ProjectPath = childProjectPath,
								ProjectUniqueName = childProjectPath
							};

							results.Add (restoreReference);
						}
					}
				} catch (Exception ex) {
					// Exceptions are expected in some scenarios for native projects,
					// ignore them and show a warning
					hasMissingReferences = true;

					logger.LogDebug (ex.ToString ());

					LoggingService.LogError ("Unable to find project dependencies.", ex);
				}
			}

			if (hasMissingReferences) {
				// Log a warning message once per project
				// This warning contains only the names of the root project and the project with the
				// broken reference. Attempting to display more details on the actual reference
				// that has the problem may lead to another exception being thrown.
				var warning = GettextCatalog.GetString (
					"Failed to resolve all project references. The package restore result for '{0}' or a dependant project may be incomplete.",
					project.Name);

				logger.LogWarning (warning);
			}

			return results;
		}

		/// <summary>
		/// Get the unique names of all references which have ReferenceOutputAssembly set to false.
		/// </summary>
		static List<string> GetExcludedReferences (DotNetProject project)
		{
			var excludedReferences = new List<string> ();

			foreach (var reference in project.References) {
				// 1. Verify that this is a project reference
				// 2. Check that it is valid and resolved
				// 3. Follow the reference to the DotNetProject and get the unique name
				if (!reference.ReferenceOutputAssembly &&
					reference.IsValid &&
					reference.ReferenceType == ReferenceType.Project) {

					var sourceProject = reference.ResolveProject (project.ParentSolution);
					if (sourceProject != null) {
						string childPath = sourceProject.FileName;
						excludedReferences.Add (childPath);
					}
				}
			}

			return excludedReferences;
		}
	}
}
