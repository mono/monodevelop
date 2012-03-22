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
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using System.Threading;
using MonoDevelop.Ide.Gui;
using System.Diagnostics;

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
				return resolver == null;
			}
		}

		public CSharpParsedFile ParsedFile {
			get {
				Debug.Assert (!IsInvalid);
				return resolver.ParsedFile;
			}
		}

		public CompilationUnit Unit {
			get {
				Debug.Assert (!IsInvalid);
				return Document.ParsedDocument.GetAst<CompilationUnit> ();
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

		public override string EolMarker {
			get {
				return Document.Editor.EolMarker;
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
		
		public override int SelectionStart {
			get {
				return Document.Editor.SelectionRange.Offset;
			}
		}
		
		public override int SelectionEnd { 
			get {
				return Document.Editor.SelectionRange.EndOffset;
			}
		}
		
		public override int SelectionLength {
			get {
				return Document.Editor.SelectionRange.Length;
			}
		}

		public override int GetOffset (TextLocation location)
		{
			return Document.Editor.LocationToOffset (location.Line, location.Column);
		}
		
		public override TextLocation GetLocation (int offset)
		{
			var loc = Document.Editor.OffsetToLocation (offset);
			return new TextLocation (loc.Line, loc.Column);
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

		public override Script StartScript ()
		{
			return new MDRefactoringScript (this.Document, this.Document.GetFormattingOptions ());
		}
		

		static CSharpAstResolver CreateResolver (Document document)
		{
			var parsedDocument = document.ParsedDocument;
			if (parsedDocument == null)
				return null;

			var unit       = parsedDocument.GetAst<CompilationUnit> ();
			var parsedFile = parsedDocument.ParsedFile as CSharpParsedFile;
			
			if (unit == null || parsedFile == null)
				return null;

			return new CSharpAstResolver (document.Compilation, unit, parsedFile);
		}

		public MDRefactoringContext (MonoDevelop.Ide.Gui.Document document, TextLocation loc, CancellationToken cancellationToken = default (CancellationToken)) : base (CreateResolver (document), cancellationToken)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			this.Document = document;
			this.location = loc;
		}
		
		public override AstType CreateShortType (IType fullType)
		{
			var parsedFile = Document.ParsedDocument.ParsedFile as CSharpParsedFile;
			
			var csResolver = parsedFile.GetResolver (Document.Compilation, Document.Editor.Caret.Location);
			
			var builder = new ICSharpCode.NRefactory.CSharp.Refactoring.TypeSystemAstBuilder (csResolver);
			return builder.ConvertType (fullType);			
		}
	}
}
