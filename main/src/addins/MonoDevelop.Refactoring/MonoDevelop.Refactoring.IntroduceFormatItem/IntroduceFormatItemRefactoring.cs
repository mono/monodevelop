// 
// IntroduceFormatRefactoring.cs
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


namespace MonoDevelop.Refactoring.IntroduceFormat
{
	public class IntroduceFormatItemRefactoring : RefactoringOperation
	{
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("Introduce Format Item");
		}

		public override bool IsValid (RefactoringOptions options)
		{
			TextEditorData data = options.GetTextEditorData ();
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			if (data.IsSomethingSelected && line != null) {
				Stack<Span> stack = line.StartSpan != null ? new Stack<Span> (line.StartSpan) : new Stack<Span> ();
				Mono.TextEditor.Highlighting.SyntaxModeService.ScanSpans (data.Document, data.Document.SyntaxMode, data.Document.SyntaxMode, stack, line.Offset, data.Caret.Offset);
				foreach (Span span in stack) {
					if (span.Color == "string.double") {
						int start, end;
						string str = MonoDevelop.Refactoring.IntroduceConstant.IntroduceConstantRefactoring.SearchString (data, '"', out start, out end);
						end = System.Math.Min (end, line.Offset + line.EditableLength);
						return str.StartsWith ("\"") && str.EndsWith ("\"") && data.SelectionRange.Offset < end;
					}
				}
			}
			return false;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object properties)
		{
			TextEditorData data = options.GetTextEditorData ();
			int start, end;
			MonoDevelop.Refactoring.IntroduceConstant.IntroduceConstantRefactoring.SearchString (data, '"', out start, out end);
			LineSegment line = data.Document.GetLineByOffset (start);
			
			int closingTagLength = 1; // length of the closing "
			
			if (end > line.Offset + line.EditableLength) { // assume missing closing tag
				end = line.Offset + line.EditableLength;
				closingTagLength = 0;
			}
			
			INRefactoryASTProvider provider = options.GetASTProvider ();

			List<Expression> args = new List<Expression> ();
			IExpressionFinder expressionFinder = options.GetParser ().CreateExpressionFinder (options.Dom);
			int expressionStart = start - 1;
			while (expressionStart > 0) {
				if (data.Document.GetCharAt (expressionStart) == '(') {
					expressionStart--;
					break;
				}
				expressionStart--;
			}
			
			// Add parameter to existing string.format call
			ExpressionResult expressionResult = expressionFinder.FindFullExpression (options.Document.TextEditor.Text, expressionStart);
			InvocationExpression formatCall = null;
			if (expressionResult != null) {
				InvocationExpression possibleFormatCall = provider.ParseExpression (expressionResult.Expression) as InvocationExpression;
				if (possibleFormatCall != null && possibleFormatCall.TargetObject is MemberReferenceExpression && ((MemberReferenceExpression)possibleFormatCall.TargetObject).MemberName == "Format") {
					PrimitiveExpression expr = possibleFormatCall.Arguments[0] as PrimitiveExpression;
					if (expr != null) {
						expr.Value = data.Document.GetTextBetween (start + 1, data.SelectionRange.Offset) + 
							"{" + (possibleFormatCall.Arguments.Count - 1) + "}" +
								data.Document.GetTextBetween (data.SelectionRange.EndOffset, end);
						possibleFormatCall.Arguments.Add (new PrimitiveExpression (data.Document.GetTextAt (data.SelectionRange)));
						formatCall = possibleFormatCall;
						start = data.Document.LocationToOffset (expressionResult.Region.Start.Line - 1, expressionResult.Region.Start.Column - 1);
						end = data.Document.LocationToOffset (expressionResult.Region.End.Line - 1, expressionResult.Region.End.Column - 1) - 1;
					}
				}
			}

			// insert new format call
			if (formatCall == null) {
				string formattedString = data.Document.GetTextBetween (start + 1, data.SelectionRange.Offset) + 
					"{0}" +
						data.Document.GetTextBetween (data.SelectionRange.EndOffset, end);

				args.Add (new PrimitiveExpression (formattedString));
				args.Add (new PrimitiveExpression (data.Document.GetTextAt (data.SelectionRange)));
				
				TypeReference typeRef = new TypeReference ("System.String");
				typeRef.IsKeyword = true;
				MemberReferenceExpression stringFormat = new MemberReferenceExpression (new TypeReferenceExpression (typeRef), "Format");
				formatCall = new InvocationExpression (stringFormat, args);
			}
			
			List<Change> changes = new List<Change> ();
			TextReplaceChange change = new TextReplaceChange ();
			change.FileName = options.Document.FileName;
			change.Offset = start;
			change.RemovedChars = end - start + closingTagLength;
			change.InsertedText = provider.OutputNode (options.Dom, formatCall);
			change.MoveCaretToReplace = true;
			changes.Add (change);
			return changes;
		}
	}
}
