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

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;


namespace MonoDevelop.AspNet.Parser
{
	
	
	public class AspNetParser : AbstractParser
	{
		public AspNetParser () : base (null, "application/x-aspx", "application/x-ascx", "application/x-master-page")
		{
		}
		
		public override bool CanParse (string fileName)
		{
			return fileName.EndsWith (".aspx") || fileName.EndsWith (".ascx") || fileName.EndsWith (".master");
		}
		
		public override ParsedDocument Parse (string fileName, string fileContent)
		{
			using (TextReader tr = new StringReader (fileContent)) {
				Document doc = new Document (tr, null, fileName);
				AspNetParsedDocument result = new AspNetParsedDocument (fileName) {
					PageInfo = new PageInfo (),
					Document = doc
				};
				
				doc.RootNode.AcceptVisit (new PageInfoVisitor (result.PageInfo));
				
				foreach (ParserException pe in doc.ParseErrors)
					result.Add (new Error (
						pe.IsWarning? ErrorType.Warning : ErrorType.Error,
						pe.Line, pe.Column, pe.Message));
				
				return result;
			}
		}
	}
}
