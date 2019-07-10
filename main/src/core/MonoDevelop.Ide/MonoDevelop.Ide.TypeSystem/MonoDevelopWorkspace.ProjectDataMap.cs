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
using System.Linq;
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
			readonly Dictionary<MonoDevelop.Projects.Project, List<FrameworkMap>> projectIdMap = new Dictionary<MonoDevelop.Projects.Project, List<FrameworkMap>> ();
			readonly Dictionary<ProjectId, ProjectData> projectDataMap = new Dictionary<ProjectId, ProjectData> ();
			readonly object updatingProjectDataLock = new object ();

			public ProjectDataMap (MonoDevelopWorkspace workspace)
			{
				Workspace = workspace;
			}

			internal ProjectId GetId (MonoDevelop.Projects.Project p, string framework = null)
			{
				lock (gate) {
					if (projectIdMap.TryGetValue (p, out var frameworkMappings)) {
						var map = frameworkMappings.FirstOrDefault (f => f.Framework == framework);
						if (map != null)
							return map.ProjectId;
						if (framework == null) {
							// Ensure that code that is not multi-framework aware finds a ProjectId.
							map = frameworkMappings.FirstOrDefault ();
							return map?.ProjectId;
						}
					}
					return null;
				}
			}

			internal ProjectId[] GetIds (MonoDevelop.Projects.Project p)
			{
				lock (gate) {
					if (projectIdMap.TryGetValue (p, out var frameworkMappings))
						return frameworkMappings.Select (f => f.ProjectId).ToArray ();
					return null;
				}
			}

			internal ProjectId GetOrCreateId (MonoDevelop.Projects.Project p, MonoDevelop.Projects.Project oldProject, string framework = null)
			{
				lock (gate) {
					var frameworkMappings = MigrateOldProjectInfo ();
					if (frameworkMappings != null) {
						var map = frameworkMappings.FirstOrDefault (f => f.Framework == framework);
						if (map != null)
							return map.ProjectId;
					} else {
						frameworkMappings = new List<FrameworkMap> ();
						projectIdMap [p] = frameworkMappings;
					}

					string debugName = string.IsNullOrEmpty (framework) ? p.Name : $"{p.Name} ({framework})";
					ProjectId id = ProjectId.CreateNewId (debugName);
					frameworkMappings.Add (new FrameworkMap (framework, id));
					projectIdToMdProjectMap = projectIdToMdProjectMap.Add (id, p);
					return id;
				}

				List<FrameworkMap> MigrateOldProjectInfo ()
				{
					if (projectIdMap.TryGetValue (p, out var frameworkMappings)) {
						return frameworkMappings;
					}

					p.Modified += Workspace.OnProjectModified;
					if (oldProject == null)
						return null;

					oldProject.Modified -= Workspace.OnProjectModified;
					if (projectIdMap.TryGetValue (oldProject, out frameworkMappings)) {
						projectIdMap.Remove (oldProject);
						projectIdMap [p] = frameworkMappings;
						foreach (var mapping in frameworkMappings)
							projectIdToMdProjectMap = projectIdToMdProjectMap.SetItem (mapping.ProjectId, p);
					}
					return frameworkMappings;
				}
			}

			internal void RemoveProject (MonoDevelop.Projects.Project project)
			{
				List<FrameworkMap> frameworkMappings = null;

				lock (gate) {
					if (projectIdMap.TryGetValue (project, out frameworkMappings)) {
						projectIdMap.Remove (project);
						projectIdToMdProjectMap = projectIdToMdProjectMap.RemoveRange (
							frameworkMappings.Select (mapping => mapping.ProjectId));
					}
				}

				if (frameworkMappings != null) {
					foreach (FrameworkMap mapping in frameworkMappings) {
						RemoveData (mapping.ProjectId);
					}
				}
			}

			internal MonoDevelop.Projects.Project RemoveProject (ProjectId id)
			{
				MonoDevelop.Projects.Project actualProject;

				lock (gate) {
					if (projectIdToMdProjectMap.TryGetValue (id, out actualProject)) {
						if (projectIdMap.TryGetValue (actualProject, out var frameworkMappings)) {
							frameworkMappings.RemoveAll (mapping => mapping.ProjectId == id);
							if (frameworkMappings.Count == 0)
								projectIdMap.Remove (actualProject);
						}
						projectIdToMdProjectMap = projectIdToMdProjectMap.Remove (id);
					}
				}

				RemoveData (id);

				return actualProject;
			}

			internal MonoDevelop.Projects.Project GetMonoProject (ProjectId projectId)
			{
				lock (gate) {
					return projectIdToMdProjectMap.TryGetValue (projectId, out var result) ? result : null;
				}
			}

			internal (MonoDevelop.Projects.Project project, string framework) GetMonoProjectAndFramework (ProjectId projectId)
			{
				lock (gate) {
					var project = projectIdToMdProjectMap.TryGetValue (projectId, out var result) ? result : null;
					if (project != null && projectIdMap.TryGetValue (project, out var frameworkMappings)) {
						var frameworkMap = frameworkMappings.FirstOrDefault (mapping => mapping.ProjectId == projectId);
						return (project, frameworkMap.Framework);
					}

					return (project, null);
				}
			}

			internal bool Contains (ProjectId projectId)
			{
				lock (updatingProjectDataLock) {
					return projectDataMap.ContainsKey (projectId);
				}
			}

			internal ProjectData GetData (ProjectId id)
			{
				lock (updatingProjectDataLock) {
					projectDataMap.TryGetValue (id, out ProjectData result);
					return result;
				}
			}

			internal ProjectData RemoveData (ProjectId id)
			{
				lock (updatingProjectDataLock) {
					if (projectDataMap.TryGetValue (id, out ProjectData result)) {
						projectDataMap.Remove (id);
						result.Disconnect ();
					}
					return result;
				}
			}

			internal ProjectData CreateData (ProjectId id, ImmutableArray<MonoDevelopMetadataReference> metadataReferences)
			{
				lock (updatingProjectDataLock) {
					var result = new ProjectData (id, metadataReferences, Workspace);
					projectDataMap [id] = result;
					return result;
				}
			}
			internal ProjectId[] GetProjectIds ()
			{
				lock (updatingProjectDataLock) {
					return projectDataMap.Keys.ToArray ();
				}
			}

			class FrameworkMap
			{
				public FrameworkMap (string framework, ProjectId projectId)
				{
					Framework = framework;
					ProjectId = projectId;
				}

				public string Framework { get; set; }
				public ProjectId ProjectId { get; set; }
			}
		}
	}
}