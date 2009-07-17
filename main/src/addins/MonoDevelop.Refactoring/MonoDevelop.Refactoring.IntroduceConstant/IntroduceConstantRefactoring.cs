// 
// IntroduceConstantRefactoring.cs
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
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Core;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.Refactoring.IntroduceConstant
{
	public class IntroduceConstantRefactoring : RefactoringOperation
	{
		public class Parameters
		{
			public string Name {
				get;
				set;
			}
			
			public ICSharpCode.NRefactory.Ast.Modifiers Modifiers {
				get;
				set;
			}
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			TextEditorData data = options.GetTextEditorData ();
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			if (line != null) {
				Stack<Span> stack = line.StartSpan != null ? new Stack<Span> (line.StartSpan) : new Stack<Span> ();
				Mono.TextEditor.Highlighting.SyntaxModeService.ScanSpans (data.Document, data.Document.SyntaxMode, data.Document.SyntaxMode, stack, line.Offset, data.Caret.Offset);
				foreach (Span span in stack) {
					if (span.Color == "string.single" || span.Color == "string.double")
						return options.Document.CompilationUnit.GetMemberAt (data.Caret.Line, data.Caret.Column) != null;
				}
			}

			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (provider == null)
				return false;
			string expressionText = null;
			if (options.ResolveResult != null)
				expressionText = options.ResolveResult.ResolvedExpression.Expression;

			if (string.IsNullOrEmpty (expressionText)) {
				int start, end;
				expressionText = SearchNumber (data, out start, out end);
			}

			Expression expression = provider.ParseExpression (expressionText);
			return expression is PrimitiveExpression;
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Introduce Constant...");
		}

		public override void Run (RefactoringOptions options)
		{
			IntroduceConstantDialog dialog = new IntroduceConstantDialog (this, options, new Parameters ());
			dialog.Show ();
		}
		
		string SearchString (TextEditorData data, char quote, out int start, out int end)
		{
			start = data.Caret.Offset;
			while (start > 0) {
				if (data.Document.GetCharAt (start) == quote)
					break;
				start--;
			}
			end = data.Caret.Offset;
			while (end < data.Document.Length) {
				if (data.Document.GetCharAt (end) == quote)
					break;
				end++;
			}
			return data.Document.GetTextBetween (start, end);
		}
		
		string SearchNumber (TextEditorData data, out int start, out int end)
		{
			start = data.Caret.Offset;
			while (start > 0 && start < data.Document.Length) {
				char ch = data.Document.GetCharAt (start);
				if (!(Char.IsNumber (ch) || ch == '.' || Char.ToUpper (ch) == 'E' || ch == '+' || ch == '-')) {
					start++;
					break;
				}
				start--;
			}
			end = data.Caret.Offset;
			while (end >= 0 && end < data.Document.Length) {
				char ch = data.Document.GetCharAt (end);
				if (!(Char.IsNumber (ch) || ch == '.' || Char.ToUpper (ch) == 'E' || ch == '+' || ch == '-'))
					break;
				end++;
			}
			return start < end ? data.Document.GetTextBetween (start, end) : "";
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object properties)
		{
			List<Change> result = new List<Change> ();
			Parameters param = properties as Parameters;
			if (param == null)
				return result;
			TextEditorData data = options.GetTextEditorData ();
			IResolver resolver = options.GetResolver ();
			IMember curMember = options.Document.CompilationUnit.GetMemberAt (data.Caret.Line, data.Caret.Column);
			ResolveResult resolveResult = options.ResolveResult;
			int start = 0;
			int end = 0;
			if (resolveResult == null) {
				LineSegment line = data.Document.GetLine (data.Caret.Line);
				if (line != null) {
					Stack<Span> stack = line.StartSpan != null ? new Stack<Span> (line.StartSpan) : new Stack<Span> ();
					Mono.TextEditor.Highlighting.SyntaxModeService.ScanSpans (data.Document, data.Document.SyntaxMode, data.Document.SyntaxMode, stack, line.Offset, data.Caret.Offset);
					foreach (Span span in stack) {
						if (span.Color == "string.single" || span.Color == "string.double") {
							resolveResult = resolver.Resolve (new ExpressionResult (SearchString (data, span.Color == "string.single" ? '\'' : '"', out start, out end)), DomLocation.Empty);
							end++;
						}
					}
				}
				if (end == 0) {
					resolveResult = resolver.Resolve (new ExpressionResult (SearchNumber (data, out start, out end)), DomLocation.Empty);
				}
			} else {
				start = data.Document.LocationToOffset (resolveResult.ResolvedExpression.Region.Start.Line - 1, resolveResult.ResolvedExpression.Region.Start.Column - 1);
				end = data.Document.LocationToOffset (resolveResult.ResolvedExpression.Region.End.Line - 1, resolveResult.ResolvedExpression.Region.End.Column - 1);
			}
			if (start == 0 && end == 0)
				return result;
			INRefactoryASTProvider provider = options.GetASTProvider ();

			FieldDeclaration fieldDeclaration = new FieldDeclaration (null);
			VariableDeclaration varDecl = new VariableDeclaration (param.Name);
			varDecl.Initializer = provider.ParseExpression (resolveResult.ResolvedExpression.Expression);
			fieldDeclaration.Fields.Add (varDecl);
			fieldDeclaration.Modifier = param.Modifiers;
			fieldDeclaration.Modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Const;
			fieldDeclaration.TypeReference = new TypeReference (resolveResult.ResolvedType.ToInvariantString ());
			fieldDeclaration.TypeReference.IsKeyword = true;

			TextReplaceChange insertConstant = new TextReplaceChange ();
			insertConstant.FileName = options.Document.FileName;
			insertConstant.Description = string.Format (GettextCatalog.GetString ("Generate constant '{0}'"), param.Name);
			insertConstant.Offset = data.Document.LocationToOffset (curMember.Location.Line - 1, 0);
			insertConstant.InsertedText = provider.OutputNode (options.Dom, fieldDeclaration, options.GetIndent (curMember)) + Environment.NewLine;
			result.Add (insertConstant);

			TextReplaceChange replaceConstant = new TextReplaceChange ();
			replaceConstant.FileName = options.Document.FileName;
			replaceConstant.Description = string.Format (GettextCatalog.GetString ("Replace expression with constant '{0}'"), param.Name);
			replaceConstant.Offset = start;
			replaceConstant.RemovedChars = end - start;
			replaceConstant.InsertedText = param.Name;
			result.Add (replaceConstant);

			return result;
		}
	}
}
