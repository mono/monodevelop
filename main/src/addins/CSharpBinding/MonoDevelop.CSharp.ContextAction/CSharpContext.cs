// 
// CSharpContext.cs
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

namespace MonoDevelop.CSharp.ContextAction
{
	public class CSharpContext
	{
		public MonoDevelop.Ide.Gui.Document Document {
			get;
			private set;
		}
		
		public ICSharpCode.NRefactory.CSharp.CompilationUnit Unit {
			get;
			private set;
		}
		
		public MonoDevelop.Projects.Dom.DomLocation Location {
			get;
			private set;
		}
		
		NRefactoryResolver resolver;
		public NRefactoryResolver Resolver {
			get {
				if (resolver == null)
					resolver = new NRefactoryResolver (Document.Dom, Document.CompilationUnit, Document.Editor, Document.FileName); 
				return resolver;
			}
		}
		
		public bool IsValid {
			get {
				return Unit != null;
			}
		}
		
		public bool HasCSharp3Support {
			get {
				var project = Document.Project as DotNetProject;
				switch (project.TargetFramework.ClrVersion) {
				case ClrVersion.Net_1_1:
				case ClrVersion.Net_2_0:
				case ClrVersion.Clr_2_1:
					return false;
				default:
					return true;
				}
			}
		}
		
		public CSharpContext (MonoDevelop.Ide.Gui.Document document, MonoDevelop.Projects.Dom.DomLocation loc)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			this.Document = document;
			this.Location = loc;
			this.Unit = document.ParsedDocument.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit;
		}
		
		public int GetIndentLevel (AstNode node)
		{
			return Document.CalcIndentLevel (Document.Editor.GetLineIndent (node.StartLocation.Line));
		}
		
		public string OutputNode (AstNode node, int indentLevel, Action<int, AstNode> outputStarted = null)
		{
			return Document.OutputNode (node, indentLevel, outputStarted);
		}
		
		public AstNode GetNode ()
		{
			return Unit.GetNodeAt (Location.Line, Location.Column);
		}
		
		public T GetNode<T> () where T : AstNode
		{
			return Unit.GetNodeAt<T> (Location.Line, Location.Column);
		}
		
		public void StartTextLinkMode (int baseOffset, int replaceLength, IEnumerable<int> offsets)
		{
			CommitChanges ();
			var link = new TextLink ("name");
			foreach (var o in offsets) {
				link.AddLink (new Segment (o, replaceLength));
			}
			var links = new List<TextLink> ();
			links.Add (link);
			var tle = new TextLinkEditMode (Document.Editor.Parent, baseOffset, links);
			tle.SetCaretPosition = false;
			if (tle.ShouldStartTextLinkMode) {
				tle.OldMode = Document.Editor.CurrentMode;
				tle.StartMode ();
				Document.Editor.CurrentMode = tle;
			}
		}
		
		List<MonoDevelop.Refactoring.Change> changes = new List<MonoDevelop.Refactoring.Change> ();
		public void Do (MonoDevelop.Refactoring.Change change)
		{
			changes.Add (change);
		}

		public void DoInsert (int offset, string text)
		{
			Do (new MonoDevelop.Refactoring.TextReplaceChange () {
				FileName = Document.FileName,
				Offset = offset,
				RemovedChars = 0,
				InsertedText = text
			});
		}
		
		public void DoRemove (AstNode node)
		{
			var startOffset = Document.Editor.LocationToOffset (node.StartLocation.Line, node.StartLocation.Column);
			var endOffset   = Document.Editor.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column);
			
			Do (new MonoDevelop.Refactoring.TextReplaceChange () {
				FileName = Document.FileName,
				Offset = startOffset,
				RemovedChars = endOffset - startOffset,
				InsertedText = null
			});
		}

		public void DoRemove (int offset, int length)
		{
			Do (new MonoDevelop.Refactoring.TextReplaceChange () {
				FileName = Document.FileName,
				Offset = offset,
				RemovedChars = length,
				InsertedText = null
			});
		}

		public void DoReplace (int offset, int length, string text)
		{
			Do (new MonoDevelop.Refactoring.TextReplaceChange () {
				FileName = Document.FileName,
				Offset = offset,
				RemovedChars = length,
				InsertedText = text
			});
		}
		
		public void DoReplace (AstNode node, AstNode replaceWith)
		{
			var segment = GetSegment (node);
			DoReplace (segment.Offset, segment.Length, OutputNode (replaceWith, GetIndentLevel (node)).Trim ());
		}
		
		public void CommitChanges ()
		{
			RefactoringService.AcceptChanges (null, Document.Dom, changes);
			changes.Clear ();
		}
		
		public string GetText (AstNode node)
		{
			return Document.Editor.GetTextAt (GetSegment (node));
		}
		
		public ISegment GetSegment (AstNode node)
		{
			var startOffset = Document.Editor.LocationToOffset (node.StartLocation.Line, node.StartLocation.Column);
			var endOffset   = Document.Editor.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column);
			
			return new Segment (startOffset, endOffset - startOffset);
		}
		
		public void FormatText (Func<CSharpContext, AstNode> update)
		{
			Document.UpdateParseDocument ();
			this.Unit = Document.ParsedDocument.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit;
			
			var node = update (this);
			if (node != null)
				node.FormatText (Document);
		}
	}
}
