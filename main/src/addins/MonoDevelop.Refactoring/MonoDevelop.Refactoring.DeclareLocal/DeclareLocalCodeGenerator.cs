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
					if (data.Document.GetTextAt (i, varName.Length) == varName) {
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
			
			if (data.IsSomethingSelected) {
				ExpressionResult expressionResult = new ExpressionResult (data.SelectedText.Trim ());
				if (expressionResult.Expression.Contains (" ") || expressionResult.Expression.Contains ("\t"))
					expressionResult.Expression = "(" + expressionResult.Expression + ")";
				resolveResult = resolver.Resolve (expressionResult, new DomLocation (data.Caret.Line, data.Caret.Column));
				if (resolveResult == null)
					return result;
				varName = CreateVariableName (resolveResult.ResolvedType);
				TypeReference returnType;
				if (resolveResult.ResolvedType == null) {
					returnType = new TypeReference ("var");
				} else {
					returnType = options.ShortenTypeName (resolveResult.ResolvedType).ConvertToTypeReference ();
				}
				options.ParseMember (resolveResult.CallingMember);
				
				TextReplaceChange insert = new TextReplaceChange ();
				insert.FileName = options.Document.FileName;
				insert.Description = GettextCatalog.GetString ("Insert variable declaration");
				lineSegment = data.Document.GetLine (data.Caret.Line);
				insert.Offset = lineSegment.Offset;
				
				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (returnType);
				varDecl.Variables.Add (new VariableDeclaration (varName, provider.ParseExpression (data.SelectedText)));
				insert.InsertedText =  options.GetWhitespaces (lineSegment.Offset) +provider.OutputNode (options.Dom, varDecl) + Environment.NewLine;
				result.Add (insert);
				selectionStart = insert.Offset;
				
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
				varName = CreateVariableName (resolveResult.ResolvedType);
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

		static string CreateVariableName (MonoDevelop.Projects.Dom.IReturnType returnType)
		{
			if (returnType == null)
				return "aVar";
			return "a" + returnType.Name;
		}

	}
}
