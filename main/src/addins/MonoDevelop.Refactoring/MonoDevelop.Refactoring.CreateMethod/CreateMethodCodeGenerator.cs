// 
// CreateMethod.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.IO;
using System.Text;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using Mono.TextEditor;

namespace MonoDevelop.Refactoring.CreateMethod
{
	public class CreateMethodCodeGenerator : RefactoringOperation
	{
		public CreateMethodCodeGenerator ()
		{
			Name = "Create Method";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			if (options.ResolveResult == null || options.ResolveResult.ResolvedExpression == null || options.ResolveResult.ResolvedType == null || !string.IsNullOrEmpty (options.ResolveResult.ResolvedType.FullName))
				return false;
			invoke = GetInvocationExpression (options);
			
			return invoke != null;
		}
		
		InvocationExpression invoke;
		
		InvocationExpression GetInvocationExpression (RefactoringOptions options)
		{
			TextEditorData data = options.GetTextEditorData ();
			if (data == null)
				return null;
			string expression = options.ResolveResult.ResolvedExpression.Expression;
			if (!expression.Contains ("(")) {
				int startPos = data.Document.LocationToOffset (options.ResolveResult.ResolvedExpression.Region.Start.Line - 1, options.ResolveResult.ResolvedExpression.Region.Start.Column - 1);
				for (int pos = startPos; pos < data.Document.Length; pos++) {
					char ch = data.Document.GetCharAt (pos);
					if (ch == '(') {
						int offset = data.Document.GetMatchingBracketOffset (pos);
						expression = data.Document.GetTextAt (startPos, offset - startPos + 1);
						break;
					}
				}
			}
			INRefactoryASTProvider provider = options.GetASTProvider ();
			return provider != null ? provider.ParseText (expression) as InvocationExpression : null;
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Create Method");
		}
		
		public override void Run (RefactoringOptions options)
		{
			base.Run (options);
			options.Document.TextEditor.CursorPosition = selectionEnd;
			options.Document.TextEditor.Select (selectionStart, selectionEnd);
		}
		
		int selectionStart;
		int selectionEnd;
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			List<Change> result = new List<Change> ();
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return result;

			Change insertNewMethod = new Change ();

			MethodDeclaration methodDecl = new MethodDeclaration ();
			methodDecl.Name = ((IdentifierExpression)invoke.TargetObject).Identifier;
			methodDecl.TypeReference = new TypeReference ("System.Void");
			methodDecl.TypeReference.IsKeyword = true;

			if (options.ResolveResult.CallingMember.IsStatic)
				methodDecl.Modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Static;
			methodDecl.Body = new BlockStatement ();
			methodDecl.Body.AddChild (new ThrowStatement (new ObjectCreateExpression (new TypeReference ("System.NotImplementedException"), null)));
			insertNewMethod.FileName = options.Document.FileName;
			insertNewMethod.Description = string.Format (GettextCatalog.GetString ("Create new method {0}"), methodDecl.Name);
			insertNewMethod.Offset = options.Document.TextEditor.GetPositionFromLineColumn (options.ResolveResult.CallingMember.BodyRegion.End.Line, options.ResolveResult.CallingMember.BodyRegion.End.Column);

			int i = 0;
			foreach (Expression expression in invoke.Arguments) {
				i++;
				string output = provider.OutputNode (options.Dom, expression);

				string parameterName;
				if (Char.IsLetter (output[0]) || output[0] == '_') {
					parameterName = output;
				} else {
					parameterName = "par" + i;
				}

				ResolveResult resolveResult2 = resolver.Resolve (new ExpressionResult (output), options.ResolveResult.ResolvedExpression.Region.Start);
				TypeReference typeReference = new TypeReference (resolveResult2.ResolvedType.ToInvariantString ());
				typeReference.IsKeyword = true;
				ParameterDeclarationExpression pde = new ParameterDeclarationExpression (typeReference, parameterName);
				methodDecl.Parameters.Add (pde);
			}

			insertNewMethod.InsertedText = Environment.NewLine + Environment.NewLine + provider.OutputNode (options.Dom, methodDecl, options.GetIndent (options.ResolveResult.CallingMember));
			result.Add (insertNewMethod);
			int idx = insertNewMethod.InsertedText.IndexOf ("throw");
			selectionStart = insertNewMethod.Offset + idx;
			selectionEnd   = insertNewMethod.Offset + insertNewMethod.InsertedText.IndexOf (';', idx) + 1;
			return result;
		}
		
	}
}
