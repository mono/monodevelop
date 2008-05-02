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
	public abstract class ParentNode : Node, IEnumerable<Node>
	{
		protected List<Node> children = new List<Node> ();
		ILocation endLocation;
		bool isClosed = false;
		
		public ParentNode (ILocation location)
			: base (location)
		{
		}
		
		public void AddChild (Node child)
		{
			System.Diagnostics.Debug.Assert (isClosed != true);
			children.Add (child);
			child.parent = this;
		}
		
		public override void AddText (ILocation location, string text)
		{
			System.Diagnostics.Debug.Assert (isClosed != true);
			this.AddChild (new TextNode (location, text));
		}
		
		//if the tag has an end tag, this is the location of the end tag
		public ILocation EndLocation {
			internal set {
				System.Diagnostics.Debug.Assert (value != null);
				endLocation = new Location (value);
				isClosed = true;
			}
			get { return endLocation; }
		}
		
		public bool IsClosed {
			get { return isClosed; }
		}
		
		public void Close ()
		{
			System.Diagnostics.Debug.Assert (isClosed != true);
			isClosed = true;
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
				positionMatch = ILocationContainsPosition (EndLocation, line, col);
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
				
				//special-case the end so that gaps in the ILocation continuum can't cause an infinite loop
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
			int baseContains = base.ContainsPosition (line, col);
			
			//before this node?
			if (baseContains < 0)
				return -1;
			
			//after this node's end, or after its last child?
			if (EndLocation != null) {
				if (ILocationContainsPosition (EndLocation, line, col) > 0)
					return 1;
			} else if (children.Count < 1) {
				return baseContains;
			} else if (children[children.Count - 1].ContainsPosition (line, col) > 0) {
				return 1;
			} 
			
			//not outside this node, must be within it
			return 0;
		}
		
		public override string ToString ()
		{
			return string.Format ("[ParentNode Location='{0}']", Location);
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return children.GetEnumerator ();
		}

		IEnumerator<Node> IEnumerable<Node>.GetEnumerator ()
		{
			return children.GetEnumerator ();
		}
	}
}
