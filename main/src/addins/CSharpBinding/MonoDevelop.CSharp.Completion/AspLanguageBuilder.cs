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
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.CSharp.Completion
{
	public class AspLanguageBuilder : Visitor, ILanguageCompletionBuilder
	{
		StringBuilder document = new StringBuilder ();
		StringBuilder pageRenderer = new StringBuilder ();
		bool hasNamespace = false;
		
		public string Text {
			get {
				return document.ToString ();
			}
		}
		
		public int CursorPosition {
			get;
			private set;
		}
		
		public int CursorLine {
			get;
			private set;
		}
		
		public int CursorColumn {
			get;
			private set;
		}
		public bool SupportsLanguage (string language)
		{
			return language == "C#";
		}
		int line, column;
		
		public ParsedDocument Parse (string fileName, string text)
		{
			return new MonoDevelop.CSharp.Parser.NRefactoryParser ().Parse (null, fileName, text);
		}
		
		public ICompletionDataList HandleCompletion (Document document, ProjectDom currentDom, char currentChar, ref int triggerWordLength)
		{
			CodeCompletionContext completionContext = new CodeCompletionContext ();
			completionContext.TriggerOffset     = CursorPosition;
			completionContext.TriggerLine       = CursorLine;
			completionContext.TriggerLineOffset = CursorColumn;
			
			using (CSharpTextEditorCompletion completion = new CSharpTextEditorCompletion (document)) {
				completion.Dom = currentDom;
				return completion.HandleCodeCompletion (completionContext, currentChar, ref triggerWordLength);
			}
		}
		public IParameterDataProvider HandleParameterCompletion (MonoDevelop.Ide.Gui.Document document, ProjectDom currentDom, char completionChar)
		{
			CodeCompletionContext completionContext = new CodeCompletionContext ();
			completionContext.TriggerOffset     = CursorPosition;
			completionContext.TriggerLine       = CursorLine;
			completionContext.TriggerLineOffset = CursorColumn;
			
			using (CSharpTextEditorCompletion completion = new CSharpTextEditorCompletion (document)) {
				completion.Dom = currentDom;
				return completion.HandleParameterCompletion (completionContext, completionChar);
			}
		}
		
		public void Build (MonoDevelop.AspNet.Parser.AspNetParsedDocument aspDocument, int line, int column)
		{
			this.line = line;
			this.column = column;
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
		}
		
		void Finish ()
		{
			document.Append ("void Render () {");
			int expressionStart = document.Length;
			document.Append (pageRenderer.ToString ());
			document.Append ("}");
			
			document.Append ("}");
			if (hasNamespace)
				document.Append ("}");
			
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
			}
		}
		
		bool buildCursorIsExpression = false;
		int buildCursorPosition = 0;
		
		public override void Visit (ExpressionNode node)
		{
			string text = node.Expression;
			StringBuilder sb = node.IsExpression ? pageRenderer : document;
			if (node.ContainsPosition (this.line, this.column) == 0) {
				buildCursorPosition = sb.Length + text.Length;
				buildCursorIsExpression = node.IsExpression;
				
				int curLine = node.Location.BeginLine;
				int curColumn = node.Location.BeginColumn + 
					(node.IsExpression ? 3 : 2); // tag len : <% or <%= 
				
				for (int i = 0; i < text.Length; i++) {
					switch (text[i]) {
					case '\n':
						if (i + 1 < text.Length && text[i + 1] == '\r')
							i++;
						goto case '\r';
					case '\r':
						if (this.line == curLine && this.column > curColumn)
							buildCursorPosition = sb.Length + i;
						curLine++;
						curColumn = 1;
						break;
					default:
						curColumn++;
						break;
					}
					if (this.line == curLine && this.column == curColumn)
						buildCursorPosition = sb.Length + i;
				}
			}
			sb.AppendLine (text);
		}
	}
}
