//
// ProjectedDocumentContext.cs
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
using MonoDevelop.Projects;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Editor.Projection
{

	class ProjectedDocumentContext : DocumentContext
	{
		DocumentContext originalContext;
		TextEditor projectedEditor;
		ParsedDocument parsedDocument;

		public TextEditor ProjectedEditor {
			get {
				return projectedEditor;
			}
		}

		public DocumentContext OriginalContext {
			get {
				return originalContext;
			}
		}

		public override bool IsUntitled {
			get {
				return originalContext.IsUntitled;
			}
		}

		Microsoft.CodeAnalysis.Document projectedDocument;

		public ProjectedDocumentContext (TextEditor projectedEditor, DocumentContext originalContext)
		{
			if (projectedEditor == null)
				throw new ArgumentNullException ("projectedEditor");
			if (originalContext == null)
				throw new ArgumentNullException ("originalContext");
			this.projectedEditor = projectedEditor;
			this.originalContext = originalContext;

			if (originalContext.Project != null) {
				var originalProjectId = TypeSystemService.GetProjectId (originalContext.Project);
				if (originalProjectId != null) {
					var originalProject = TypeSystemService.Workspace.CurrentSolution.GetProject (originalProjectId);
					if (originalProject != null) {
						projectedDocument = originalProject.AddDocument (
							projectedEditor.FileName,
							projectedEditor
						);
					}
				}
			}

			projectedEditor.TextChanged += delegate(object sender, TextChangeEventArgs e) {
				if (projectedDocument != null)
					projectedDocument = projectedDocument.WithText (projectedEditor);
				ReparseDocument ();
			};

			ReparseDocument ();
		}

		#region implemented abstract members of DocumentContext
		public override void AttachToProject (MonoDevelop.Projects.Project project)
		{
		}

		public override void ReparseDocument ()
		{
			ReparseDocumentInternal ();
		}

		Task ReparseDocumentInternal ()
		{
			var options = new ParseOptions {
				FileName = projectedEditor.FileName,
				Content = projectedEditor,
				Project = Project,
				RoslynDocument = projectedDocument,
				OldParsedDocument = parsedDocument
			}; 
			return TypeSystemService.ParseFile (options, projectedEditor.MimeType).ContinueWith (t => {
				parsedDocument = t.Result;
				base.OnDocumentParsed (EventArgs.Empty);
			});
		}

		public override Microsoft.CodeAnalysis.Options.OptionSet GetOptionSet ()
		{
			return originalContext.GetOptionSet ();
		}

		public override async Task<MonoDevelop.Ide.TypeSystem.ParsedDocument> UpdateParseDocument ()
		{
			await ReparseDocumentInternal ();
			return parsedDocument;
		}

		public override string Name {
			get {
				return projectedEditor.FileName;
			}
		}

		public override MonoDevelop.Projects.Project Project {
			get {
				return originalContext.Owner as Project;
			}
		}

		public override MonoDevelop.Projects.SolutionItem Owner {
			get {
				return originalContext.Owner;
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
}

