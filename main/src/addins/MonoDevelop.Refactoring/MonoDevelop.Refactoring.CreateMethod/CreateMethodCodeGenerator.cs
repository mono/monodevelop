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
using System.Text;

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
			INRefactoryASTProvider provider = options.GetASTProvider ();
			IResolver resolver = options.GetResolver ();
			if (provider == null || resolver == null)
				return false;
			if (invoke == null)
				invoke = GetInvocationExpression (options);
			if (invoke == null)
				return false;
			returnType = DomReturnType.Void;
			modifiers = ICSharpCode.NRefactory.Ast.Modifiers.None;
			resolvePosition = new DomLocation (options.Document.TextEditor.CursorLine, options.Document.TextEditor.CursorColumn);
			ResolveResult resolveResult = resolver.Resolve (new ExpressionResult (provider.OutputNode (options.Dom, invoke)), resolvePosition);
			
			if (resolveResult is MethodResolveResult) {
				MethodResolveResult mrr = (MethodResolveResult)resolveResult ;
				if (mrr.ExactMethodMatch)
					return false;
				returnType = mrr.MostLikelyMethod.ReturnType;
				modifiers = (ICSharpCode.NRefactory.Ast.Modifiers)mrr.MostLikelyMethod.Modifiers;
			}
			
			if (invoke.TargetObject is MemberReferenceExpression) {
				string callingObject = provider.OutputNode (options.Dom, ((MemberReferenceExpression)invoke.TargetObject).TargetObject);
				resolveResult = resolver.Resolve (new ExpressionResult (callingObject), resolvePosition);
				if (resolveResult == null || resolveResult.ResolvedType == null || resolveResult.CallingType == null)
					return false;
				IType type = options.Dom.GetType (resolveResult.ResolvedType);
				return type != null && type.CompilationUnit != null && File.Exists (type.CompilationUnit.FileName) && RefactoringService.GetASTProvider (DesktopService.GetMimeTypeForUri (type.CompilationUnit.FileName)) != null;
			}
			return invoke.TargetObject is IdentifierExpression;
		}
		
		InvocationExpression invoke;
		IReturnType returnType = DomReturnType.Void;
		ICSharpCode.NRefactory.Ast.Modifiers modifiers = ICSharpCode.NRefactory.Ast.Modifiers.None;
		DomLocation resolvePosition;
		InsertionPoint insertionPoint;
		int insertionOffset;
		
		InvocationExpression GetInvocationExpression (RefactoringOptions options)
		{
			TextEditorData data = options.GetTextEditorData ();
			if (data == null || options.ResolveResult == null || options.ResolveResult.ResolvedExpression == null)
				return null;
			string expression = options.ResolveResult.ResolvedExpression.Expression;
			if (!expression.Contains ("(")) {
				int startPos = data.Document.LocationToOffset (options.ResolveResult.ResolvedExpression.Region.Start.Line - 1, options.ResolveResult.ResolvedExpression.Region.Start.Column - 1);
				if (startPos < 0)
					return null;
				bool gotWs = false;
				for (int pos = startPos; pos > 2; pos--) {
					char ch = data.Document.GetCharAt (pos);
					if (char.IsWhiteSpace (ch)) {
						if (gotWs)
							break;
						gotWs = true;
						continue;
					}
					if (gotWs && ch == 'w' && data.Document.GetCharAt (pos - 1) == 'e' && data.Document.GetCharAt (pos - 2) == 'n')
						return null;
				}
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
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			TextEditorData data = options.GetTextEditorData ();
			IType type = options.ResolveResult.CallingType;
			Console.WriteLine ("target:" + invoke.TargetObject);
			if (invoke.TargetObject is IdentifierExpression) {
				fileName = options.Document.FileName;
				newMethodName = ((IdentifierExpression)invoke.TargetObject).Identifier;
				indent = options.GetIndent (options.ResolveResult.CallingMember);
				if (options.ResolveResult.CallingMember.IsStatic)
					modifiers |= ICSharpCode.NRefactory.Ast.Modifiers.Static;
			} else {
				newMethodName = ((MemberReferenceExpression)invoke.TargetObject).MemberName;
				string callingObject = provider.OutputNode (options.Dom, ((MemberReferenceExpression)invoke.TargetObject).TargetObject);
				ResolveResult resolveResult = resolver.Resolve (new ExpressionResult (callingObject), resolvePosition);
				type = options.Dom.GetType (resolveResult.ResolvedType);
				fileName = type.CompilationUnit.FileName;
				if (resolveResult.StaticResolve)
					modifiers |= ICSharpCode.NRefactory.Ast.Modifiers.Static;
				
				if (fileName == options.Document.FileName) {
					indent = options.GetIndent (options.ResolveResult.CallingMember);
//					insertNewMethod.Offset = options.Document.TextEditor.GetPositionFromLineColumn (options.ResolveResult.CallingMember.BodyRegion.End.Line, options.ResolveResult.CallingMember.BodyRegion.End.Column);
				} else {
					var openDocument = IdeApp.Workbench.OpenDocument (fileName);
					data = openDocument.TextEditorData;
					if (data == null)
						return;
					modifiers |= ICSharpCode.NRefactory.Ast.Modifiers.Public;
					bool isInInterface = type.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface;
					if (isInInterface) 
						modifiers = ICSharpCode.NRefactory.Ast.Modifiers.None;
					if (data == null)
						throw new InvalidOperationException ("Can't open file:" + modifiers);
					try {
						indent = data.Document.GetLine (type.Location.Line - 1).GetIndentation (data.Document) ?? "";
					} catch (Exception) {
						indent = "";
					}
					indent += "\t";
//					insertNewMethod.Offset = otherFile.Document.LocationToOffset (type.BodyRegion.End.Line - 1, 0);
				}
			}
			
			InsertionCursorEditMode mode = new InsertionCursorEditMode (data.Parent, HelperMethods.GetInsertionPoints (data.Document, type));
			if (fileName == options.Document.FileName) {
				for (int i = 0; i < mode.InsertionPoints.Count; i++) {
					var point = mode.InsertionPoints[i];
					if (point.Location < data.Caret.Location) {
						mode.CurIndex = i;
					} else {
						break;
					}
				}
			}
			
			mode.StartMode ();
			mode.Exited += delegate(object s, InsertionCursorEventArgs args) {
				if (args.Success) {
					insertionPoint = args.InsertionPoint;
					insertionOffset = data.Document.LocationToOffset (args.InsertionPoint.Location);
					base.Run (options);
					if (string.IsNullOrEmpty (fileName))
						return;
					MonoDevelop.Ide.Gui.Document document = IdeApp.Workbench.OpenDocument (fileName);
					TextEditorData docData = document.TextEditorData;
					if (docData != null) {
						docData.ClearSelection ();
						docData.Caret.Offset = selectionEnd;
						docData.SetSelection (selectionStart, selectionEnd);
					}
				}
			};
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
		
		string fileName, newMethodName, indent;
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
			insertNewMethod.FileName = fileName;
			insertNewMethod.RemovedChars = insertionPoint.LineBefore == NewLineInsertion.Eol ? 0 : insertionPoint.Location.Column;
			insertNewMethod.Offset = insertionOffset - insertNewMethod.RemovedChars;
			MethodDeclaration methodDecl = new MethodDeclaration ();
			bool isInInterface = false;
			
			methodDecl.Modifier = modifiers;
			methodDecl.TypeReference = HelperMethods.ConvertToTypeReference (returnType);
			methodDecl.Name = newMethodName;
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
				
				ResolveResult resolveResult2 = resolver.Resolve (new ExpressionResult (output), resolvePosition);
				
				TypeReference typeReference = new TypeReference ((resolveResult2 != null && resolveResult2.ResolvedType != null) ? options.Document.CompilationUnit.ShortenTypeName (resolveResult2.ResolvedType, data.Caret.Line, data.Caret.Column).ToInvariantString () : "System.Object");
				typeReference.IsKeyword = true;
				ParameterDeclarationExpression pde = new ParameterDeclarationExpression (typeReference, parameterName);
				methodDecl.Parameters.Add (pde);
			}
			StringBuilder sb = new StringBuilder ();
			switch (insertionPoint.LineBefore) {
			case NewLineInsertion.Eol:
				sb.AppendLine ();
				break;
			case NewLineInsertion.BlankLine:
				sb.Append (indent);
				sb.AppendLine ();
				break;
			}
			sb.Append (provider.OutputNode (options.Dom, methodDecl, indent).TrimEnd ('\n', '\r'));
			switch (insertionPoint.LineAfter) {
			case NewLineInsertion.Eol:
				sb.AppendLine ();
				break;
			case NewLineInsertion.BlankLine:
				sb.AppendLine ();
				sb.AppendLine ();
				sb.Append (indent);
				break;
			}
			insertNewMethod.InsertedText = sb.ToString ();
			result.Add (insertNewMethod);
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
