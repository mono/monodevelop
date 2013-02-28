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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory;
using System.Linq;

namespace MonoDevelop.AspNet.StateEngine
{
	
	public class AspNetDirective : XNode, IAttributedXObject
	{
		XName name;
		XAttributeCollection attributes;
		
		public AspNetDirective (TextLocation start) : base (start)
		{
			attributes = new XAttributeCollection (this);
		}
		
		public AspNetDirective (TextLocation start, XName name) : this (start)
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
			return string.Format ("[AspNetDirective Name='{0}' Location='{1}'",  name.FullName, this.Region);
		}
		
		public override void BuildTreeString (StringBuilder builder, int indentLevel)
		{
			builder.Append (' ', indentLevel * 2);
			builder.AppendFormat ("[AspNetDirective Name='{0}' Location='{1}' Children=", name.FullName, this.Region);
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
		public AspNetExpression (DomRegion region) : base (region) {}
		public AspNetExpression (TextLocation start) : base (start) {}
		protected AspNetExpression () {}
	}
	
	
	public class AspNetRenderExpression : AspNetExpression
	{
		public AspNetRenderExpression (DomRegion region) : base (region) {}
		public AspNetRenderExpression (TextLocation start) : base (start) {}
		protected AspNetRenderExpression () {}
		
		protected override XObject NewInstance () { return new AspNetRenderExpression (); }
		
		public override string ToString ()
		{
			return string.Format ("[AspNetRenderExpression Location='{0}'", this.Region);
		}
		
		public override string FriendlyPathRepresentation {
			get { return "<%= %>"; }
		}
	}
	
	public class AspNetHtmlEncodedExpression : AspNetExpression
	{
		public AspNetHtmlEncodedExpression (DomRegion region) : base (region) {}
		public AspNetHtmlEncodedExpression (TextLocation start) : base (start) {}
		protected AspNetHtmlEncodedExpression () {}
		
		protected override XObject NewInstance () { return new AspNetHtmlEncodedExpression (); }
		
		public override string ToString ()
		{
			return string.Format ("[AspNetHtmlEncodedExpression Location='{0}'", this.Region);
		}
		
		public override string FriendlyPathRepresentation {
			get { return "<%: %>"; }
		}
	}
	
	public class AspNetDataBindingExpression : AspNetExpression
	{
		public AspNetDataBindingExpression (DomRegion region) : base (region) {}
		public AspNetDataBindingExpression (TextLocation start) : base (start) {}
		protected AspNetDataBindingExpression () {}
		
		protected override XObject NewInstance () { return new AspNetDataBindingExpression (); }
		
		public override string ToString ()
		{
			return string.Format ("[AspNetDataBindingExpression Location='{0}'", this.Region);
		}
		
		public override string FriendlyPathRepresentation {
			get { return "<%# %>"; }
		}
	}
	
	public class AspNetResourceExpression : AspNetExpression
	{
		public AspNetResourceExpression (DomRegion region) : base (region) {}
		public AspNetResourceExpression (TextLocation start) : base (start) {}
		protected AspNetResourceExpression () {}
		
		protected override XObject NewInstance () { return new AspNetResourceExpression (); }
		
		public override string ToString ()
		{
			return string.Format ("[AspNetResourceExpression Location='{0}'", this.Region);
		}
		
		public override string FriendlyPathRepresentation {
			get { return "<%$ %>"; }
		}
	}
	
	public class AspNetServerComment : XNode
	{
		public AspNetServerComment (DomRegion region) : base (region) {}
		public AspNetServerComment (TextLocation start) : base (start) {}
		protected AspNetServerComment () {}
		
		protected override XObject NewInstance () { return new AspNetServerComment (); }
		
		public override string ToString ()
		{
			return string.Format ("[AspNetServerComment Location='{0}'", this.Region);
		}
		
		public override string FriendlyPathRepresentation {
			get { return "<%-- --%>"; }
		}
	}
	
	public class AspNetRenderBlock : XNode
	{
		public AspNetRenderBlock (DomRegion region) : base (region) {}
		public AspNetRenderBlock (TextLocation start) : base (start) {}
		protected AspNetRenderBlock () {}
		
		protected override XObject NewInstance () { return new AspNetRenderBlock (); }
		
		public override string ToString ()
		{
			return string.Format ("[AspNetRenderBlock Location='{0}'", this.Region);
		}
		
		
		public override string FriendlyPathRepresentation {
			get { return "<% %>"; }
		}
	}

	public static class AspNetDomExtensions
	{
		static XName scriptName = new XName ("script");
		static XName runatName = new XName ("runat");
		static XName idName = new XName ("id");

		public static bool IsRunatServer (this XElement el)
		{
			var val = el.Attributes.GetValue (runatName, true);
			return string.Equals (val, "server", StringComparison.OrdinalIgnoreCase);
		}

		public static string GetId (this IAttributedXObject el)
		{
			return el.Attributes.GetValue (idName, true);
		}

		public static bool IsServerScriptTag (this XElement el)
		{
			return XName.Equals (el.Name, scriptName, true) && IsRunatServer (el);
		}

		public static IEnumerable<T> WithName<T> (this IEnumerable<XNode> nodes, XName name, bool ignoreCase) where T : XNode, INamedXObject
		{
			return nodes.OfType<T> ().Where (el => XName.Equals (el.Name, name, true));
		}

		public static IEnumerable<string> GetAllPlaceholderIds (this XDocument doc)
		{
			return doc.AllDescendentNodes
				.WithName<XElement> (new XName ("asp", "ContentPlaceHolder"), true)
				.Where (x => x.IsRunatServer ())
				.Select (x => x.GetId ())
				.Where (id => !string.IsNullOrEmpty (id));
		}
	}
}
