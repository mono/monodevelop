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
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.AspNet;

namespace MonoDevelop.AspNet.Parser
{
	public class PageInfoVisitor : Visitor
	{
		string inheritedClass;
		string codeBehindFile;
		string codeFile;
		string language;
		string docType;
		WebSubtype type = WebSubtype.None;
		
		public override void Visit (DirectiveNode node)
		{
			switch (node.Name.ToLower ()) {
				case "page":
					type = WebSubtype.WebForm;
					break;
				case "control":
					type = WebSubtype.WebControl;
					break;
				case "webservice":
					type = WebSubtype.WebService;
					break;
				case "webhandler":
					type = WebSubtype.WebHandler;
					break;
				case "application":
					type = WebSubtype.Global;
					break;
				case "master":
					type = WebSubtype.MasterPage;
					break;
				default:
					type = WebSubtype.None;
					return;
			}
			
			//we have the info, stop walking
			if (type != WebSubtype.WebForm && type != WebSubtype.MasterPage)
				QuickExit = true;
			
			inheritedClass = node.Attributes ["inherits"] as string;
			if (inheritedClass == null)
				inheritedClass = node.Attributes ["class"] as string;
			
			codeBehindFile = node.Attributes ["codebehind"] as string;
			language = node.Attributes ["language"] as string;
			codeFile = node.Attributes ["codefile"] as string;
		}
		
		public override void Visit (TextNode node)
		{
			int start = node.Text.IndexOf ("<!DOCTYPE");
			if (start < 0)
				return;
			int end = node.Text.IndexOf (">", start);
			docType = node.Text.Substring (start, end - start + 1);
			QuickExit = true;
		}

		
		public override void Visit (TagNode node)
		{
			//as soon as tags are declared, doctypes and 
			QuickExit = true;
		}

		
		public string InheritedClass {
			get { return inheritedClass; }
		}
		
		public string CodeBehindFile {
			get { return codeBehindFile; }
		}
		
		public string CodeFile {
			get { return codeFile; }
		}
		
		public string Language {
			get { return language; }
		}
		
		public string DocType {
			get { return docType; }
		}
		
		public WebSubtype Subtype {
			get { return type; }
		}
		
		public override string ToString ()
		{
			return string.Format ("[PageInfoVisitor WebSubtype='{0}' InheritedClass='{1}' CodeBehindFile='{2}' CodeFile='{3}' Language='{4}' DocType='{5}']",
			    Subtype, InheritedClass, CodeBehindFile, CodeFile, Language, DocType
			    );
		}

	}
}
