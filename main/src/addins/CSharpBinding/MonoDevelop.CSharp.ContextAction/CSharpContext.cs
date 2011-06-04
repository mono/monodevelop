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
	}
}
