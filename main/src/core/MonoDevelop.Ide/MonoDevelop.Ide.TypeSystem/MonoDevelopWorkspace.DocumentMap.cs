//
// MonoDevelopWorkspace.DocumentMap.cs
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
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace
	{
		internal class DocumentMap
		{
			readonly Dictionary<string, DocumentId> documentIdMap = new Dictionary<string, DocumentId> (FilePath.PathComparer);
			readonly ProjectId projectId;

			public DocumentMap (ProjectId projectId)
			{
				this.projectId = projectId;
			}

			internal DocumentId GetOrCreate (string name, DocumentMap previous)
			{
				var oldId = previous?.Get (name);
				if (oldId != null) {
					Add (oldId, name);
					return oldId;
				}
				return GetOrCreate (name);
			}

			internal DocumentId GetOrCreate (string name)
			{
				lock (documentIdMap) {
					if (!documentIdMap.TryGetValue (name, out DocumentId result)) {
						documentIdMap[name] = result = DocumentId.CreateNewId (projectId, name);
					}
					return result;
				}
			}

			internal void Add (DocumentId id, string name)
			{
				lock (documentIdMap) {
					documentIdMap [name] = id;
				}
			}

			public DocumentId Get (string name)
			{
				lock (documentIdMap) {
					documentIdMap.TryGetValue (name, out DocumentId result);
					return result;
				}
			}

			internal void Remove (string name)
			{
				lock (documentIdMap) {
					documentIdMap.Remove (name);
				}
			}
		}
	}
}