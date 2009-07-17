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
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Projects.Dom.Parser;
using ICSharpCode.NRefactory.PrettyPrinter;
using ICSharpCode.NRefactory;
using System.IO;

namespace CSharpBinding.Parser
{
	public class CSharpNRefactoryASTProvider : INRefactoryASTProvider
	{
		public bool CanGenerateASTFrom (string mimeType)
		{
			return mimeType == CSharpFormatter.MimeType;
		}

		public string OutputNode (ProjectDom dom, INode node)
		{
			return OutputNode (dom, node, "");
		}
		
		public string OutputNode (ProjectDom dom, INode node, string indent)
		{
			CSharpOutputVisitor outputVisitor = new CSharpOutputVisitor ();
			CSharpFormatter.SetFormatOptions (outputVisitor, dom.Project);
			int col = CSharpFormatter.GetColumn (indent, 0, 4);
			outputVisitor.OutputFormatter.IndentationLevel = System.Math.Max (0, col / 4);
			node.AcceptVisitor (outputVisitor, null);
			return outputVisitor.Text;
		}
		public Expression ParseExpression (string expressionText)
		{
			expressionText = expressionText.Trim ();
			using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (expressionText))) {
				return parser.ParseExpression ();
			}
		}
		
		public INode ParseText (string text)
		{
			text = text.Trim ();
			using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (text))) {
				if (text.EndsWith (";") || text.EndsWith ("}")) {
					BlockStatement block = parser.ParseBlock ();
					if (block != null)
						return block;
				}
				return parser.ParseExpression ();
			}
		}
		
		public CompilationUnit ParseFile (string content)
		{
			content = content.Trim ();
			using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (content))) {
				parser.Parse ();
				return parser.CompilationUnit;
			}
		}
		
	}
}
