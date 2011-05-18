// 
// CSharpNRefactoryASTProvider.cs
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
using MonoDevelop.Refactoring;
using MonoDevelop.Projects.Dom.Parser;
using ICSharpCode.NRefactory.CSharp;
using System.IO;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Text;
using System.Reflection;
using System.Collections.Generic;
using MonoDevelop.Ide;
using System.Linq;
using Mono.TextEditor;

namespace MonoDevelop.CSharp.Refactoring
{
	public class CSharpNRefactoryASTProvider : INRefactoryASTProvider
	{
		public bool CanGenerateASTFrom (string mimeType)
		{
			return mimeType == CSharpFormatter.MimeType;
		}

		public string OutputNode (ProjectDom dom, AstNode node)
		{
			return OutputNode (dom, node, "");
		}
		
		public string OutputNode (ProjectDom dom, AstNode node, string indent)
		{
			StringWriter w = new StringWriter();
			var policyParent = dom != null && dom.Project != null ? dom.Project.Policies : null;
			IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			CSharpFormattingPolicy codePolicy = policyParent != null ? policyParent.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
			var formatter = new TextWriterOutputFormatter(w);
			int col = GetColumn (indent, 0, 4);
			formatter.Indentation = System.Math.Max (0, col / 4);
			OutputVisitor visitor = new OutputVisitor (formatter, codePolicy.CreateOptions ());
			node.AcceptVisitor (visitor, null);
			return w.ToString();
		}
		
		public static int GetColumn (string wrapper, int i, int tabSize)
		{
			int j = i;
			int col = 0;
			for (; j < wrapper.Length && (wrapper[j] == ' ' || wrapper[j] == '\t'); j++) {
				if (wrapper[j] == ' ') {
					col++;
				} else {
					col = GetNextTabstop (col, tabSize);
				}
			}
			return col;
		}
		
		static int GetNextTabstop (int currentColumn, int tabSize)
		{
			int result = currentColumn + tabSize;
			return (result / tabSize) * tabSize;
		}
		
	/*	public ICSharpCode.OldNRefactory.Parser.Errors LastErrors {
			get;
			private set;
		}*/
		
		public Expression ParseExpression (string expressionText)
		{
			expressionText = expressionText.Trim ();
			var parser = new CSharpParser ();
			Expression result = null;
			try {
				using (var reader = new StringReader (expressionText)) {
					result = parser.ParseExpression (reader) as Expression;
					result.Remove ();
//					LastErrors = parser.Errors;
				}
			} catch (Exception) {
			}
			return result;
		}
		
		public AstNode ParseText (string text)
		{
			using (TextEditorData data = new TextEditorData()) {
				data.Text = text;
				var parser = new CSharpParser ();
				return parser.ParseSnippet (data);
			}
		}
		
		public CompilationUnit ParseFile (string content)
		{
			try {
				using (var reader = new StringReader (content)) {
					var parser = new CSharpParser ();
					return parser.Parse (reader);
				}
			} catch {
				return null;
			}
		}

		public AstType ParseTypeReference (string content)
		{
			try {
				using (var reader = new StringReader (content.Trim ())) {
					var parser = new CSharpParser ();
					return parser.ParseTypeReference (reader);
				}
			} catch {
				return null;
			}
		}
	}
}
