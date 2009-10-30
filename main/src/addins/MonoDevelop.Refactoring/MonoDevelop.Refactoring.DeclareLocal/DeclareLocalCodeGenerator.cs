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
				var cmdInfo = IdeApp.CommandService.GetCommandInfo (RefactoryCommands.DeclareLocal, null);
				if (cmdInfo != null && cmdInfo.AccelKey != null)
					return cmdInfo.AccelKey.Replace ("dead_circumflex", "^");
				return null;
			}
		}
		
		public DeclareLocalCodeGenerator ()
		{
			Name = "Declare Local";
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Declare Local");
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return false;
			TextEditorData data = options.GetTextEditorData ();
			ResolveResult resolveResult;
			if (data.IsSomethingSelected) {
				ExpressionResult expressionResult = new ExpressionResult (data.SelectedText.Trim ());
				if (expressionResult.Expression.Contains (" ") || expressionResult.Expression.Contains ("\t"))
					expressionResult.Expression = "(" + expressionResult.Expression + ")";
				resolveResult = resolver.Resolve (expressionResult, new DomLocation (data.Caret.Line, data.Caret.Column));
				if (resolveResult == null)
					return false;
				return true;
			}
			LineSegment lineSegment = data.Document.GetLine (data.Caret.Line);
			string line = data.Document.GetTextAt (lineSegment);
			Expression expression = provider.ParseExpression (line);
			BlockStatement block = provider.ParseText (line) as BlockStatement;
			if (expression == null || (block != null && block.Children[0] is LocalVariableDeclaration))
				return false;
			
			resolveResult = resolver.Resolve (new ExpressionResult (line), new DomLocation (options.Document.TextEditor.CursorLine, options.Document.TextEditor.CursorColumn));
			return resolveResult.ResolvedType != null && !string.IsNullOrEmpty (resolveResult.ResolvedType.FullName) && resolveResult.ResolvedType.FullName != DomReturnType.Void.FullName;
		}
		
		public override void Run (RefactoringOptions options)
		{
			base.Run (options);
			if (selectionEnd >= 0) {
				options.Document.TextEditor.CursorPosition = selectionEnd;
				options.Document.TextEditor.Select (selectionStart, selectionEnd);
			} else {
				Mono.TextEditor.TextEditor editor = MonoDevelop.Refactoring.Rename.RenameRefactoring.GetEditor (options.Document.ActiveView.Control);
				TextEditorData data = options.GetTextEditorData ();
				TextLink link = new TextLink ("name");
				for (int i = selectionStart; i < data.Document.Length - varName.Length; i++) {
					if (data.Document.GetTextAt (i, varName.Length) == varName && !IsIdentifierPart (data, i - 1) && !IsIdentifierPart (data, i + varName.Length)) {
						link.AddLink (new Segment (i - selectionStart, varName.Length));
						if (link.Count == 2)
							break;
					}
				}
				List<TextLink> links = new List<TextLink> ();
				links.Add (link);
				TextLinkEditMode tle = new TextLinkEditMode (editor, selectionStart, links);
				tle.SetCaretPosition = false;
				if (tle.ShouldStartTextLinkMode) {
					tle.OldMode = data.CurrentMode;
					tle.StartMode ();
					data.CurrentMode = tle;
				}
			}
		}

		static bool IsIdentifierPart (Mono.TextEditor.TextEditorData data, int offset)
		{
			if (offset < 0 || offset >= data.Document.Length)
				return false;
			char ch = data.Document.GetCharAt (offset);
			return char.IsLetterOrDigit (ch) || ch == '_' || ch == '.';
		}

		
		int selectionStart;
		int selectionEnd;
		string varName;
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			selectionStart = selectionEnd = -1;
			List<Change> result = new List<Change> ();
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return result;
			TextEditorData data = options.GetTextEditorData ();

			ResolveResult resolveResult;
			LineSegment lineSegment;
			ICSharpCode.NRefactory.Ast.CompilationUnit unit = provider.ParseFile (data.Document.Text);
			MonoDevelop.Refactoring.ExtractMethod.VariableLookupVisitor visitor = new MonoDevelop.Refactoring.ExtractMethod.VariableLookupVisitor (resolver, new DomLocation (data.Caret.Line + 1, data.Caret.Column + 1));
			visitor.MemberLocation = new Location (options.ResolveResult.CallingMember.Location.Column, options.ResolveResult.CallingMember.Location.Line);
			unit.AcceptVisitor (visitor, null);
			
			if (data.IsSomethingSelected) {
				ExpressionResult expressionResult = new ExpressionResult (data.SelectedText.Trim ());
				if (expressionResult.Expression.Contains (" ") || expressionResult.Expression.Contains ("\t"))
					expressionResult.Expression = "(" + expressionResult.Expression + ")";
				resolveResult = resolver.Resolve (expressionResult, new DomLocation (data.Caret.Line, data.Caret.Column));
				if (resolveResult == null)
					return result;
				IReturnType resolvedType = resolveResult.ResolvedType;
				if (resolvedType == null || string.IsNullOrEmpty (resolvedType.Name))
					resolvedType = DomReturnType.Object;
				varName = CreateVariableName (resolvedType, visitor);
				TypeReference returnType;
				if (resolveResult.ResolvedType == null || string.IsNullOrEmpty (resolveResult.ResolvedType.Name)) {
					returnType = new TypeReference ("var");
					returnType.IsKeyword = true;
				} else {
					returnType = options.ShortenTypeName (resolveResult.ResolvedType).ConvertToTypeReference ();
				}
				options.ParseMember (resolveResult.CallingMember);
				
				TextReplaceChange insert = new TextReplaceChange ();
				insert.FileName = options.Document.FileName;
				insert.Description = GettextCatalog.GetString ("Insert variable declaration");
				
				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (returnType);
				varDecl.Variables.Add (new VariableDeclaration (varName, provider.ParseExpression (data.SelectedText)));
				
				GetContainingEmbeddedStatementVisitor blockVisitor = new GetContainingEmbeddedStatementVisitor ();
				blockVisitor.LookupLocation = new Location (data.Caret.Column + 1, data.Caret.Line + 1);
				
				unit.AcceptVisitor (blockVisitor, null);
				
				StatementWithEmbeddedStatement containing = blockVisitor.ContainingStatement as StatementWithEmbeddedStatement;
				
				if (containing != null && !(containing.EmbeddedStatement is BlockStatement)) {
					insert.Offset = data.Document.LocationToOffset (containing.StartLocation.Line - 1, containing.StartLocation.Column - 1);
					lineSegment = data.Document.GetLineByOffset (insert.Offset);
					insert.RemovedChars = data.Document.LocationToOffset (containing.EndLocation.Line - 1, containing.EndLocation.Column - 1) - insert.Offset;
					BlockStatement insertedBlock = new BlockStatement ();
					insertedBlock.AddChild (varDecl);
					insertedBlock.AddChild (containing.EmbeddedStatement);
					
					containing.EmbeddedStatement = insertedBlock;
					insert.InsertedText = provider.OutputNode (options.Dom, containing, options.GetWhitespaces (lineSegment.Offset)).TrimStart ();
					int offset, length;
					if (SearchSubExpression (insert.InsertedText, data.SelectedText, 0, out offset, out length)) 
					if (SearchSubExpression (insert.InsertedText, data.SelectedText, offset + 1, out offset, out length)) {
						insert.InsertedText = insert.InsertedText.Substring (0, offset) + varName + insert.InsertedText.Substring (offset + length);
					}
					
				} else if (blockVisitor.ContainingStatement is IfElseStatement) {
					IfElseStatement ifElse = blockVisitor.ContainingStatement as IfElseStatement;
					
					insert.Offset = data.Document.LocationToOffset (blockVisitor.ContainingStatement.StartLocation.Line - 1, blockVisitor.ContainingStatement.StartLocation.Column - 1);
					lineSegment = data.Document.GetLineByOffset (insert.Offset);
					insert.RemovedChars = data.Document.LocationToOffset (blockVisitor.ContainingStatement.EndLocation.Line - 1, blockVisitor.ContainingStatement.EndLocation.Column - 1) - insert.Offset;
					BlockStatement insertedBlock = new BlockStatement ();
					insertedBlock.AddChild (varDecl);
					if (blockVisitor.ContainsLocation (ifElse.TrueStatement[0])) {
						insertedBlock.AddChild (ifElse.TrueStatement[0]);
						ifElse.TrueStatement[0] = insertedBlock;
					} else {
						insertedBlock.AddChild (ifElse.FalseStatement[0]);
						ifElse.FalseStatement[0] = insertedBlock;
					}
					
					insert.InsertedText = provider.OutputNode (options.Dom, blockVisitor.ContainingStatement, options.GetWhitespaces (lineSegment.Offset));
					int offset, length;
					
					if (SearchSubExpression (insert.InsertedText, provider.OutputNode (options.Dom, insertedBlock), 0, out offset, out length)) 
					if (SearchSubExpression (insert.InsertedText, data.SelectedText, offset + 1, out offset, out length)) 
					if (SearchSubExpression (insert.InsertedText, data.SelectedText, offset + 1, out offset, out length)) {
						insert.InsertedText = insert.InsertedText.Substring (0, offset) + varName + insert.InsertedText.Substring (offset + length);
					}
				} else {
					lineSegment = data.Document.GetLine (data.Caret.Line);
					insert.Offset = lineSegment.Offset;
					insert.InsertedText =  options.GetWhitespaces (lineSegment.Offset) + provider.OutputNode (options.Dom, varDecl) + Environment.NewLine;
					
					TextReplaceChange replace = new TextReplaceChange ();
					replace.FileName = options.Document.FileName;
					replace.Offset = data.SelectionRange.Offset;
					replace.RemovedChars = data.SelectionRange.Length;
					replace.InsertedText = varName;
					result.Add (replace);
				}
				result.Add (insert);
				selectionStart = insert.Offset;
				
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
				varName = CreateVariableName (resolveResult.ResolvedType, visitor);
				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (options.ShortenTypeName (resolveResult.ResolvedType).ConvertToTypeReference ());
				varDecl.Variables.Add (new VariableDeclaration (varName, expression));
				insert.RemovedChars = expression.EndLocation.Column - 1;
				insert.InsertedText = provider.OutputNode (options.Dom, varDecl);
				result.Add (insert);
				selectionStart = insert.Offset + insert.InsertedText.IndexOf (varName);
				selectionEnd = selectionStart + varName.Length;
			}
			
			return result;
		}

		static bool SearchSubExpression (string expression, string subexpression, int startOffset, out int offset, out int length)
		{
			length = -1;
			for (offset = startOffset; offset < expression.Length; offset++) {
				if (Char.IsWhiteSpace (expression[offset])) 
					continue;
				
				bool mismatch = false;
				int i = offset, j = 0;
				while (i < expression.Length && j < subexpression.Length) {
					if (Char.IsWhiteSpace (expression[i])) {
						i++;
						continue;
					}
					if (Char.IsWhiteSpace (subexpression[j])) {
						j++;
						continue;
					}
					if (expression[i] != subexpression[j]) {
						mismatch = true;
						break;
					}
					i++;
					j++;
				}
				if (!mismatch && j > 0) {
					length = j;
					return true;
				}
			}
			return false;
		}
		
		static string[] GetPossibleName (MonoDevelop.Projects.Dom.IReturnType returnType)
		{
			switch (returnType.FullName) {
			case "System.Byte":
			case "System.SByte":
				return new [] { "b" };
				
			case "System.Int16":
			case "System.UInt16":
			case "System.Int32":
			case "System.UInt32":
			case "System.Int64":
			case "System.UInt64":
				return new [] { "i", "j", "k", "l" };
				
			case "System.Bool":
				return new [] {"b"};
				
			case "System.DateTime":
				return new [] { "date", "d" };
				
			case "System.Char":
				return new [] {"ch", "c"};
			case "System.String":
				return new [] {"str", "s"};
				
			case "System.Exception":
				return new [] {"e"};
			case "System.Object":
				return new [] {"obj", "o"};
			}
			if (Char.IsLower (returnType.Name[0]))
				return new [] { "a" + Char.ToUpper (returnType.Name[0]) + returnType.Name.Substring (1) };
			
			return new [] { Char.ToLower (returnType.Name[0]) + returnType.Name.Substring (1) };
		}
		
		static string CreateVariableName (MonoDevelop.Projects.Dom.IReturnType returnType, MonoDevelop.Refactoring.ExtractMethod.VariableLookupVisitor visitor)
		{
			string[] possibleNames = GetPossibleName (returnType);
			foreach (string name in possibleNames) {
				if (!VariableExists (visitor, name))
					return name;
			}
			foreach (string name in possibleNames) {
				for (int i = 1; i < 99; i++) {
					if (!VariableExists (visitor, name + i.ToString ()))
						return name + i.ToString ();
				}
			}
			return "a" + returnType.Name;
		}

		static bool VariableExists (ExtractMethod.VariableLookupVisitor visitor, string name)
		{
			foreach (var descriptor in visitor.Variables.Values) {
				if (descriptor.Name == name)
					return true;
			}
			return false;
		}
	}
}
