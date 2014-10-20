//
// XObject.cs
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory;

namespace MonoDevelop.Xml.Dom
{
	public abstract class XObject
	{
		DomRegion region;

		protected XObject (TextLocation start)
		{
			region = new DomRegion (start, TextLocation.Empty);
		}

		protected XObject (DomRegion region)
		{
			this.region = region;
		}

		public XObject Parent { get; internal protected set; }

		public IEnumerable<XNode> Parents {
			get {
				XNode next = Parent as XNode;
				while (next != null) {
					yield return next;
					next = next.Parent as XNode;
				}
			}
		}

		public DomRegion Region {
			get { return region; }
		}

		public void End (TextLocation endLocation)
		{
			region = new DomRegion (region.Begin, endLocation);
		}

		public bool IsEnded {
			get { return region.End > region.Begin; }
		}

		public virtual bool IsComplete {
			get { return region.End > region.Begin; }
		}

		public virtual void BuildTreeString (StringBuilder builder, int indentLevel)
		{
			builder.Append (' ', indentLevel * 2);
			builder.AppendFormat (ToString ());
			builder.AppendLine ();
		}

		public override string ToString ()
		{
			return string.Format ("[{0} Location='{1}']", GetType (), Region);
		}

		//creates a parallel tree -- should NOT retain references into old tree
		public XObject ShallowCopy ()
		{
			XObject copy = NewInstance ();
			copy.ShallowCopyFrom (this);
			return copy;
		}

		protected abstract XObject NewInstance ();

		protected virtual void ShallowCopyFrom (XObject copyFrom)
		{
			region = copyFrom.region; //immutable value type
		}

		protected XObject () {}

		public virtual string FriendlyPathRepresentation {
			get { return GetType ().ToString (); }
		}
	}
}
