// 
// AspDocumentBuilder.cs
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
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.AspNet.Gui;
using System.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.CSharp.Completion
{
	public class AspLanguageBuilder : Visitor, ILanguageCompletionBuilder
	{
		StringBuilder document = new StringBuilder ();
		StringBuilder pageRenderer = new StringBuilder ();
		bool hasNamespace = false;
		
		public bool SupportsLanguage (string language)
		{
			return language == "C#";
		}
		
		ParsedDocument Parse (string fileName, string text)
		{
			return new MonoDevelop.CSharp.Parser.NRefactoryParser ().Parse (null, fileName, text);
		}
		
		public LocalDocumentInfo BuildLocalDocument (DocumentInfo documentInfo, ILocation location, string expressionText, bool isExpression)
		{
			StringBuilder result = new StringBuilder ();
			string typeName = "Generated";
			if (documentInfo.AspNetParsedDocument.PageInfo != null && !string.IsNullOrEmpty (documentInfo.AspNetParsedDocument.PageInfo.InheritedClass))
				typeName = documentInfo.AspNetParsedDocument.PageInfo.InheritedClass;
			
			int idx = typeName.LastIndexOf ('.');
			if (idx > 0) {
				result.Append ("namespace ");
				result.Append (typeName.Substring (0, idx));
				result.AppendLine (" {");
				typeName = typeName.Substring (idx + 1);
				hasNamespace = true;
			}
			
			result.Append ("partial class ");
			result.AppendLine (typeName);
			result.AppendLine ("{");
		
			if (isExpression) {
				result.AppendLine ("void Generated ()");
				result.AppendLine ("{");
				//Console.WriteLine ("start:" + location.BeginLine  +"/" +location.BeginColumn);
				foreach (ExpressionNode expression in documentInfo.Expressions) {
					if (expression.Location.BeginLine > location.BeginLine || expression.Location.BeginLine == location.BeginLine && expression.Location.BeginColumn > location.BeginColumn - 5) 
						continue;
					//Console.WriteLine ("take xprt:" + expressions.Key.BeginLine  +"/" +expressions.Key.BeginColumn);
					if (expression.IsExpression)
						result.Append ("WriteLine (");
					result.Append (expression.Expression.Trim ('='));
					if (expression.IsExpression)
						result.Append (");");
				}
			}
			result.Append (expressionText);
			int caretPosition = result.Length;
			result.AppendLine ();
			result.AppendLine ("}");
			result.AppendLine ("}");
			Console.WriteLine (result);
			return new LocalDocumentInfo () {
				LocalDocument = result.ToString (),
				CaretPosition = caretPosition,
				ParsedLocalDocument = Parse (documentInfo.AspNetParsedDocument.FileName, result.ToString ())
			};
		}
		
		
		public ICompletionDataList HandleCompletion (Document document, ProjectDom currentDom, char currentChar, ref int triggerWordLength)
		{
			CodeCompletionContext codeCompletionContext;
			using (CSharpTextEditorCompletion completion = CreateCompletion (document, currentDom, out codeCompletionContext)) {
				return completion.HandleCodeCompletion (codeCompletionContext, currentChar, ref triggerWordLength);
			}
		}
		
		public IParameterDataProvider HandleParameterCompletion (MonoDevelop.Ide.Gui.Document document, ProjectDom currentDom, char completionChar)
		{
			CodeCompletionContext codeCompletionContext;
			using (CSharpTextEditorCompletion completion = CreateCompletion (document, currentDom, out codeCompletionContext)) {
				return completion.HandleParameterCompletion (codeCompletionContext, completionChar);
			}
		}

		CSharpTextEditorCompletion CreateCompletion (MonoDevelop.Ide.Gui.Document document, ProjectDom currentDom, out CodeCompletionContext codeCompletionContext)
		{
			codeCompletionContext = new CodeCompletionContext ();
			codeCompletionContext.TriggerOffset = document.TextEditorData.Caret.Offset;
			
			codeCompletionContext.TriggerLine       = document.TextEditorData.Caret.Line;
			codeCompletionContext.TriggerLineOffset = document.TextEditorData.Caret.Column;
			
			CSharpTextEditorCompletion completion = new CSharpTextEditorCompletion (document);
			
			completion.Dom = currentDom;
			return completion;
		}
		DocumentInfo documentInfo;
		public DocumentInfo BuildDocument (MonoDevelop.AspNet.Parser.AspNetParsedDocument aspDocument)
		{
			documentInfo = new DocumentInfo ();
			string typeName = "Generated";
			if (aspDocument.PageInfo != null && !string.IsNullOrEmpty (aspDocument.PageInfo.InheritedClass))
				typeName = aspDocument.PageInfo.InheritedClass;
			
			int idx = typeName.LastIndexOf ('.');
			if (idx > 0) {
				document.Append ("namespace ");
				document.Append (typeName.Substring (0, idx));
				document.Append (" {");
				typeName = typeName.Substring (idx + 1);
				hasNamespace = true;
			}
			
			document.Append ("partial class ");
			document.Append (typeName);
			document.Append ("{");
			
			aspDocument.Document.RootNode.AcceptVisit (this);
			Finish ();
			documentInfo.AspNetParsedDocument = aspDocument;
			documentInfo.ParsedDocument = Parse (aspDocument.FileName, document.ToString ());
			return documentInfo;
		}
		
		void Finish ()
		{
			document.Append ("void Render () {");
			document.Append (pageRenderer.ToString ());
			document.Append ("}");
			
			document.Append ("}");
			if (hasNamespace)
				document.Append ("}");
			/*
			CursorPosition = buildCursorPosition;
			if (buildCursorIsExpression)
			    CursorPosition += expressionStart;
			
			int curLine = 1;
			int curColumn = 1;
			
			string text = document.ToString ();
			
			for (int i = 0; i < text.Length; i++) {
				switch (text[i]) {
				case '\n':
					if (i + 1 < text.Length && text[i + 1] == '\r')
						i++;
					goto case '\r';
				case '\r':
					curLine++;
					curColumn = 1;
					break;
				default:
					curColumn++;
					break;
				}
				if (i == CursorPosition) {
					CursorLine = curLine;
					CursorColumn = curColumn;
					break;
				}
			}*/
		}
		
		public override void Visit (ExpressionNode node)
		{
			string text = node.Expression;
			documentInfo.Expressions.Add (node);
			pageRenderer.AppendLine (text);
		}
	}
}
