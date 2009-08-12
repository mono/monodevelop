//
// DirectiveNode.cs: Represents a directive in an ASP.NET document tree
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

namespace MonoDevelop.AspNet.Parser.Dom
{
	public class DirectiveNode : Node
	{
		string name;
		TagAttributes attributes;
		
		public DirectiveNode (ILocation location, string name, TagAttributes attributes)
			: base (location)
		{
			this.name = name;
			this.attributes = attributes;
			
			//FIXME: workaround for weird parser MasterPage bug
			if (string.IsNullOrEmpty(name))
				foreach (string key in attributes.Keys)
					if (key.ToLower() == "master")
						this.name = "Master";
		}
		
		public override void AcceptVisit (Visitor visitor)
		{
			visitor.Visit (this);
		}
		
		public string Name {
			get {return name; }
		}
		
		public TagAttributes Attributes {
			get { return attributes; }
		}
		
		public override string ToString ()
		{
			return string.Format ("[DirectiveNode Name='{0}' Attributes='{1}' Location='{2}']", Name, Attributes, Location);
		}

	}
}
