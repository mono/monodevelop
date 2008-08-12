// 
// AspNetDom.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet.StateEngine
{
	
	public class AspNetDirective : XNode, IAttributedXObject
	{
		XName name;
		XAttributeCollection attributes;
		
		public AspNetDirective (int start) : base (start)
		{
			attributes = new XAttributeCollection (this);
		}
		
		public AspNetDirective (int start, XName name) : this (start)
		{
			this.name = name;
		}
		
		protected AspNetDirective ()
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
			return new AspNetDirective ();
		}
		
		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			AspNetDirective copyFromEl = (AspNetDirective) copyFrom;
			name = copyFromEl.name; //XName is immutable value type
		}
		
		public override string ToString ()
		{
			return string.Format ("[AspNetDirective Name='{0}' Location='{1}'",  name.FullName, this.Position);
		}
		
		public override void BuildTreeString (StringBuilder builder, int indentLevel)
		{
			builder.Append (' ', indentLevel * 2);
			builder.AppendFormat ("[AspNetDirective Name='{0}' Location='{1}' Children=", name.FullName, this.Position);
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
	
	public abstract class AspNetExpression : XNode
	{
		public AspNetExpression (int start, int end) : base (start, end) {}
		public AspNetExpression (int start) : base (start) {}
		protected AspNetExpression () {}
	}
	
	
	public class AspNetRenderExpression : AspNetExpression
	{
		public AspNetRenderExpression (int start, int end) : base (start, end) {}
		public AspNetRenderExpression (int start) : base (start) {}
		protected AspNetRenderExpression () {}
		
		protected override XObject NewInstance () { return new AspNetRenderExpression (); }
		
		public override string ToString ()
		{
			return string.Format ("[AspNetRenderExpression Location='{0}'", this.Position);
		}
		
		public override string FriendlyPathRepresentation {
			get { return "<%= %>"; }
		}
	}
	
	public class AspNetDataBindingExpression : AspNetExpression
	{
		public AspNetDataBindingExpression (int start, int end) : base (start, end) {}
		public AspNetDataBindingExpression (int start) : base (start) {}
		protected AspNetDataBindingExpression () {}
		
		protected override XObject NewInstance () { return new AspNetDataBindingExpression (); }
		
		public override string ToString ()
		{
			return string.Format ("[AspNetDataBindingExpression Location='{0}'", this.Position);
		}
		
		public override string FriendlyPathRepresentation {
			get { return "<%# %>"; }
		}
	}
	
	public class AspNetResourceExpression : AspNetExpression
	{
		public AspNetResourceExpression (int start, int end) : base (start, end) {}
		public AspNetResourceExpression (int start) : base (start) {}
		protected AspNetResourceExpression () {}
		
		protected override XObject NewInstance () { return new AspNetResourceExpression (); }
		
		public override string ToString ()
		{
			return string.Format ("[AspNetResourceExpression Location='{0}'", this.Position);
		}
		
		public override string FriendlyPathRepresentation {
			get { return "<%$ %>"; }
		}
	}
	
	public class AspNetServerComment : XNode
	{
		public AspNetServerComment (int start, int end) : base (start, end) {}
		public AspNetServerComment (int start) : base (start) {}
		protected AspNetServerComment () {}
		
		protected override XObject NewInstance () { return new AspNetServerComment (); }
		
		public override string ToString ()
		{
			return string.Format ("[AspNetServerComment Location='{0}'", this.Position);
		}
		
		public override string FriendlyPathRepresentation {
			get { return "<%-- --%>"; }
		}
	}
	
	public class AspNetRenderBlock : XNode
	{
		public AspNetRenderBlock (int start, int end) : base (start, end) {}
		public AspNetRenderBlock (int start) : base (start) {}
		protected AspNetRenderBlock () {}
		
		protected override XObject NewInstance () { return new AspNetRenderBlock (); }
		
		public override string ToString ()
		{
			return string.Format ("[AspNetRenderBlock Location='{0}'", this.Position);
		}
		
		
		public override string FriendlyPathRepresentation {
			get { return "<% %>"; }
		}
	}
}
