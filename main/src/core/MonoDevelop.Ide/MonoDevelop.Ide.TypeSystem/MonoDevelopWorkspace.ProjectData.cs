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
using System.Threading;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace
	{
		internal class ProjectData : IDisposable
		{
			readonly WeakReference<MonoDevelopWorkspace> workspaceRef;
			readonly ProjectId projectId;
			readonly Dictionary<string, DocumentId> documentIdMap;
			readonly List<MonoDevelopMetadataReference> metadataReferences = new List<MonoDevelopMetadataReference> ();

			public ProjectInfo Info {
				get;
				set;
			}

			public ProjectData (ProjectId projectId, List<MonoDevelopMetadataReference> metadataReferences, MonoDevelopWorkspace ws)
			{
				this.projectId = projectId;
				workspaceRef = new WeakReference<MonoDevelopWorkspace> (ws);

				System.Diagnostics.Debug.Assert (Monitor.IsEntered (ws.updatingProjectDataLock));
				foreach (var metadataReference in metadataReferences) {
					AddMetadataReference_NoLock (metadataReference, ws);
				}
				documentIdMap = new Dictionary<string, DocumentId> (FilePath.PathComparer);
			}

			void OnMetadataReferenceUpdated (object sender, EventArgs args)
			{
				var reference = (MonoDevelopMetadataReference)sender;
				// If we didn't contain the reference, bail
				if (!workspaceRef.TryGetTarget (out var workspace))
					return;

				lock (workspace.updatingProjectDataLock) {
					if (!RemoveMetadataReference_NoLock (reference, workspace))
						return;
					workspace.OnMetadataReferenceRemoved (projectId, reference.CurrentSnapshot);

					reference.UpdateSnapshot ();
					AddMetadataReference_NoLock (reference, workspace);
					workspace.OnMetadataReferenceAdded (projectId, reference.CurrentSnapshot);
				}
			}

			internal void AddMetadataReference_NoLock (MonoDevelopMetadataReference metadataReference, MonoDevelopWorkspace ws)
			{
				System.Diagnostics.Debug.Assert (Monitor.IsEntered (ws.updatingProjectDataLock));

				metadataReferences.Add (metadataReference);
				metadataReference.UpdatedOnDisk += OnMetadataReferenceUpdated;
			}

			internal bool RemoveMetadataReference_NoLock (MonoDevelopMetadataReference metadataReference, MonoDevelopWorkspace ws)
			{
				System.Diagnostics.Debug.Assert (Monitor.IsEntered (ws.updatingProjectDataLock));

				metadataReference.UpdatedOnDisk -= OnMetadataReferenceUpdated;
				return metadataReferences.Remove (metadataReference);
			}

			internal DocumentId GetOrCreateDocumentId (string name, ProjectData previous)
			{
				if (previous != null) {
					var oldId = previous.GetDocumentId (name);
					if (oldId != null) {
						AddDocumentId (oldId, name);
						return oldId;
					}
				}
				return GetOrCreateDocumentId (name);
			}

			internal DocumentId GetOrCreateDocumentId (string name)
			{
				lock (documentIdMap) {
					if (!documentIdMap.TryGetValue (name, out DocumentId result)) {
						result = DocumentId.CreateNewId (projectId, name);
						documentIdMap [name] = result;
					}
					return result;
				}
			}

			internal void AddDocumentId (DocumentId id, string name)
			{
				lock (documentIdMap) {
					documentIdMap [name] = id;
				}
			}

			public DocumentId GetDocumentId (string name)
			{
				if (!documentIdMap.TryGetValue (name, out DocumentId result)) {
					return null;
				}
				return result;
			}

			internal void RemoveDocument (string name)
			{
				documentIdMap.Remove (name);
			}

			public void Dispose ()
			{
				if (!workspaceRef.TryGetTarget (out var workspace))
					return;

				lock (workspace.updatingProjectDataLock) {
					foreach (var reference in metadataReferences)
						reference.UpdatedOnDisk -= OnMetadataReferenceUpdated;
				}
			}
		}
	}
}
