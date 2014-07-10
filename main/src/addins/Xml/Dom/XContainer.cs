//
// XContainer.cs
//
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Text;
using ICSharpCode.NRefactory;

namespace MonoDevelop.Xml.Dom
{
	public abstract class XContainer : XNode
	{
		protected XContainer (TextLocation start) : base (start) {	}

		XNode firstNode;
		XNode lastChild;
		public XNode FirstChild { get { return firstNode; } }
		public XNode LastChild { get { return lastChild; } }

		public IEnumerable<XNode> Nodes {
			get {
				XNode next = firstNode;
				while (next != null) {
					yield return next;
					next = next.NextSibling;
				}
			}
		}

		public IEnumerable<XNode> AllDescendentNodes {
			get {
				foreach (XNode n in Nodes) {
					yield return n;
					XContainer c = n as XContainer;
					if (c != null)
						foreach (XNode n2 in c.AllDescendentNodes)
							yield return n2;
				}
			}
		}

		public virtual void AddChildNode (XNode newChild)
		{
			newChild.Parent = this;
			if (lastChild != null)
				lastChild.NextSibling = newChild;
			if (firstNode == null)
				firstNode = newChild;
			lastChild = newChild;
		}

		protected XContainer () {}

		public override void BuildTreeString (StringBuilder builder, int indentLevel)
		{
			base.BuildTreeString (builder, indentLevel);
			foreach (XNode child in Nodes)
				child.BuildTreeString (builder, indentLevel + 1);
		}
	}
}
