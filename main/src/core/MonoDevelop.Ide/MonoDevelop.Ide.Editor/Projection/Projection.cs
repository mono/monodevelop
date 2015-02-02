//
// Projection.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Editor;
using System.Collections.Immutable;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Editor.Projection
{
	public sealed class Projection
	{
		public ITextDocument Document { get; private set; }

		public ImmutableList<ProjectedSegment> ProjectedSegments { get; private set; }

		TextEditor projectedEditor;

		public TextEditor ProjectedEditor {
			get {
				return projectedEditor;
			}
		}

		public TextEditor CreateProjectedEditor (DocumentContext originalContext)
		{ 
			if (projectedEditor == null) {
				projectedEditor = TextEditorFactory.CreateNewEditor (Document);
				var doc = new ProjectedDocumentContext (projectedEditor, originalContext);
				projectedEditor.InitializeExtensionChain (doc);
			}
			return projectedEditor;
		}

		class ProjectedDocumentContext : DocumentContext
		{
			DocumentContext originalContext;
			TextEditor projectedEditor;
			ParsedDocument parsedDocument;

			Microsoft.CodeAnalysis.Document projectedDocument;

			public ProjectedDocumentContext (TextEditor projectedEditor, DocumentContext originalContext)
			{
				if (projectedEditor == null)
					throw new ArgumentNullException ("projectedEditor");
				if (originalContext == null)
					throw new ArgumentNullException ("originalContext");
				this.projectedEditor = projectedEditor;
				this.originalContext = originalContext;

				var originalProjectId = TypeSystemService.Workspace.GetProjectId (originalContext.Project);
				var originalProject = TypeSystemService.Workspace.CurrentSolution.GetProject (originalProjectId);

				projectedDocument = originalProject.AddDocument (
					projectedEditor.FileName,
					projectedEditor
				);
				ReparseDocument ();
			}

			#region implemented abstract members of DocumentContext
			public override void AttachToProject (MonoDevelop.Projects.Project project)
			{
			}

			public override void ReparseDocument ()
			{
				var options = new ParseOptions {
					FileName = projectedEditor.FileName,
					Content = projectedEditor,
					Project = Project,
					RoslynDocument = projectedDocument
				}; 
				parsedDocument = TypeSystemService.ParseFile (options, projectedEditor.MimeType).Result;

				base.OnDocumentParsed (EventArgs.Empty);
			}

			public override Microsoft.CodeAnalysis.Options.OptionSet GetOptionSet ()
			{
				return originalContext.GetOptionSet ();
			}

			public override MonoDevelop.Ide.TypeSystem.ParsedDocument UpdateParseDocument ()
			{
				ReparseDocument ();
				return parsedDocument;
			}

			public override string Name {
				get {
					return projectedEditor.FileName;
				}
			}

			public override MonoDevelop.Projects.Project Project {
				get {
					return originalContext.Project;
				}
			}

			public override Microsoft.CodeAnalysis.Document AnalysisDocument {
				get {
					
					return projectedDocument;
				}
			}

			public override MonoDevelop.Ide.TypeSystem.ParsedDocument ParsedDocument {
				get {
					return parsedDocument;
				}
			}
			#endregion
		}

		public Projection (ITextDocument document, ImmutableList<ProjectedSegment> projectedSegments)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (projectedSegments == null)
				throw new ArgumentNullException ("projectedSegments");
			this.Document = document;
			this.ProjectedSegments = projectedSegments;
		}
	}
}