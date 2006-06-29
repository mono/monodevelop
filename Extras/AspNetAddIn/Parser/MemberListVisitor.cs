//
// MemberListVisitor.cs: Collects members from ASP.NET document tree, 
//     for code completion and other services.
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
using System.Collections;
using AspNetAddIn.Parser.Tree;

namespace AspNetAddIn.Parser
{
	//purpose is to find all named tags for code completion and compilation of base class
	public class MemberListVisitor : Visitor
	{
		private Hashtable list = new Hashtable ();
		Document doc;
		
		public MemberListVisitor (Document parent)
		{
			this.doc = parent;	
		}
		
		public override void Visit (TagNode node)
		{
			if (!node.Attributes.IsRunAtServer ())
				return;
			
			string id = node.Attributes ["id"] as string;
			
			if (id == null)
				return;
			
			if (list.ContainsKey (id))
				throw new Exception ("Tag id must be unique within the document");
			
			string [] s = node.TagName.Split (':');
			string prefix = (s.Length == 1)? "" : s[0];
			string name = (s.Length == 1)? s[0] : s[1];
			if (s.Length > 2)
				throw new Exception ("Malformed tag name");
				
			Type type = doc.WebFormReferenceManager.GetType (prefix, name);
			System.CodeDom.CodeTypeReference ctRef = new System.CodeDom.CodeTypeReference (type.ToString ());
			System.CodeDom.CodeMemberField member = new System.CodeDom.CodeMemberField (ctRef, id);
			member.Attributes = System.CodeDom.MemberAttributes.Family;
			
			list [id] = member;
		}
		
		public IDictionary List {
			get { return list; }
		}
	}
}
