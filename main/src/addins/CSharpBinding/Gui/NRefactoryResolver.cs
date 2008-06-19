//
// NRefactoryResolver.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Gui.Completion;

using CSharpBinding.Parser.SharpDevelopTree;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;

namespace MonoDevelop.CSharpBinding
{
	public class NRefactoryResolver
	{
		Project    project;
		TextEditor editor;
		IType   callingType;
		IMember callingMember;
		
		public NRefactoryResolver (Project project, TextEditor editor, string fileName)
		{
			this.editor = editor;
			ProjectDom dom = ProjectDomService.GetDom (project);
			if (dom == null)
				return;
			foreach (IType type in dom.GetTypesFrom (fileName)) {
				if (type.BodyRegion.Contains (editor.CursorLine, editor.CursorColumn)) {
					callingType = type;
					break;
				}
			}
			
			if (callingType != null) {
				foreach (IMember member in callingType.Members) {
					if (member.BodyRegion.Contains (editor.CursorLine, editor.CursorColumn)) {
						callingMember = member;
						break;
					}
				}
			}
		}
			
		Expression ParseExpression (ExpressionResult expressionResult)
		{
			ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (expressionResult.Expression));
			return parser.ParseExpression();
		}
		
		public ResolveResult Resolve (ExpressionResult expressionResult)
		{
			Expression expr = ParseExpression (expressionResult);
			if (expr == null) 
				return null;
			
			ResolveResult result = new ResolveResult ();
			
			return result;
		}
		
		string CreateWrapperClassForMember (IMember member)
		{
			StringBuilder result = new StringBuilder ();
			int startLine = member.Location.Line;
			int endLine   = member.Location.Line;
			if (!member.BodyRegion.IsEmpty)
				endLine = member.BodyRegion.End.Line;
			result.Append ("class Wrapper {");
			result.Append (this.editor.GetText (this.editor.GetPositionFromLineColumn (startLine, 0),
			                                    this.editor.GetPositionFromLineColumn (endLine, this.editor.GetLineLength (endLine))));

			result.Append ("}");
			return result.ToString ();
		}
		
	}
}
