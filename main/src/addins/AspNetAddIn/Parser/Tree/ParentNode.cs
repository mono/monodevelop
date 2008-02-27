//
// ParentNode.cs: base class for node with children in ASP.NET document tree
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
using System.Collections.Generic;
using AspNetAddIn.Parser.Internal;

namespace AspNetAddIn.Parser.Tree
{
	public abstract class ParentNode : Node
	{
		protected List<Node> children = new List<Node> ();
		
		public ParentNode (ILocation location)
			: base (location)
		{
		}
		
		public void AddChild (Node child)
		{
			children.Add (child);
			child.parent = this;
		}
		
		public override void AddText (ILocation location, string text)
		{
			this.AddChild (new TextNode (location, text));
		}
		
		public Node GetNodeAtPosition (int line, int col)
		{
			//check if position is before or within this node's definition
			int positionMatch = ContainsPosition (line, col);
			if (positionMatch < 0)
				return null;
			else if (positionMatch == 0)
				return this;
			
			//binary search through children
			int start = 0, end = children.Count - 1;
			do {
				int middle = (int) Math.Ceiling ((double) (start + end) / 2);
				int middleMatches = children[middle].ContainsPosition (line, col);
				if (middleMatches == 0)
					return children[middle];
				else if (middleMatches < 0)
					start = middle;
				else
					end = middle;
			} while (start < end);
			
			//position is after the node and its children
			return null;
		}
		
		public override string ToString ()
		{
			return string.Format ("[ParentNode Location='{0}']", Location);
		}
	}
}
