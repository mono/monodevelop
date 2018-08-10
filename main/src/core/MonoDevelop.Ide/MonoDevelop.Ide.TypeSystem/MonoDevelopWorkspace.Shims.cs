//
// MonoDevelopWorkspace.Shims.cs
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Editor.Projection;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace
	{
		internal bool Contains (ProjectId projectId)
			=> ProjectMap.Contains (projectId);

		internal ProjectId GetProjectId (MonoDevelop.Projects.Project project)
			=> ProjectMap.GetId (project);

		internal MonoDevelop.Projects.Project GetMonoProject (Project project)
			=> GetMonoProject (project.Id);

		internal MonoDevelop.Projects.Project GetMonoProject (ProjectId projectId)
			=> ProjectMap.GetMonoProject (projectId);

		internal Task<ProjectInfo> LoadProject (MonoDevelop.Projects.Project p, CancellationToken token, MonoDevelop.Projects.Project oldProject)
			=> ProjectHandler.LoadProject (p, token, oldProject);

		internal DocumentId GetDocumentId (ProjectId projectId, string name)
		{
			var projectData = ProjectMap.GetData (projectId);
			return projectData?.DocumentData.Get (name);
		}

		internal IReadOnlyList<ProjectionEntry> ProjectionList => Projections.ProjectionList;
		/// <summary>
		/// Tries the get original file from projection. If the fileName / offset is inside a projection this method tries to convert it 
		/// back to the original physical file.
		/// </summary>
		internal bool TryGetOriginalFileFromProjection (string fileName, int offset, out string originalName, out int originalOffset)
			=> Projections.TryGetOriginalFileFromProjection (fileName, offset, out originalName, out originalOffset);

		internal void UpdateProjectionEntry (MonoDevelop.Projects.ProjectFile projectFile, IReadOnlyList<Projection> projections)
			=> Projections.UpdateProjectionEntry (projectFile, projections);
	}
}