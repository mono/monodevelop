// 
// ReplaceEmptyString.cs
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.ContextAction
{
	public class ReplaceEmptyString : CSharpContextAction
	{
		public ReplaceEmptyString ()
		{
			Description = GettextCatalog.GetString ("Replaces \"\" with string.Empty");
		}
		
		public override string GetMenuText (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			return GettextCatalog.GetString ("Use string.Empty");
		}
		
		public override bool IsValid (MonoDevelop.Ide.Gui.Document document, MonoDevelop.Projects.Dom.DomLocation loc)
		{
			return GetEmptyString (document, loc) != null;
		}
		
		public override void Run (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var expr = GetEmptyString (document, loc);
			if (expr == null)
				return;
			
			int offset = document.Editor.LocationToOffset (expr.StartLocation.Line, expr.StartLocation.Column);
			int endOffset = document.Editor.LocationToOffset (expr.EndLocation.Line, expr.EndLocation.Column);
			
			string text = "string.Empty";
			document.Editor.Replace (offset, endOffset - offset, text);
			document.Editor.Caret.Offset = offset + text.Length;
			document.Editor.Document.CommitUpdateAll ();
		}
		
		PrimitiveExpression GetEmptyString (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var unit = document.ParsedDocument.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit;
			if (unit == null)
				return null;
			var astNode = unit.GetNodeAt (loc.Line, loc.Column) as PrimitiveExpression;
			if (astNode == null || !(astNode.Value is string) || astNode.Value.ToString () != "")
				return null;
			return  astNode;
		}
		
		
	}
}

