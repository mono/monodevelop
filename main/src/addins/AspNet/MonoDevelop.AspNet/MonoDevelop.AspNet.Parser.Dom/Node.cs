//
// Node.cs: base class for all nodes in ASP.NET document tree
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
using MonoDevelop.AspNet.Parser.Internal;

namespace MonoDevelop.AspNet.Parser.Dom
{
	public abstract class Node
	{
		internal Node parent;
		ILocation location;
		
		public Node (ILocation location)
		{
			this.location = new Location (location);
		}
		
		public Node Parent {
			get { return parent; }
		}
		
		public ILocation Location {
			get { return location; }
		}
		
		public abstract void AcceptVisit (Visitor visitor);
		
		public virtual void AddText (ILocation location, string text)
		{
			throw new ParseException (location, "This parse tree node does not support internal text");
		}
		
		public virtual Node GetNodeAtPosition (int line, int col)
		{
			if (ContainsPosition (line, col) == 0)
				return this;
			return null;
		}
		
		public virtual int ContainsPosition (int line, int col)
		{
			return ILocationContainsPosition (Location, line, col);
		}
		
		public int LocationContainsPosition (int line, int col)
		{
			return ILocationContainsPosition (Location, line, col);
		}
		
		internal protected int ILocationContainsPosition (ILocation loc, int line, int col)
		{
			if (line < loc.BeginLine || (line == loc.BeginLine && col < loc.BeginColumn))
				return -1;
			if (line > loc.EndLine || (line == loc.EndLine && col > loc.EndColumn))
				return 1;
			return 0;
		}
		
		public override string ToString ()
		{
			return string.Format ("[Node Location='{0}']", Location);
		}
	}
}
