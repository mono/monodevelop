// 
// AspNetParser.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

using MonoDevelop.Projects.Parser;


namespace MonoDevelop.AspNet.Parser
{
	
	
	public class AspNetParser : IParser
	{
		string[] lexerTags;
		
		public string[] LexerTags {
			get { return lexerTags; }
			set { lexerTags = value; }
		}

		public IExpressionFinder CreateExpressionFinder (string fileName)
		{
			return new AspNetExpressionFinder ();
		}

		public ICompilationUnitBase Parse (string fileName)
		{
			using (TextReader tr = new StreamReader (fileName)) {
				Document doc = new Document (tr, null, fileName);
				return BuildCU (doc);
			}
		}

		public ICompilationUnitBase Parse (string fileName, string fileContent)
		{
			using (TextReader tr = new StringReader (fileContent)) {
				Document doc = new Document (tr, null, fileName);
				return BuildCU (doc);
			}
		}
		
		AspNetCompilationUnit BuildCU (Document doc)
		{
			AspNetCompilationUnit cu = new AspNetCompilationUnit ();
			cu.Document = doc;
			cu.PageInfo = new PageInfo ();
			
			CompilationUnitVisitor cuVisitor = new CompilationUnitVisitor (cu);
			doc.RootNode.AcceptVisit (cuVisitor);
			
			PageInfoVisitor piVisitor = new PageInfoVisitor (cu.PageInfo);
			doc.RootNode.AcceptVisit (piVisitor);
			
			foreach (ParserException pe in doc.ParseErrors)
				cu.AddError (new ErrorInfo (pe.Line, pe.Column, pe.Message));
			cu.CompileErrors ();
			return cu;
		}
		
		ErrorInfo[] GetErrors (Document doc)
		{
			System.Collections.Generic.List<ErrorInfo> list = new System.Collections.Generic.List<ErrorInfo> ();
			foreach (ParserException pe in doc.ParseErrors)
				list.Add (new ErrorInfo (pe.Line, pe.Column, pe.Message));
			return list.Count == 0? null : list.ToArray ();
		}

		public ResolveResult Resolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			return null;
		}

		public LanguageItemCollection CtrlSpace (IParserContext parserContext, int caretLine, int caretColumn, string fileName)
		{
			return null;
		}

		public ILanguageItem ResolveIdentifier (IParserContext parserContext, string id, int line, int col, string fileName, string fileContent)
		{
			return null;
		}
	}
}
