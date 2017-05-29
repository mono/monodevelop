//
// SharedAssetsProjectMSBuildExtension.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.IO;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects.SharedAssetsProjects
{
	[ExportProjectModelExtension]
	class SharedAssetsProjectMSBuildExtension: DotNetProjectExtension
	{
		internal protected override void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnReadProject (monitor, msproject);

			// Convert .projitems imports into project references

			foreach (var sp in msproject.Imports.Where (im => im.Label == "Shared" && im.Project.EndsWith (".projitems"))) {
				var projitemsFile = sp.Project;
				if (!string.IsNullOrEmpty (projitemsFile)) {
					projitemsFile = MSBuildProjectService.FromMSBuildPath (Project.ItemDirectory, projitemsFile);
					projitemsFile = Path.Combine (Path.GetDirectoryName (msproject.FileName), projitemsFile);
					if (File.Exists (projitemsFile)) {
						var r = ProjectReference.CreateProjectReference (projitemsFile);
						r.Flags = ProjectItemFlags.DontPersist;
						r.SetItemsProjectPath (projitemsFile);
						Project.References.Add (r);
					}
				}
			}
		}

		internal protected override void OnWriteProject (ProgressMonitor monitor, MSBuildProject project)
		{
			base.OnWriteProject (monitor, project);

			HashSet<string> validProjitems = new HashSet<string> ();
			foreach (var r in Project.References.Where (rp => rp.ReferenceType == ReferenceType.Project)) {
				var ip = r.GetItemsProjectPath ();
				if (!string.IsNullOrEmpty (ip)) {
					ip = MSBuildProjectService.ToMSBuildPath (Project.ItemDirectory, ip);
					validProjitems.Add (ip);
					if (!project.Imports.Any (im => im.Project == ip)) {
						// If there is already a Shared import, place the new import in the same location
						MSBuildObject before = project.Imports.FirstOrDefault (i => i.Label == "Shared" && i.Project.EndsWith (".projitems"));
						if (before == null) {
							var fsharpProject = project.ProjectTypeGuids.Contains("{F2A71F9B-5D33-465A-A702-920D77279786}");
							if (fsharpProject)
								//For F# use the first item group as the shared project files have to be listed first
								before = project.ItemGroups.FirstOrDefault (i => i.Label != "Shared");
							else
								before = project.Imports.FirstOrDefault (i => i.Label != "Shared");
						}
						
						var im = project.AddNewImport (ip, beforeObject: before);
						im.Label = "Shared";
						im.Condition = "Exists('" + ip + "')";
					}
				}
			}
			foreach (var im in project.Imports.ToArray ()) {
				if (im.Label == "Shared" && im.Project.EndsWith (".projitems") && !(validProjitems.Contains (im.Project)))
					project.RemoveImport (im.Project);
			}
		}
	}
}

