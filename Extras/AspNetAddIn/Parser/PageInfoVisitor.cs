//
// PageInfoVisitor.cs: Collects information about the document from ASP.NET 
//     document tree, for code completion and other services.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using AspNetAddIn.Parser.Tree;

namespace AspNetAddIn.Parser
{
	public class PageInfoVisitor : Visitor
	{
		private string inheritedClass;
		private string codeBehindFile;
		private string language;
		
		public override void Visit (DirectiveNode node)
		{
			if (String.Compare (node.Name, "page", true) != 0)
				return;
			
			inheritedClass = (string) node.Attributes ["inherits"];
			codeBehindFile = (string) node.Attributes ["codebehind"];
			language = (string) node.Attributes ["language"];
		}
		
		public string InheritedClass {
			get { return inheritedClass; }
		}
		
		public string CodeBehindFile {
			get { return codeBehindFile; }
		}
		
		public string Language {
			get { return language; }
		}
	}
}
