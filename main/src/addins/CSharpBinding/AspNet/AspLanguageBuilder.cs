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
using Mono.TextEditor;
using ICSharpCode.NRefactory;

namespace MonoDevelop.CSharp.Completion
{
	public class AspLanguageBuilder : Visitor, ILanguageCompletionBuilder
	{
		public bool SupportsLanguage (string language)
		{
			return language == "C#";
		}
		
		ParsedDocument Parse (string fileName, string text)
		{
			return new MonoDevelop.CSharp.Parser.NRefactoryParser ().Parse (null, fileName, text);
		}
		
		static void WriteUsings (IEnumerable<string> usings, StringBuilder builder)
		{
			foreach (var u in usings) {
				builder.Append ("using ");
				builder.Append (u);
				builder.AppendLine (";");
			}
		}
		
		static void WriteClassDeclaration (DocumentInfo info, StringBuilder builder)
		{
			builder.Append ("partial class ");
			builder.Append (info.ClassName);
			builder.Append (" : ");
			builder.AppendLine (info.BaseType);
		}
		
		public LocalDocumentInfo BuildLocalDocument (DocumentInfo info, TextEditorData data,
		                                             string expressionText, bool isExpression)
		{
			var result = new StringBuilder ();
			
			WriteUsings (info.Imports, result);
			WriteClassDeclaration (info, result);
			result.AppendLine ("{");
		
			if (isExpression) {
				result.AppendLine ("void Generated ()");
				result.AppendLine ("{");
				//Console.WriteLine ("start:" + location.BeginLine  +"/" +location.BeginColumn);
				foreach (var expression in info.Expressions) {
					if (expression.Location.BeginLine > data.Caret.Line || expression.Location.BeginLine == data.Caret.Line && expression.Location.BeginColumn > data.Caret.Column - 5) 
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
			
			return new LocalDocumentInfo () {
				LocalDocument = result.ToString (),
				CaretPosition = caretPosition,
				ParsedLocalDocument = Parse (info.AspNetDocument.FileName, result.ToString ())
			};
		}
		
		
		public ICompletionDataList HandleCompletion (MonoDevelop.Ide.Gui.Document document, DocumentInfo info,
			LocalDocumentInfo localInfo, ProjectDom dom, char currentChar, ref int triggerWordLength)
		{
			CodeCompletionContext codeCompletionContext;
			using (var completion = CreateCompletion (document, info, localInfo, dom, out codeCompletionContext)) {
				return completion.HandleCodeCompletion (codeCompletionContext, currentChar, ref triggerWordLength);
			}
		}
		
		public IParameterDataProvider HandleParameterCompletion (MonoDevelop.Ide.Gui.Document document, 
			DocumentInfo info, LocalDocumentInfo localInfo, ProjectDom dom, char completionChar)
		{
			CodeCompletionContext codeCompletionContext;
			using (var completion = CreateCompletion (document, info, localInfo, dom, out codeCompletionContext)) {
				return completion.HandleParameterCompletion (codeCompletionContext, completionChar);
			}
		}

		CSharpTextEditorCompletion CreateCompletion (MonoDevelop.Ide.Gui.Document document, DocumentInfo info,
			LocalDocumentInfo localInfo, ProjectDom dom, out CodeCompletionContext codeCompletionContext)
		{
			var doc = new Mono.TextEditor.Document () {
				Text = localInfo.LocalDocument,
			};
			var documentLocation = doc.OffsetToLocation (localInfo.CaretPosition);
			
			codeCompletionContext = new CodeCompletionContext () {
				TriggerOffset = localInfo.CaretPosition,
				TriggerLine = documentLocation.Line + 1,
				TriggerLineOffset = documentLocation.Column + 1,
			};
			
			var r = new System.IO.StringReader (localInfo.LocalDocument);
			using (var parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (SupportedLanguage.CSharp, r)) {
				parser.Parse ();
				return new CSharpTextEditorCompletion (document) {
					ParsedUnit = parser.CompilationUnit,
					Dom = dom,
				};
			}
		}
		
		public ParsedDocument BuildDocument (DocumentInfo info, TextEditorData data)
		{
			var document = new StringBuilder ();
			
			WriteUsings (info.Imports, document);
			WriteClassDeclaration (info, document);
			
			foreach (var node in info.ScriptBlocks) {
				int start = data.Document.LocationToOffset (node.Location.EndLine - 1,  node.Location.EndColumn);
				int end = data.Document.LocationToOffset (node.EndLocation.BeginLine - 1, node.EndLocation.BeginColumn);
				document.AppendLine (data.Document.GetTextBetween (start, end));
			}
			
			var docStr = document.ToString ();
			document.Length = 0;
			return Parse (info.AspNetDocument.FileName, docStr);
		}
	}
}
