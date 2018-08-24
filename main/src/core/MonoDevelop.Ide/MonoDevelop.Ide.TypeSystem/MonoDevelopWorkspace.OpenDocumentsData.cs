//
// MonoDevelopWorkspace.OpenDocumentsData.cs
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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace
	{
		internal class OpenDocumentsData
		{
			readonly Dictionary<DocumentId, (SourceTextContainer Container, TextEditor Editor, DocumentContext Context)> openDocuments =
				new Dictionary<DocumentId, (SourceTextContainer, TextEditor, DocumentContext)> ();

			internal void Add (DocumentId documentId, SourceTextContainer container, TextEditor editor, DocumentContext context)
			{
				lock (openDocuments) {
					openDocuments.Add (documentId, (container, editor, context));
				}
			}

			internal bool Contains (DocumentId documentId)
			{
				lock (openDocuments) {
					return openDocuments.ContainsKey (documentId);
				}
			}

			internal bool Remove (DocumentId documentId)
			{
				lock (openDocuments) {
					return openDocuments.Remove (documentId);
				}
			}

			internal void CorrectDocumentIds (DotNetProject project, ProjectInfo projectInfo)
			{
				lock (openDocuments) {
					foreach (var openDoc in openDocuments) {
						if (openDoc.Value.Context.Project != project)
							continue;

						var doc = openDoc.Value.Context.AnalysisDocument;
						if (doc == null)
							continue;

						var newDocument = projectInfo.Documents.FirstOrDefault (d => d.FilePath == doc.FilePath);
						if (newDocument == null || newDocument.Id == doc.Id)
							continue;

						openDoc.Value.Context.UpdateDocumentId (newDocument.Id);
					}
				}
			}
		}
	}
}