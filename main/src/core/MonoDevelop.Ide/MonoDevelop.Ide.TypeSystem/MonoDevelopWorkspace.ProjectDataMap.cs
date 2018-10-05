//
// MonoDevelopWorkspace.ProjectDataMap.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace
	{
		internal class ProjectDataMap
		{
			public MonoDevelopWorkspace Workspace { get; }
			readonly object gate = new object ();

			ImmutableDictionary<ProjectId, MonoDevelop.Projects.Project> projectIdToMdProjectMap = ImmutableDictionary<ProjectId, MonoDevelop.Projects.Project>.Empty;
			readonly Dictionary<MonoDevelop.Projects.Project, ProjectId> projectIdMap = new Dictionary<MonoDevelop.Projects.Project, ProjectId> ();
			// FIXME: Make this private
			internal readonly Dictionary<ProjectId, ProjectData> projectDataMap = new Dictionary<ProjectId, ProjectData> ();

			public ProjectDataMap (MonoDevelopWorkspace workspace)
			{
				Workspace = workspace;
			}

			internal ProjectId GetId (MonoDevelop.Projects.Project p)
			{
				lock (gate) {
					return projectIdMap.TryGetValue (p, out ProjectId result) ? result : null;
				}
			}

			internal ProjectId GetOrCreateId (MonoDevelop.Projects.Project p, MonoDevelop.Projects.Project oldProject)
			{
				lock (gate) {
					var result = MigrateOldProjectInfo ();
					if (result == null) {
						projectIdMap [p] = result = ProjectId.CreateNewId (p.Name);
						projectIdToMdProjectMap = projectIdToMdProjectMap.Add (result, p);
					}
					return result;
				}

				ProjectId MigrateOldProjectInfo ()
				{
					if (projectIdMap.TryGetValue (p, out var id))
						return id;

					p.Modified += Workspace.OnProjectModified;
					if (oldProject == null)
						return null;

					oldProject.Modified -= Workspace.OnProjectModified;
					if (projectIdMap.TryGetValue (oldProject, out id)) {
						projectIdMap.Remove (oldProject);
						projectIdMap [p] = id;
						projectIdToMdProjectMap = projectIdToMdProjectMap.SetItem (id, p);
					}
					return id;
				}
			}

			internal void RemoveProject (MonoDevelop.Projects.Project project, ProjectId id)
			{
				lock (gate) {
					if (projectIdMap.TryGetValue (project, out ProjectId val))
						projectIdMap.Remove (project);
					projectIdToMdProjectMap = projectIdToMdProjectMap.Remove (val);
				}

				lock (Workspace.updatingProjectDataLock) {
					projectDataMap.Remove (id);
				}
			}

			internal MonoDevelop.Projects.Project GetMonoProject (ProjectId projectId)
			{
				lock (gate) {
					return projectIdToMdProjectMap.TryGetValue (projectId, out var result) ? result : null;
				}
			}

			internal bool Contains (ProjectId projectId)
			{
				lock (Workspace.updatingProjectDataLock) {
					return projectDataMap.ContainsKey (projectId);
				}
			}

			internal ProjectData GetData (ProjectId id)
			{
				lock (Workspace.updatingProjectDataLock) {
					projectDataMap.TryGetValue (id, out ProjectData result);
					return result;
				}
			}

			internal ProjectData RemoveData (ProjectId id)
			{
				lock (Workspace.updatingProjectDataLock) {
					if (projectDataMap.TryGetValue (id, out ProjectData result)) {
						projectDataMap.Remove (id);
						result.Disconnect ();
					}
					return result;
				}
			}

			internal ProjectData CreateData (ProjectId id, ImmutableArray<MonoDevelopMetadataReference> metadataReferences)
			{
				lock (Workspace.updatingProjectDataLock) {
					var result = new ProjectData (id, metadataReferences, Workspace);
					projectDataMap [id] = result;
					return result;
				}
			}
		}
	}
}