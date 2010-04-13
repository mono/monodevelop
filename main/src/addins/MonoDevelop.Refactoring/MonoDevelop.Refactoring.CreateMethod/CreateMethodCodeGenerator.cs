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

using ICSharpCode.NRefactory.Ast;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;

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
			if (invoke == null)
				return false;
			if (invoke.TargetObject is MemberReferenceExpression) {
				INRefactoryASTProvider provider = options.GetASTProvider ();
				IResolver resolver = options.GetResolver ();
				if (provider == null || resolver == null)
					return false;
				string callingObject = provider.OutputNode (options.Dom, ((MemberReferenceExpression)invoke.TargetObject).TargetObject);
				ResolveResult resolveResult = resolver.Resolve (new ExpressionResult (callingObject), new DomLocation (options.Document.TextEditor.CursorLine, options.Document.TextEditor.CursorColumn));
				if (resolveResult == null || resolveResult.ResolvedType == null || resolveResult.CallingType == null)
					return false;
				IType type = options.Dom.GetType (resolveResult.ResolvedType);
				return type != null && type.CompilationUnit != null && File.Exists (type.CompilationUnit.FileName) && RefactoringService.GetASTProvider (DesktopService.GetMimeTypeForUri (type.CompilationUnit.FileName)) != null;
			}
			return invoke.TargetObject is IdentifierExpression;
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
						if (offset < startPos)
							return null;
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
			MonoDevelop.Ide.Gui.Document document = IdeApp.Workbench.OpenDocument (fileName);
			TextEditorData data = document.TextEditorData;
			if (data != null) {
				data.ClearSelection ();
				data.Caret.Offset = selectionEnd;
				data.SetSelection (selectionStart, selectionEnd);
			}
		}
		
		static bool IsValidIdentifier (string name)
		{
			if (string.IsNullOrEmpty (name) || !(name[0] == '_' || char.IsLetter (name[0])))
				return false;
			for (int i = 1; i < name.Length; i++) {
				if (!(name[i] == '_' || char.IsLetter (name[i])))
					return false;
			}
			return true;
		}
		
		string fileName;
		int selectionStart;
		int selectionEnd;
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			List<Change> result = new List<Change> ();
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return result;
			TextEditorData data = options.GetTextEditorData ();
			TextReplaceChange insertNewMethod = new TextReplaceChange ();
			insertNewMethod.InsertedText = "";
			string indent = "";
			MethodDeclaration methodDecl = new MethodDeclaration ();
			bool isInInterface = false;
			if (invoke.TargetObject is IdentifierExpression) {
				insertNewMethod.FileName = options.Document.FileName;
				insertNewMethod.Offset = options.Document.TextEditor.GetPositionFromLineColumn (options.ResolveResult.CallingMember.BodyRegion.End.Line, options.ResolveResult.CallingMember.BodyRegion.End.Column);
				methodDecl.Name = ((IdentifierExpression)invoke.TargetObject).Identifier;
				indent = options.GetIndent (options.ResolveResult.CallingMember);
				insertNewMethod.InsertedText = Environment.NewLine;
				if (options.ResolveResult.CallingMember.IsStatic)
					methodDecl.Modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Static;
			
			} else {
				methodDecl.Name = ((MemberReferenceExpression)invoke.TargetObject).MemberName;
				string callingObject = provider.OutputNode (options.Dom, ((MemberReferenceExpression)invoke.TargetObject).TargetObject);
				ResolveResult resolveResult = resolver.Resolve (new ExpressionResult (callingObject), new DomLocation (options.Document.TextEditor.CursorLine, options.Document.TextEditor.CursorColumn));
				IType type = options.Dom.GetType (resolveResult.ResolvedType);
				insertNewMethod.FileName = type.CompilationUnit.FileName;
				if (resolveResult.StaticResolve)
					methodDecl.Modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Static;
				
				if (insertNewMethod.FileName == options.Document.FileName) {
					indent = options.GetIndent (options.ResolveResult.CallingMember);
					insertNewMethod.InsertedText = Environment.NewLine;
					insertNewMethod.Offset = options.Document.TextEditor.GetPositionFromLineColumn (options.ResolveResult.CallingMember.BodyRegion.End.Line, options.ResolveResult.CallingMember.BodyRegion.End.Column);
				} else {
					TextEditorData otherFile = TextReplaceChange.GetTextEditorData (insertNewMethod.FileName);
					if (otherFile == null) {
						IdeApp.Workbench.OpenDocument (insertNewMethod.FileName);
						otherFile = TextReplaceChange.GetTextEditorData (insertNewMethod.FileName);
					}
					methodDecl.Modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Public;
					isInInterface = type.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface;
					if (isInInterface) 
						methodDecl.Modifier = ICSharpCode.NRefactory.Ast.Modifiers.None;
					if (otherFile == null)
						throw new InvalidOperationException ("Can't open file:" + insertNewMethod.FileName);
					try {
						indent = otherFile.Document.GetLine (type.Location.Line - 1).GetIndentation (otherFile.Document) ?? "";
					} catch (Exception) {
						indent = "";
					}
					indent += "\t";
					insertNewMethod.Offset = otherFile.Document.LocationToOffset (type.BodyRegion.End.Line - 1, 0);
				}
				
			}
			methodDecl.TypeReference = new TypeReference ("System.Void");
			methodDecl.TypeReference.IsKeyword = true;
			if (!isInInterface) {
				methodDecl.Body = new BlockStatement ();
				methodDecl.Body.AddChild (new ThrowStatement (new ObjectCreateExpression (new TypeReference ("System.NotImplementedException"), null)));
			}
			insertNewMethod.Description = string.Format (GettextCatalog.GetString ("Create new method {0}"), methodDecl.Name);

			int i = 0;
			foreach (Expression expression in invoke.Arguments) {
				i++;
				string output = provider.OutputNode (options.Dom, expression);

				string parameterName = "par" + i;
				int idx = output.LastIndexOf ('.');
				string lastName = output.Substring (idx + 1); // start from 0, if '.' wasn't found
				if (IsValidIdentifier (lastName)) 
					parameterName = lastName;
				
				ResolveResult resolveResult2 = resolver.Resolve (new ExpressionResult (output), options.ResolveResult.ResolvedExpression.Region.Start);
				
				TypeReference typeReference = new TypeReference ((resolveResult2 != null && resolveResult2.ResolvedType != null) ? options.Document.CompilationUnit.ShortenTypeName (resolveResult2.ResolvedType, data.Caret.Line, data.Caret.Column).ToInvariantString () : "System.Object");
				typeReference.IsKeyword = true;
				ParameterDeclarationExpression pde = new ParameterDeclarationExpression (typeReference, parameterName);
				methodDecl.Parameters.Add (pde);
			}

			insertNewMethod.InsertedText += Environment.NewLine + provider.OutputNode (options.Dom, methodDecl, indent);
			result.Add (insertNewMethod);
			fileName = insertNewMethod.FileName;
			if (!isInInterface) {
				int idx = insertNewMethod.InsertedText.IndexOf ("throw");
				selectionStart = insertNewMethod.Offset + idx;
				selectionEnd   = insertNewMethod.Offset + insertNewMethod.InsertedText.IndexOf (';', idx) + 1;
			} else {
				selectionStart = selectionEnd = insertNewMethod.Offset;
			}
			return result;
		}
		
	}
}
