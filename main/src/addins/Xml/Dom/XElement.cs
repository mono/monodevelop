//
// XElement.cs
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
	public class XElement : XContainer, IAttributedXObject
	{
		XNode closingTag;
		readonly XAttributeCollection attributes;

		public XElement (TextLocation start) : base (start)
		{
			attributes = new XAttributeCollection (this);
		}

		public XElement (TextLocation start, XName name) : this (start)
		{
			this.Name = name;
		}

		public XNode ClosingTag { get { return closingTag; } }
		public bool IsClosed { get { return closingTag != null; } }
		public bool IsSelfClosing { get { return closingTag == this; } }

		public void Close (XNode closingTag)
		{
			this.closingTag = closingTag;
			if (closingTag is XClosingTag)
				closingTag.Parent = this;
		}

		public XName Name { get; set; }

		public override bool IsComplete { get { return base.IsComplete && IsNamed; } }
		public bool IsNamed { get { return Name.IsValid; } }

		public XAttributeCollection Attributes {
			get { return attributes; }
		}

		protected XElement ()
		{
			attributes = new XAttributeCollection (this);
		}

		protected override XObject NewInstance () { return new XElement (); }

		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			var copyFromEl = (XElement) copyFrom;
			Name = copyFromEl.Name; //XName is immutable value type
			//include attributes
			foreach (var a in copyFromEl.Attributes)
				Attributes.AddAttribute ((XAttribute) a.ShallowCopy ());
		}

		public override string ToString ()
		{
			return string.Format ("[XElement Name='{0}' Location='{1}'", Name.FullName, Region);
		}

		public override void BuildTreeString (StringBuilder builder, int indentLevel)
		{
			builder.Append (' ', indentLevel * 2);
			builder.AppendFormat ("[XElement Name='{0}' Location='{1}' Children=", Name.FullName, Region);
			builder.AppendLine ();

			foreach (XNode child in Nodes)
				child.BuildTreeString (builder, indentLevel + 1);

			builder.Append (' ', indentLevel * 2);
			builder.Append ("Attributes=");
			builder.AppendLine ();

			foreach (XAttribute att in Attributes)
				att.BuildTreeString (builder, indentLevel + 1);

			if (closingTag is XClosingTag) {
				builder.AppendLine ("ClosingTag=");
				closingTag.BuildTreeString (builder, indentLevel + 1);
			} else if (closingTag == null)
				builder.AppendLine ("ClosingTag=(null)");
			else
				builder.AppendLine ("ClosingTag=(Self)");

			builder.Append (' ', indentLevel * 2);
			builder.AppendLine ("]");
		}

		public override string FriendlyPathRepresentation {
			get { return Name.FullName; }
		}

		public IEnumerable<XElement> Elements {
			get {
				XElement el;
				foreach (XNode node in Nodes) {
					el = node as XElement;
					if (el != null)
						yield return el;
				}
			}
		}

		public IEnumerable<XElement> AllDescendentElements {
			get {
				foreach (XElement el in Elements) {
					yield return el;
					foreach (XElement el2 in el.AllDescendentElements)
						yield return el2;
				}
			}
		}

	}
}
