// 
// T4Parser.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using Mono.TextTemplating;

namespace MonoDevelop.TextTemplating.Parser
{
	
	
	public class T4Parser : AbstractParser
	{
		
		public T4Parser () : base (null, "application/t4-template")
		{
		}
		
		public override bool CanParse (string fileName)
		{
			return fileName.EndsWith (".tt");
		}
		
		public override ParsedDocument Parse (string fileName, string fileContent)
		{
			ParsedTemplate template = new ParsedTemplate (fileName);
			try {
				Tokeniser tk = new Tokeniser (fileName, fileContent);
				template.ParseWithoutIncludes (tk);
			} catch (ParserException ex) {
				template.LogError (ex.Message, ex.Location);
			}
			
			T4ParsedDocument doc = new T4ParsedDocument (fileName, template.RawSegments);
			doc.Flags |= ParsedDocumentFlags.NonSerializable;
			foreach (System.CodeDom.Compiler.CompilerError err in template.Errors)
				doc.Errors.Add (new Error (err.IsWarning? ErrorType.Warning : ErrorType.Error,
				                           err.Line, err.Column, err.ErrorText)); 
			
			return doc;
		}
	}
}
