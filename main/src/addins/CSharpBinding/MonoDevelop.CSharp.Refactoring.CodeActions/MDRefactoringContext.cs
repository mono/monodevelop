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
using MonoDevelop.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using System.Threading;
using MonoDevelop.Ide.Gui;
using System.Diagnostics;
using MonoDevelop.CSharp.Refactoring.CodeIssues;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	public class MDRefactoringContext : RefactoringContext
	{
		public MonoDevelop.Ide.Gui.Document Document {
			get;
			private set;
		}

		public bool IsInvalid {
			get {
				if (Resolver == null || Document == null)
					return true;
				var pd = Document.ParsedDocument;
				return pd == null || pd.HasErrors;
			}
		}

		public SyntaxTree Unit {
			get {
				Debug.Assert (!IsInvalid);
				return Document.ParsedDocument.GetAst<SyntaxTree> ();
			}
		}

		public override bool Supports (Version version)
		{
			var project = Document.Project as DotNetProject;
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
				return Document.Editor.CreateNRefactoryTextEditorOptions ();
			}
		}
		
		public override bool IsSomethingSelected { 
			get {
				return Document.Editor.IsSomethingSelected;
			}
		}
		
		public override string SelectedText {
			get {
				return Document.Editor.SelectedText;
			}
		}
		
		public override TextLocation SelectionStart {
			get {
				return Document.Editor.MainSelection.Start;
			}
		}
		
		public override TextLocation SelectionEnd { 
			get {
				return Document.Editor.MainSelection.End;
			}
		}

		public override int GetOffset (TextLocation location)
		{
			return Document.Editor.LocationToOffset (location);
		}
		
		public override TextLocation GetLocation (int offset)
		{
			return Document.Editor.OffsetToLocation (offset);
		}

		public override string GetText (int offset, int length)
		{
			return Document.Editor.GetTextAt (offset, length);
		}
		
		public override string GetText (ICSharpCode.NRefactory.Editor.ISegment segment)
		{
			return Document.Editor.GetTextAt (segment.Offset, segment.Length);
		}
		
		public override ICSharpCode.NRefactory.Editor.IDocumentLine GetLineByOffset (int offset)
		{
			return Document.Editor.GetLineByOffset (offset);
		}

		readonly TextLocation location;
		public override TextLocation Location {
			get {
				return location;
			}
		}

		public Script StartScript ()
		{
			return new MDRefactoringScript (this, this.Document, this.Document.GetFormattingOptions ());
		}


		static CSharpAstResolver CreateResolver (Document document)
		{
			var parsedDocument = document.ParsedDocument;
			if (parsedDocument == null)
				return null;

			var unit       = parsedDocument.GetAst<SyntaxTree> ();
			var parsedFile = parsedDocument.ParsedFile as CSharpUnresolvedFile;
			
			if (unit == null || parsedFile == null)
				return null;

			return new CSharpAstResolver (document.Compilation, unit, parsedFile);
		}

		public MDRefactoringContext (MonoDevelop.Ide.Gui.Document document, TextLocation loc, CancellationToken cancellationToken = default (CancellationToken)) : base (CreateResolver (document), cancellationToken)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			this.Document = document;
			this.location = RefactoringService.GetCorrectResolveLocation (document, loc);
			var policy = Document.HasProject ? Document.Project.Policies.Get<NameConventionPolicy> () : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<NameConventionPolicy> ();
			Services.AddService (typeof(NamingConventionService), policy.CreateNRefactoryService ());

		}

	}
}
