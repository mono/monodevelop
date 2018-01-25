//
// DocumentContext.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor.Projection;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// A document context puts a textual document in a semantic context inside a project and gives access
	/// to the parse information of the textual document.
	/// </summary>
	public abstract class DocumentContext
	{
		/// <summary>
		/// The name of the document. It's the file name for files on disc. 
		/// For unsaved files that name is different.
		/// </summary>
		public abstract string Name {
			get;
		}

		/// <summary>
		/// Project != null
		/// </summary>
		public virtual bool HasProject {
			get { return Project != null; }
		}

		internal virtual bool IsAdHocProject {
			get { return false; }
		}

		/// <summary>
		/// Gets the project this context is in.
		/// </summary>
		public abstract Project Project {
			get;
		}


		/// <summary>
		/// Determine if the file has already saved on disk. Untitled files are open
		/// in the IDE only. After the first save the file is no longer untitled.
		/// </summary>
		public virtual bool IsUntitled {
			get {
				return false;
			}
		}

		WorkspaceId workspaceId = WorkspaceId.Empty;

		public virtual T GetPolicy<T> (IEnumerable<string> types) where T : class, IEquatable<T>, new ()
		{
			var project = Project;
			if (project != null && project.Policies != null) {
				return project.Policies.Get<T> (types);
			}
			return MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<T> (types);
		}

		public Microsoft.CodeAnalysis.Workspace RoslynWorkspace
		{
			get { return TypeSystemService.GetWorkspace (workspaceId); }
			protected set { workspaceId = ((MonoDevelopWorkspace)value).Id; }
		}

		/// <summary>
		/// Returns the roslyn document for this document. This may return <c>null</c> if it's no compileable document.
		/// Even if it's a C# file. Is always not <c>null</c> when the parser returns <c>true</c> on CanGenerateAnalysisDocument.
		/// </summary>
		public abstract Microsoft.CodeAnalysis.Document AnalysisDocument
		{
			get;
		}

		public event EventHandler AnalysisDocumentChanged;

		protected virtual void OnAnalysisDocumentChanged (global::System.EventArgs e)
		{
			AnalysisDocumentChanged?.Invoke (this, e);
		}

		/// <summary>
		/// The parsed document. Contains all syntax information about the text.
		/// </summary>
		public abstract ParsedDocument ParsedDocument
		{
			get;
		}

		/// <summary>
		/// If true, the document is part of the ProjectContent.
		/// </summary>
		public virtual bool IsCompileableInProject
		{
			get
			{
				return true;
			}
		}

		public virtual T GetContent<T>() where T : class
		{
			var t = this as T;
			if (t != null)
				return t;
			return null;
		}

		public virtual IEnumerable<T> GetContents<T>() where T : class
		{
			var t = this as T;
			if (t != null)
				yield return t;
		}

		/// <summary>
		/// This is called after the ParsedDocument updated.
		/// </summary>
		public event EventHandler DocumentParsed;

		protected void OnDocumentParsed (EventArgs e)
		{
			var handler = DocumentParsed;
			if (handler != null)
				handler (this, e);
		}

		public abstract void AttachToProject (Project project);

		/// <summary>
		/// Forces a reparse of the document. This call doesn't block the ui thread. 
		/// The next call to ParsedDocument will give always the current parsed document but may block the UI thread.
		/// </summary>
		public abstract void ReparseDocument ();

		public abstract OptionSet GetOptionSet ();

		public abstract Task<ParsedDocument> UpdateParseDocument ();

		public event EventHandler Saved;

		protected virtual void OnSaved (EventArgs e)
		{
			var handler = Saved;
			if (handler != null)
				handler (this, e);
		}

		internal virtual Task<IReadOnlyList<Editor.Projection.Projection>> GetPartialProjectionsAsync (CancellationToken cancellationToken = default(CancellationToken))
		{
			return null;
		}
	}
}