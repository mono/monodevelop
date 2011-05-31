// 
// InvertIf.cs
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
using System.Collections.Generic;
using Mono.TextEditor;
using System.Linq;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.QuickFix
{
	public class InvertIf : CSharpQuickFix
	{
		public InvertIf ()
		{
			Description = GettextCatalog.GetString ("Inverts an 'if ... else' expression.");
		}
		
		public override string GetMenuText (MonoDevelop.Ide.Gui.Document editor, DomLocation loc)
		{
			return GettextCatalog.GetString ("Invert if");
		}
		
		IfElseStatement GetIfElseStatement (ParsedDocument doc, DomLocation loc)
		{
			var unit = doc.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit;
			if (unit == null)
				return null;
			var result = unit.GetNodeAt<IfElseStatement> (loc.Line, loc.Column);
			if (result != null && result.IfToken.Contains (loc.Line, loc.Column))
				return result;
			return null;
		}
		
		public override bool IsValid (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var ifStatement = GetIfElseStatement (document.ParsedDocument, loc);
			return ifStatement != null && !ifStatement.TrueStatement.IsNull && !ifStatement.FalseStatement.IsNull;
		}
		// TODO: Invert if without else
		// ex. if (cond) DoSomething () == if (!cond) return; DoSomething ()
		// beware of loop contexts return should be continue then.
		public override void Run (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var ifStatement = GetIfElseStatement (document.ParsedDocument, loc);
			
			if (ifStatement == null)
				return;
			document.Editor.Document.BeginAtomicUndo ();
			try {
				if (!ifStatement.FalseStatement.IsNull) {
					ifStatement.FalseStatement.Replace (document, ifStatement.TrueStatement);
					ifStatement.TrueStatement.Replace (document, ifStatement.FalseStatement);
				}
				ifStatement.Condition.Replace (document, CSharpUtil.InvertCondition (ifStatement.Condition));
				
				ifStatement.FormatText (document);
			} finally {
				document.Editor.Document.EndAtomicUndo ();
			}
		}
		
	}
}

