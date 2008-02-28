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

using MonoDevelop.AspNet.Parser.Internal;

namespace MonoDevelop.AspNet.Parser.Dom
{
	public abstract class ParentNode : Node
	{
		protected List<Node> children = new List<Node> ();
		ILocation endLocation;
		
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
		
		//if the tag has an end tag, this is the location of the end tag
		public ILocation EndLocation {
			internal set { endLocation = new Location (value); }
			get { return endLocation; }
		}
		
		public override Node GetNodeAtPosition (int line, int col)
		{
			//check if position is before or within this node's actual tags
			int positionMatch = base.ContainsPosition (line, col);
			if (positionMatch < 0)
				return null;
			else if (positionMatch == 0)
				return this;
			else if (EndLocation != null) {
				positionMatch = LocationContainsPosition (EndLocation, line, col);
				if (positionMatch == 0)
					return this;
				else if (positionMatch > 0)
					return null;
			}
			
			//binary search through children
			int start = 0, end = children.Count - 1;
			while (start < end) {
				//(start + end) cannot be more than int.MaxValue so no need to worry about overflow
				int middle = (int) Math.Ceiling ((double) (start + end) / 2);
				int middleMatches = children[middle].ContainsPosition (line, col);
				
				//simple binary search
				if (middleMatches == 0)
					return children[middle].GetNodeAtPosition (line, col);
				else if (middleMatches > 0)
					start = middle;
				else
					end = middle;
				
				//special-cas ethe end so that gaps in the ILocation continuum can't cause an infinite loop
				if (end - start == 1) {
					Node n = (middle == start)? children[end] : children[start];
					if (n.ContainsPosition (line, col) == 0)
						return n.GetNodeAtPosition (line, col);	
					//oops, hope the user reports this
					MonoDevelop.Core.LoggingService.LogWarning ("Gap in ASP.NET parsed page between {0} and {1}", children[start], children[end]);
					return null;
				}
			}
			//position not in the node and its children
			return null;
		}
		
		public override int ContainsPosition (int line, int col)
		{
			if (EndLocation != null) {
				if (LocationContainsPosition (EndLocation, line, col) > 0)
					return 1;
				else if (base.ContainsPosition (line, col) < 0)
					return -1;
				else
					return 0;
			}
			return base.ContainsPosition (line, col);
		}
		
		public override string ToString ()
		{
			return string.Format ("[ParentNode Location='{0}']", Location);
		}
	}
}
