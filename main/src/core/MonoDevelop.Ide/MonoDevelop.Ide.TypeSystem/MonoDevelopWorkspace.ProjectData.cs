//
// MonoDevelopWorkspace.ProjectData.cs
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
using System.Threading;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace
	{
		internal class ProjectData
		{
			readonly WeakReference<MonoDevelopWorkspace> workspaceRef;
			readonly ProjectId projectId;
			readonly List<MonoDevelopMetadataReference> metadataReferences;
			internal DocumentMap DocumentData { get; }

			public ProjectData (ProjectId projectId, ImmutableArray<MonoDevelopMetadataReference> metadataReferences, MonoDevelopWorkspace ws)
			{
				this.projectId = projectId;
				workspaceRef = new WeakReference<MonoDevelopWorkspace> (ws);
				DocumentData = new DocumentMap (projectId);
				this.metadataReferences = new List<MonoDevelopMetadataReference> (metadataReferences.Length);

				System.Diagnostics.Debug.Assert (Monitor.IsEntered (ws.updatingProjectDataLock));
				foreach (var metadataReference in metadataReferences) {
					AddMetadataReference_NoLock (metadataReference, ws);
				}
			}

			void OnMetadataReferenceUpdated (object sender, MetadataReferenceUpdatedEventArgs args)
			{
				var reference = (MonoDevelopMetadataReference)sender;
				// If we didn't contain the reference, bail
				if (!workspaceRef.TryGetTarget (out var workspace))
					return;

				lock (workspace.updatingProjectDataLock) {
					if (!RemoveMetadataReference_NoLock (reference, workspace))
						return;
					workspace.OnMetadataReferenceRemoved (projectId, args.OldSnapshot);

					AddMetadataReference_NoLock (reference, workspace);
					workspace.OnMetadataReferenceAdded (projectId, args.NewSnapshot.Value);
				}
			}

			void AddMetadataReference_NoLock (MonoDevelopMetadataReference metadataReference, MonoDevelopWorkspace ws)
			{
				System.Diagnostics.Debug.Assert (Monitor.IsEntered (ws.updatingProjectDataLock));

				metadataReferences.Add (metadataReference);
				metadataReference.SnapshotUpdated += OnMetadataReferenceUpdated;
			}

			bool RemoveMetadataReference_NoLock (MonoDevelopMetadataReference metadataReference, MonoDevelopWorkspace ws)
			{
				System.Diagnostics.Debug.Assert (Monitor.IsEntered (ws.updatingProjectDataLock));

				metadataReference.SnapshotUpdated -= OnMetadataReferenceUpdated;
				return metadataReferences.Remove (metadataReference);
			}

			public void Disconnect ()
			{
				if (!workspaceRef.TryGetTarget (out var ws))
					return;

				System.Diagnostics.Debug.Assert (Monitor.IsEntered (ws.updatingProjectDataLock));
				foreach (var reference in metadataReferences)
					reference.SnapshotUpdated -= OnMetadataReferenceUpdated;
			}
		}
	}
}