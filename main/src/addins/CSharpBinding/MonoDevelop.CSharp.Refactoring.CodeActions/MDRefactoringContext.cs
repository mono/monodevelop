// 
// MDRefactoringContext.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory;
using System.Threading;
using MonoDevelop.Ide.Gui;
using System.Diagnostics;
using MonoDevelop.CSharp.Refactoring.CodeIssues;
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.CSharp.Formatting;
using System.Threading.Tasks;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	public class MDRefactoringContext : RefactoringContext, IRefactoringContext
	{
		MonoDevelop.Projects.Project fileContainerProject;

		public TextEditorData TextEditor {
			get;
			private set;
		}

		public DotNetProject Project {
			get;
			private set;
		}

		public MonoDevelop.Projects.Project FileContainerProject {
			get {
				if (fileContainerProject == null)
					fileContainerProject = FindProjectContainingFile () ?? Project;
				return fileContainerProject;
			}
		}

		MonoDevelop.Projects.Project FindProjectContainingFile ()
		{
			var file = TextEditor.FileName;
			if (string.IsNullOrEmpty (file) || Project == null)
				return null;

			var pf = Project.GetProjectFile (file);
			if (pf != null && (pf.Flags & ProjectItemFlags.Hidden) != 0) {
				// The file is hidden in this project, so it may also be part of another project.
				// Try to find a project in which this file is not hidden
				foreach (var p in Project.ParentSolution.GetAllProjects ()) {
					pf = p.GetProjectFile (file);
					if (pf != null && (pf.Flags & ProjectItemFlags.Hidden) == 0)
						return p;
				}
			}
			return null;
		}

		public bool IsInvalid {
			get {
				if (Resolver == null || TextEditor == null)
					return true;
				return ParsedDocument == null;
			}
		}

		public ParsedDocument ParsedDocument {
			get;
			private set;
		}

		public SyntaxTree Unit {
			get {
				Debug.Assert (!IsInvalid);
				return Resolver.RootNode as SyntaxTree;
			}
		}

		public override string DefaultNamespace {
			get {
				var p = FileContainerProject as IDotNetFileContainer;
				if (p == null || TextEditor == null || string.IsNullOrEmpty (TextEditor.FileName))
					return null;
				return p.GetDefaultNamespace (TextEditor.FileName);
			}
		}

		public override bool Supports (Version version)
		{
			var project = Project;
			if (project == null)
				return true;
			switch (project.TargetFramework.ClrVersion) {
			case ClrVersion.Net_1_1:
				return version.Major > 1 || version.Major == 1 && version.Minor >= 1;
			case ClrVersion.Net_2_0:
				return version.Major >= 2;
			case ClrVersion.Clr_2_1:
				return version.Major > 2 || version.Major == 2 && version.Minor >= 1;
			default:
				return true;
			}
		}

		public override ICSharpCode.NRefactory.CSharp.TextEditorOptions TextEditorOptions {
			get {
				return TextEditor.CreateNRefactoryTextEditorOptions ();
			}
		}

		public override bool IsSomethingSelected { 
			get {
				return TextEditor.IsSomethingSelected;
			}
		}

		public override string SelectedText {
			get {
				return TextEditor.SelectedText;
			}
		}

		public override TextLocation SelectionStart {
			get {
				return TextEditor.MainSelection.Start;
			}
		}

		public override TextLocation SelectionEnd { 
			get {
				return TextEditor.MainSelection.End;
			}
		}

		public override int GetOffset (TextLocation location)
		{
			return TextEditor.LocationToOffset (location);
		}

		public override TextLocation GetLocation (int offset)
		{
			return TextEditor.OffsetToLocation (offset);
		}

		public override string GetText (int offset, int length)
		{
			return TextEditor.GetTextAt (offset, length);
		}

		public override string GetText (ICSharpCode.NRefactory.Editor.ISegment segment)
		{
			return TextEditor.GetTextAt (segment.Offset, segment.Length);
		}

		public override ICSharpCode.NRefactory.Editor.IDocumentLine GetLineByOffset (int offset)
		{
			return TextEditor.GetLineByOffset (offset);
		}

		readonly Document document;
		TextLocation location;

		public override TextLocation Location {
			get {
				return location;
			}
		}

		internal void SetLocation (TextLocation loc)
		{
			if (document != null)
				location = RefactoringService.GetCorrectResolveLocation (document, loc);
			else
				location = loc;
		}

		readonly CSharpFormattingOptions formattingOptions;

		public CSharpFormattingOptions FormattingOptions {
			get {
				return formattingOptions;
			}
		}

		public Script StartScript ()
		{
			return new MDRefactoringScript (this, formattingOptions);
		}

		public IDisposable CreateScript ()
		{
			return StartScript ();
		}

		internal static Task<MDRefactoringContext> Create (Document document, TextLocation loc, CancellationToken cancellationToken = default (CancellationToken))
		{
			var shared = document.GetSharedResolver ();
			if (shared == null) {
				var tcs = new TaskCompletionSource<MDRefactoringContext> ();
				tcs.SetResult (null);
				return tcs.Task;
			}

			return shared.ContinueWith (t => {
				var sharedResolver = t.Result;
				if (sharedResolver == null)
					return null;
				return new MDRefactoringContext (document, sharedResolver, loc, cancellationToken);
				// Do not add TaskContinuationOptions.ExecuteSynchronously
				// https://bugzilla.xamarin.com/show_bug.cgi?id=25065
				// adding ExecuteSynchronously appears to create a deadlock situtation in TypeSystemParser.Parse()
			});
		}

		internal MDRefactoringContext (Document document, CSharpAstResolver resolver, TextLocation loc, CancellationToken cancellationToken = default (CancellationToken)) : base (resolver, cancellationToken)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			this.document = document;
			this.TextEditor = document.Editor;
			this.ParsedDocument = document.ParsedDocument;
			this.Project = document.Project as DotNetProject;
			this.formattingOptions = document.GetFormattingOptions ();
			this.location = loc;
			var policy = document.HasProject ? document.Project.Policies.Get<NameConventionPolicy> () : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<NameConventionPolicy> ();
			Services.AddService (typeof(NamingConventionService), policy.CreateNRefactoryService ());
			Services.AddService (typeof(ICSharpCode.NRefactory.CSharp.CodeGenerationService), new CSharpCodeGenerationService());
		}

		public MDRefactoringContext (DotNetProject project, TextEditorData data, ParsedDocument parsedDocument, CSharpAstResolver resolver, TextLocation loc, CancellationToken cancellationToken = default (CancellationToken)) : base (resolver, cancellationToken)
		{
			this.TextEditor = data;
			this.ParsedDocument = parsedDocument;
			this.Project = project;
			var policy = Project.Policies.Get<CSharpFormattingPolicy> ();
			this.formattingOptions = policy.CreateOptions ();
			this.location = loc;
			var namingPolicy = Project.Policies.Get<NameConventionPolicy> ();
			Services.AddService (typeof(NamingConventionService), namingPolicy.CreateNRefactoryService ());
		}
	}
}
