//
// TagNode.cs: represents a tag (HTML or ASP) in an ASP.NET document tree
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

using MonoDevelop.AspNet.Parser.Internal;

namespace MonoDevelop.AspNet.Parser.Dom
{
	public class TagNode : ParentNode
	{
		string tagName;
		TagAttributes attributes; 
		
		internal TagNode (ILocation location, string name, TagAttributes attributes)
			: base (location)
		{
			this.tagName = name;
			this.attributes = attributes;
		}
		
		public string TagName {
			get {return tagName; }
		}
		
		public TagAttributes Attributes {
			get { return attributes; }
		}
		
		public override void AcceptVisit (Visitor visitor)
		{
			visitor.Visit (this);
			foreach (Node n in children) {
				if (visitor.QuickExit)
					break;
				n.AcceptVisit (visitor);
			}
			visitor.Leave (this);
		}
		
		public override string ToString ()
		{
			return string.Format ("[TagNode Name='{0}' Attributes='{1}' Location='{2}' EndTagLocation='{3}']",
			    TagName, Attributes, Location, EndLocation);
		}

	}
}
