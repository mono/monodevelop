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
	public class CreateMethod : RefactoringOperation
	{
		public CreateMethod ()
		{
			Name = "Create Method";
		}
		
		public override bool IsValid (ProjectDom dom, MonoDevelop.Ide.Gui.Document document, ResolveResult resolveResult)
		{
			if (resolveResult == null || resolveResult.ResolvedExpression == null || !string.IsNullOrEmpty (resolveResult.ResolvedType.FullName))
				return false;
			invoke = GetInvocationExpression (document, resolveResult);
			return invoke != null;
		}
		DomLocation loc ;
		InvocationExpression invoke;
		ResolveResult resolveResult;
		MonoDevelop.Ide.Gui.Document document;
		
		InvocationExpression GetInvocationExpression (MonoDevelop.Ide.Gui.Document document, ResolveResult resolveResult)
		{
			this.document = document;
			this.resolveResult = resolveResult;
			Mono.TextEditor.ITextEditorDataProvider view = document.ActiveView as Mono.TextEditor.ITextEditorDataProvider;
			if (view == null)
				return null;
			
			TextEditorData data = view.GetTextEditorData ();
			loc = resolveResult.ResolvedExpression.Region.Start;
			string expression = resolveResult.ResolvedExpression.Expression;
			if (!expression.Contains ("(")) {
				int startPos = data.Document.LocationToOffset (resolveResult.ResolvedExpression.Region.Start.Line - 1, resolveResult.ResolvedExpression.Region.Start.Column - 1);
				StringBuilder methodCall = new StringBuilder ();
				for (int pos = startPos; pos < data.Document.Length; pos++) {
					char ch = data.Document.GetCharAt (pos);
					if (ch == '(') {
						int offset = data.Document.GetMatchingBracketOffset (pos);
						expression = data.Document.GetTextAt (startPos, offset - startPos + 1);
						break;
					}
				}
			}
			string mimeType = DesktopService.GetMimeTypeForUri (document.FileName);
			INRefactoryASTProvider provider = RefactoringService.GetASTProvider (mimeType);
			return provider != null ? provider.ParseText (expression) as InvocationExpression : null;
		}
		
		public override string GetMenuDescription (ProjectDom dom, IDomVisitable item)
		{
			return GettextCatalog.GetString ("_Create Method");
		}
		
		public override void Run (ProjectDom dom, MonoDevelop.Ide.Gui.Document document, IDomVisitable item)
		{
			List<Change> changes = PerformChanges (dom, null, null);
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor ("Create Method", null);
			RefactoringService.AcceptChanges (monitor, dom, changes);
		}
		
		public override List<Change> PerformChanges (ProjectDom dom, IDomVisitable item, object prop)
		{
			List<Change> result = new List<Change> ();
			string mimeType = DesktopService.GetMimeTypeForUri (document.FileName);
			MonoDevelop.Projects.Dom.Parser.IParser domParser = ProjectDomService.GetParser (document.FileName, mimeType);
			if (domParser == null) {
				Console.WriteLine ("parser == null");
				return result;
			}

			INRefactoryASTProvider provider = RefactoringService.GetASTProvider (mimeType);

			Change insertNewMethod = new Change ();

			MethodDeclaration methodDecl = new MethodDeclaration ();
			methodDecl.Name = ((IdentifierExpression)invoke.TargetObject).Identifier;
			methodDecl.TypeReference = new TypeReference ("System.Void");
			methodDecl.TypeReference.IsKeyword = true;

			if (resolveResult.CallingMember.IsStatic)
				methodDecl.Modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Static;
			methodDecl.Body = new BlockStatement ();
			methodDecl.Body.AddChild (new ThrowStatement (new ObjectCreateExpression (new TypeReference ("System.NotImplementedException"), null)));
			insertNewMethod.FileName = document.FileName;
			insertNewMethod.Description = string.Format (GettextCatalog.GetString ("Create new method {0}"), methodDecl.Name);
			insertNewMethod.Offset = document.TextEditor.GetPositionFromLineColumn (resolveResult.CallingMember.BodyRegion.End.Line, resolveResult.CallingMember.BodyRegion.End.Column);

			IResolver resolver = domParser.CreateResolver (dom, document, document.FileName);
			int i = 0;
			foreach (Expression expression in invoke.Arguments) {
				i++;
				string output = provider.OutputNode (dom, expression);

				string parameterName;
				if (Char.IsLetter (output[0]) || output[0] == '_') {
					parameterName = output;
				} else {
					parameterName = "par" + i;
				}

				ResolveResult resolveResult2 = resolver.Resolve (new ExpressionResult (output), loc);
				TypeReference typeReference = new TypeReference (resolveResult2.ResolvedType.ToInvariantString ());
				typeReference.IsKeyword = true;
				ParameterDeclarationExpression pde = new ParameterDeclarationExpression (typeReference, parameterName);
				methodDecl.Parameters.Add (pde);
			}

			insertNewMethod.InsertedText = Environment.NewLine + Environment.NewLine + provider.OutputNode (dom, methodDecl, GetIndent (document, resolveResult.CallingMember));
			result.Add (insertNewMethod);
			return result;
		}
		
		static string GetWhitespaces (MonoDevelop.Ide.Gui.Document doc, int insertionOffset)
		{
			StringBuilder result = new StringBuilder ();
			for (int i = insertionOffset; i < doc.TextEditor.TextLength; i++) {
				char ch = doc.TextEditor.GetCharAt (i);
				Console.WriteLine ("ch:" + ch + " " + ((int)ch));
				if (ch == ' ' || ch == '\t') {
					result.Append (ch);
				} else {
					break;
				}
			}
			return result.ToString ();
		}
		
		static string GetIndent (MonoDevelop.Ide.Gui.Document doc, IMember member)
		{
			return GetWhitespaces (doc, doc.TextEditor.GetPositionFromLineColumn (member.Location.Line, 1));
		}
	}
}
