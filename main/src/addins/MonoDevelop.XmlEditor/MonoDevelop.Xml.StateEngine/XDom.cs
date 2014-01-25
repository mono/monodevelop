//
// XDom.cs
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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory;

namespace MonoDevelop.Xml.StateEngine
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

	public abstract class XNode : XObject
	{
		protected XNode (TextLocation start) : base (start) {}
		protected XNode (DomRegion region) : base (region) {}
		protected XNode () {}

		public XNode NextSibling { get; internal protected set; }
	}

	public struct XName : IEquatable<XName>
	{
		readonly string prefix;
		readonly string name;

		public XName (string prefix, string name)
		{
			this.prefix = prefix;
			this.name = name;
		}

		public XName (string name)
		{
			prefix = null;
			this.name = name;
		}

		public string Prefix { get { return prefix; } }
		public string Name { get { return name; } }
		public string FullName { get { return prefix == null? name : prefix + ':' + name; } }

		public bool IsValid { get { return !string.IsNullOrEmpty (name); } }
		public bool HasPrefix { get { return !string.IsNullOrEmpty (prefix); } }

		#region Equality

		public static bool operator == (XName x, XName y)
		{
			return x.Equals (y);
		}

		public static bool operator != (XName x, XName y)
		{
			return !x.Equals (y);
		}

		public bool Equals (XName other)
		{
			return prefix == other.prefix && name == other.name;
		}

		public override bool Equals (object obj)
		{
			if (!(obj is XName))
				return false;
			return Equals ((XName) obj);
		}

		public override int GetHashCode ()
		{
			int hash = 0;
			if (prefix != null) hash += prefix.GetHashCode ();
			if (name != null) hash += name.GetHashCode ();
			return hash;
		}

		#endregion

		public static bool Equals (XName a, XName b, bool ignoreCase)
		{
			StringComparison comp = ignoreCase? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			return string.Equals (a.prefix, b.prefix, comp) && string.Equals (a.name, b.name, comp);
		}
	}

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
			XElement copyFromEl = (XElement) copyFrom;
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

	public class XAttribute : XObject, INamedXObject
	{

		public XAttribute (TextLocation start, XName name, string value) : base (start)
		{
			this.Name = name;
			this.Value = value;
		}

		public XAttribute (TextLocation start) : base (start)
		{
		}

		public XName Name { get; set; }

		public override bool IsComplete { get { return base.IsComplete && IsNamed; } }
		public bool IsNamed { get { return Name.IsValid; } }

		public string Value { get; set; }
		public XAttribute NextSibling { get; internal protected set; }

		protected XAttribute () {}
		protected override XObject NewInstance () { return new XAttribute (); }

		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			XAttribute copyFromAtt = (XAttribute) copyFrom;
			//immutable types
			Name = copyFromAtt.Name;
			Value = copyFromAtt.Value;
		}

		public override string ToString ()
		{
			return string.Format (
				"[XAttribute Name='{0}' Location='{1}' Value='{2}']", Name.FullName, Region, Value);
		}

		public override string FriendlyPathRepresentation {
			get { return "@" + Name.FullName; }
		}


	}

	public class XAttributeCollection : IEnumerable<XAttribute>
	{
		readonly XObject parent;
		XAttribute firstChild;
		XAttribute lastChild;

		public XAttributeCollection (XObject parent)
		{
			this.parent = parent;
		}

		public Dictionary<XName, XAttribute> ToDictionary ()
		{
			Dictionary<XName, XAttribute> dict = new Dictionary<XName,XAttribute> ();
			XAttribute current = firstChild;
			while (current != null) {
				dict.Add (current.Name, current);
				current = current.NextSibling;
			}
			return dict;
		}

		public XAttribute this [XName name] {
			get {
				XAttribute current = firstChild;
				while (current != null) {
					if (current.Name == name)
						return current;
					current = current.NextSibling;
				}
				return null;
			}
		}

		public XAttribute this [int index] {
			get {
				XAttribute current = firstChild;
				while (current != null) {
					if (index == 0)
						return current;
					index--;
					current = current.NextSibling;
				}
				throw new IndexOutOfRangeException ();
			}
		}

		public XAttribute Get (XName name, bool ignoreCase)
		{
			XAttribute current = firstChild;
			while (current != null) {
				if (XName.Equals (current.Name, name, ignoreCase))
					return current;
				current = current.NextSibling;
			}
			return null;
		}

		public string GetValue (XName name, bool ignoreCase)
		{
			var att = Get (name, ignoreCase);
			return att != null? att.Value : null;
		}

		public void AddAttribute (XAttribute newChild)
		{
			newChild.Parent = parent;
			if (lastChild != null) {
				lastChild.NextSibling = newChild;
			}
			if (firstChild == null)
				firstChild = newChild;
			lastChild = newChild;
		}

		public IEnumerator<XAttribute> GetEnumerator ()
		{
			XAttribute current = firstChild;
			while (current != null) {
				yield return current;
				current = current.NextSibling;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			IEnumerator<XAttribute> en = GetEnumerator ();
			return en;
		}
	}

	public class XCData : XNode
	{
		public XCData (TextLocation start) : base (start) {}
		public XCData (DomRegion region) : base (region) {}

		protected XCData () {}
		protected override XObject NewInstance () { return new XCData (); }

		public override string FriendlyPathRepresentation {
			get { return "<![CDATA[ ]]>"; }
		}

	}

	public class XComment : XNode
	{
		public XComment (TextLocation start) : base (start) {}
		public XComment (DomRegion region) : base (region) {}

		protected XComment () {}
		protected override XObject NewInstance () { return new XComment (); }

		public override string FriendlyPathRepresentation {
			get { return "<!-- -->"; }
		}
	}

	public class XProcessingInstruction : XNode
	{
		public XProcessingInstruction (TextLocation start) : base (start) {}
		public XProcessingInstruction (DomRegion region) : base (region) {}

		protected XProcessingInstruction () {}
		protected override XObject NewInstance () { return new XProcessingInstruction (); }

		public override string FriendlyPathRepresentation {
			get { return "<? ?>"; }
		}
	}

	public class XDocType : XNode, INamedXObject
	{
		public XDocType (TextLocation start) : base (start) {}
		public XDocType (DomRegion region) : base (region) {}

		protected XDocType () {}
		protected override XObject NewInstance () { return new XDocType (); }

		public XName RootElement { get; set; }
		public string PublicFpi { get; set; }
		public bool IsPublic { get { return PublicFpi != null; } }
		public DomRegion InternalDeclarationRegion { get; set; }
		public string Uri { get; set; }

		public override string FriendlyPathRepresentation {
			get { return "<!DOCTYPE>"; }
		}

		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			XDocType copyFromDT = (XDocType) copyFrom;
			//immutable types
			RootElement = copyFromDT.RootElement;
			PublicFpi = copyFromDT.PublicFpi;
			InternalDeclarationRegion = copyFromDT.InternalDeclarationRegion;
			Uri = copyFromDT.Uri;
		}

		XName INamedXObject.Name {
			get { return RootElement; }
			set { RootElement = value; }
		}

		bool INamedXObject.IsNamed {
			get { return RootElement.IsValid; }
		}

		public override string ToString ()
		{
			return string.Format("[DocType: RootElement='{0}', PublicFpi='{1}',  InternalDeclarationRegion='{2}', Uri='{3}']",
			                     RootElement.FullName, PublicFpi, InternalDeclarationRegion, Uri);
		}
	}

	public class XClosingTag : XNode, INamedXObject
	{

		public XClosingTag (TextLocation start) : base (start) {}

		public XClosingTag (XName name, TextLocation start) : base (start)
		{
			this.Name = name;
		}

		public XName Name { get; set; }

		public override bool IsComplete { get { return base.IsComplete && IsNamed; } }
		public bool IsNamed { get { return Name.IsValid; } }

		protected XClosingTag () {}
		protected override XObject NewInstance () { return new XClosingTag (); }

		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			XClosingTag copyFromAtt = (XClosingTag) copyFrom;
			//immutable types
			Name = copyFromAtt.Name;
		}

		public override string FriendlyPathRepresentation {
			get { return "/" + Name.FullName; }
		}

	}

	public class XDocument : XContainer
	{
		public XElement RootElement { get; private set; }

		public XDocument () : base (new TextLocation (1, 1)) {}
		protected override XObject NewInstance () { return new XDocument (); }

		public override string FriendlyPathRepresentation {
			get { throw new InvalidOperationException ("Should not display document in path bar."); }
		}

		public override void AddChildNode (XNode newChild)
		{
			if (RootElement == null && newChild is XElement)
				RootElement = (XElement) newChild;
			base.AddChildNode (newChild);
		}

	}

	public interface INamedXObject
	{
		XName Name { get; set; }
		bool IsNamed { get; }
	}

	public interface IAttributedXObject : INamedXObject
	{
		XAttributeCollection Attributes { get; }
	}
}
