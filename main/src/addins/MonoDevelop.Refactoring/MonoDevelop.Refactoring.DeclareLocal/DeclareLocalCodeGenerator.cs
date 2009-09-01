// 
// DeclareLocalCodeGenerator.cs
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

namespace MonoDevelop.Refactoring.DeclareLocal
{
	public class DeclareLocalCodeGenerator : RefactoringOperation
	{
		public override string AccelKey {
			get {
				return IdeApp.CommandService.GetCommandInfo (RefactoryCommands.DeclareLocal, null).AccelKey.Replace ("dead_circumflex", "^");
			}
		}
		
		public DeclareLocalCodeGenerator ()
		{
			Name = "Declare Local";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			if (options.SelectedItem != null)
				return false;
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return false;
			TextEditorData data = options.GetTextEditorData ();
			if (data.IsSomethingSelected)
				return provider.ParseText (data.SelectedText) is Expression;

			LineSegment lineSegment = data.Document.GetLine (data.Caret.Line);
			string line = data.Document.GetTextAt (lineSegment);
			Expression expression = provider.ParseExpression (line);
			BlockStatement block = provider.ParseText (line) as BlockStatement;
			if (expression == null || (block != null && block.Children[0] is LocalVariableDeclaration))
				return false;
			
			ResolveResult resolveResult = resolver.Resolve (new ExpressionResult (line), new DomLocation (options.Document.TextEditor.CursorLine, options.Document.TextEditor.CursorColumn));
			return resolveResult.ResolvedType != null && !string.IsNullOrEmpty (resolveResult.ResolvedType.FullName) && resolveResult.ResolvedType.FullName != DomReturnType.Void.FullName;
		}
		
		static HashSet<string> builtInTypes = new HashSet<string> (new string[] {
			"System.Void", "System.Object","System.Boolean","System.Byte", "System.SByte",
			"System.Char", "System.Enum", "System.Int16", "System.Int32", "System.Int64", 
			"System.UInt16", "System.UInt32", "System.UInt64", "System.Single", "System.Double", "System.Decimal",
			"System.String"
		});
		
		public string GetSimpleTypeName (RefactoringOptions options, string fullTypeName)
		{
			if (builtInTypes.Contains (fullTypeName))
				return fullTypeName;
			IType foundType = null;
			string curType = fullTypeName;
			while (foundType == null) {
				foundType = options.Dom.GetType (curType);
				int idx = curType.LastIndexOf ('.');
				if (idx < 0)
					break;
				curType = fullTypeName.Substring (0, idx);
			}

			if (foundType == null)
				foundType = new DomType (fullTypeName);
			if (options.Document.ParsedDocument != null) {
				foreach (IUsing u in options.Document.ParsedDocument.CompilationUnit.Usings) {
					foreach (string includedNamespace in u.Namespaces) {
						if (includedNamespace == foundType.Namespace)
							return fullTypeName.Substring (includedNamespace.Length + 1);
					}
				}
			}
			return fullTypeName;
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
			TextEditorData data = options.GetTextEditorData ();
			
			ResolveResult resolveResult;
			LineSegment lineSegment;
			if (data.IsSomethingSelected) {
				ExpressionResult expressionResult = new ExpressionResult (data.SelectedText.Trim ());
				if (expressionResult.Expression.Contains (" ") || expressionResult.Expression.Contains ("\t"))
					expressionResult.Expression = "(" + expressionResult.Expression + ")";
				resolveResult = resolver.Resolve (expressionResult, new DomLocation (data.Caret.Line, data.Caret.Column));
				if (resolveResult == null)
					return result;
				string varName = CreateVariableName (resolveResult.ResolvedType);
				TypeReference returnType;
				if (resolveResult.ResolvedType == null) {
					returnType = new TypeReference ("var");
				} else {
					
					returnType = options.ShortenTypeName (resolveResult.ResolvedType).ConvertToTypeReference ();
				}
				options.ParseMember (resolveResult.CallingMember);
	/*			
				Statement lastStatement = null;
				Location curLocation = new Location (data.Caret.Column, data.Caret.Line);
				foreach (Statement statement in options.ParseMember (options.ResolveResult.CallingMember).Children) {
					if (statement.StartLocation > curLocation) 
						break;
					lastStatement = statement;
				}
				if (lastStatement == null)
					return result;
				*/
				
				TextReplaceChange insert = new TextReplaceChange ();
				insert.FileName = options.Document.FileName;
				insert.Description = GettextCatalog.GetString ("Insert variable declaration");
				lineSegment = data.Document.GetLine (data.Caret.Line);
				insert.Offset = lineSegment.Offset;
				
				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (returnType);
				varDecl.Variables.Add (new VariableDeclaration (varName, provider.ParseExpression (data.SelectedText)));
				insert.InsertedText =  options.GetWhitespaces (lineSegment.Offset) +provider.OutputNode (options.Dom, varDecl) + Environment.NewLine;
				result.Add (insert);
				
				TextReplaceChange replace = new TextReplaceChange ();
				replace.FileName = options.Document.FileName;
				replace.Offset = data.SelectionRange.Offset;
				replace.RemovedChars = data.SelectionRange.Length;
				replace.InsertedText = varName;
				result.Add (replace);
				
				
				return result;
			}

			lineSegment = data.Document.GetLine (data.Caret.Line);
			string line = data.Document.GetTextAt (lineSegment);

			Expression expression = provider.ParseExpression (line);

			if (expression == null)
				return result;

			resolveResult = resolver.Resolve (new ExpressionResult (line), new DomLocation (options.Document.TextEditor.CursorLine, options.Document.TextEditor.CursorColumn));

			if (resolveResult.ResolvedType != null && !string.IsNullOrEmpty (resolveResult.ResolvedType.FullName)) {
				TextReplaceChange insert = new TextReplaceChange ();
				insert.FileName = options.Document.FileName;
				insert.Description = GettextCatalog.GetString ("Insert variable declaration");
				insert.Offset = lineSegment.Offset + options.GetWhitespaces (lineSegment.Offset).Length;
				string varName = CreateVariableName (resolveResult.ResolvedType);
				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (ConvertToTypeRef (options, resolveResult.ResolvedType));
				varDecl.Variables.Add (new VariableDeclaration (varName, expression));
				insert.RemovedChars = expression.EndLocation.Column - 1;
				insert.InsertedText = provider.OutputNode (options.Dom, varDecl);
				result.Add (insert);
				selectionStart = insert.Offset + insert.InsertedText.IndexOf (varName);
				selectionEnd = selectionStart + varName.Length;
			}
			
			return result;
		}

		static string CreateVariableName (MonoDevelop.Projects.Dom.IReturnType returnType)
		{
			if (returnType == null)
				return "aVar";
			return "a" + returnType.Name;
		}


		TypeReference ConvertToTypeRef (RefactoringOptions options, MonoDevelop.Projects.Dom.IReturnType returnType)
		{
			TypeReference result = new TypeReference (GetSimpleTypeName (options, returnType.FullName));
			result.IsKeyword = true;
			foreach (IReturnType generic in returnType.GenericArguments) {
				result.GenericTypes.Add (ConvertToTypeRef (options, generic));
			}
			return result;
		}

	}
}
