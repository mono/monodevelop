//
// WebFormsDirective.cs
//
// Author:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
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

using System.Diagnostics;
using System.Text;
using ICSharpCode.NRefactory;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.AspNet.WebForms.Dom
{
	public class WebFormsDirective : XNode, IAttributedXObject
	{
		XName name;
		XAttributeCollection attributes;

		public WebFormsDirective (TextLocation start) : base (start)
		{
			attributes = new XAttributeCollection (this);
		}

		public WebFormsDirective (TextLocation start, XName name) : this (start)
		{
			this.name = name;
		}

		protected WebFormsDirective ()
		{
			attributes = new XAttributeCollection (this);
		}

		public XAttributeCollection Attributes {
			get { return attributes; }
		}

		public XName Name {
			get { return name; }
			set {
				Debug.Assert (!IsNamed, "Should not name node more than once.");
				name = value;
			}
		}

		public override bool IsComplete { get { return base.IsComplete && IsNamed; } }

		public bool IsNamed { get { return name.IsValid; } }

		protected override XObject NewInstance ()
		{
			return new WebFormsDirective ();
		}

		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			WebFormsDirective copyFromEl = (WebFormsDirective)copyFrom;
			name = copyFromEl.name; //XName is immutable value type
		}

		public override string ToString ()
		{
			return string.Format ("[WebFormsDirective Name='{0}' Location='{1}'", name.FullName, Region);
		}

		public override void BuildTreeString (StringBuilder builder, int indentLevel)
		{
			builder.Append (' ', indentLevel * 2);
			builder.AppendFormat ("[WebFormsDirective Name='{0}' Location='{1}' Children=", name.FullName, Region);
			builder.AppendLine ();
			
			builder.Append (' ', indentLevel * 2);
			builder.Append ("Attributes=");
			builder.AppendLine ();
			
			foreach (XAttribute att in Attributes)
				att.BuildTreeString (builder, indentLevel + 1);
			
			builder.Append (' ', indentLevel * 2);
			builder.AppendLine ("]");
		}

		public override string FriendlyPathRepresentation {
			get { return "<%@ " + name.FullName + " %>"; }
		}
	}
}
