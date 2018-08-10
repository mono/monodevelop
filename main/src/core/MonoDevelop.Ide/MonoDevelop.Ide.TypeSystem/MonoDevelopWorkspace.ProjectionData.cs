//
// MonoDevelopWorkspace.ProjectionData.cs
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Projection;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace
	{
		internal class ProjectionData
		{
			readonly object projectionListUpdateLock = new object ();

			ImmutableList<ProjectionEntry> projectionList = ImmutableList<ProjectionEntry>.Empty;
			internal IReadOnlyList<ProjectionEntry> ProjectionList => projectionList;

			internal void AddProjectionEntry (ProjectionEntry entry)
			{
				lock (projectionListUpdateLock)
					projectionList = projectionList.Add (entry);
			}

			internal void UpdateProjectionEntry (MonoDevelop.Projects.ProjectFile projectFile, IReadOnlyList<Projection> projections)
			{
				if (projectFile == null)
					throw new ArgumentNullException (nameof (projectFile));
				if (projections == null)
					throw new ArgumentNullException (nameof (projections));

				lock (projectionListUpdateLock) {
					foreach (var entry in projectionList) {
						if (entry?.File?.FilePath == projectFile.FilePath) {
							projectionList = projectionList.Remove (entry);
							// Since it's disposing projected editor, it needs to dispose in MainThread.
							Runtime.RunInMainThread (() => entry.Dispose ()).Ignore ();
							break;
						}
					}
					projectionList = projectionList.Add (new ProjectionEntry { File = projectFile, Projections = projections });
				}
			}

			/// <summary>
			/// Tries the get original file from projection. If the fileName / offset is inside a projection this method tries to convert it 
			/// back to the original physical file.
			/// </summary>
			internal bool TryGetOriginalFileFromProjection (string fileName, int offset, out string originalName, out int originalOffset)
			{
				foreach (var projectionEntry in ProjectionList) {
					var projection = projectionEntry.Projections.FirstOrDefault (p => FilePath.PathComparer.Equals (p.Document.FileName, fileName));
					if (projection == null)
						continue;

					if (projection.TryConvertFromProjectionToOriginal (offset, out originalOffset)) {
						originalName = projectionEntry.File.FilePath;
						return true;
					}
				}

				originalName = fileName;
				originalOffset = offset;
				return false;
			}

			internal void ClearOldProjectionList ()
			{
				ImmutableList<ProjectionEntry> toDispose;
				lock (projectionListUpdateLock) {
					toDispose = projectionList;
					projectionList = projectionList.Clear ();
				}
				foreach (var p in toDispose)
					p.Dispose ();
			}

			internal (Projection, string) Get (string filePath)
			{
				Projection projection = null;
				foreach (var entry in projectionList) {
					var p = entry.Projections.FirstOrDefault (proj => proj?.Document?.FileName != null && FilePath.PathComparer.Equals (proj.Document.FileName, filePath));
					if (p != null) {
						filePath = entry.File.FilePath;
						projection = p;
						break;
					}
				}

				return (projection, filePath);
			}
		}

		internal class ProjectionEntry : IDisposable
		{
			public MonoDevelop.Projects.ProjectFile File;
			public IReadOnlyList<Projection> Projections;

			public void Dispose ()
			{
				Runtime.RunInMainThread (delegate {
					foreach (var p in Projections)
						p.Dispose ();
				});
			}
		}
	}
}