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
		PageInfo info;
		
		public PageInfoVisitor (PageInfo info)
		{
			this.info = info;
		}
		
		public override void Visit (DirectiveNode node)
		{
			if (info.Subtype == WebSubtype.None) {
				info.SetSubtypeFromDirective (node.Name);
			}
			else if (String.Compare (node.Name, "mastertype", StringComparison.OrdinalIgnoreCase) == 0)
			{
				info.MasterPageTypeName = node.Attributes["typename"] as string;
				info.MasterPageTypeVPath = node.Attributes["virtualpath"] as string;
				return;
			}
			else
				return;
			
			//after SetSubtypeFromDirective
			if (info.Subtype != WebSubtype.WebForm && info.Subtype != WebSubtype.MasterPage
			    && info.Subtype != WebSubtype.WebControl)
				return;
			
			info.InheritedClass = node.Attributes ["inherits"] as string;
			if (info.InheritedClass == null)
				info.InheritedClass = node.Attributes ["class"] as string;
			
			info.CodeBehindFile = node.Attributes ["codebehind"] as string;
			info.Language = node.Attributes ["language"] as string;
			info.CodeFile = node.Attributes ["codefile"] as string;
			info.MasterPageFile = node.Attributes ["masterpagefile"] as string;
		}
		
		public override void Visit (TextNode node)
		{
			int start = node.Text.IndexOf ("<!DOCTYPE");
			if (start < 0)
				return;
			int end = node.Text.IndexOf (">", start);
			info.DocType = node.Text.Substring (start, end - start + 1);
			QuickExit = true;
		}

		
		public override void Visit (TagNode node)
		{
			//as soon as tags are declared, doctypes and the page directive must been set
			QuickExit = true;
		}

		
		public PageInfo Info {
			get { return info; }
		}
		
		public override string ToString ()
		{
			return info.ToString ();
		}
	}
}
