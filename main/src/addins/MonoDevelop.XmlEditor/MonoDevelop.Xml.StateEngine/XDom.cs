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

using MonoDevelop.Projects.Dom;

namespace MonoDevelop.Xml.StateEngine
{
	public abstract class XObject
	{
		DomRegion region;
		XObject parent;
		
		public XObject (DomLocation start)
		{
			region = new DomRegion (start, DomLocation.Empty);
		}
		
		protected XObject (DomRegion region)
		{
			Debug.Assert (region.Start < region.End, "End must be greater than start.");
			this.region = region;
		}
		
		public XObject Parent {
			get { return parent; }
			internal protected set {
				parent = value;
				Debug.Assert (parent != null || !region.Start.IsEmpty, "When parent is null, start must not be negative.");
				Debug.Assert (parent.IsComplete, "Parent must be complete.");
				Debug.Assert (region.Start > parent.Region.End, "Start must greater than parent's end.");
			}
		}
		
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
		
		public void End (DomLocation endLocation)
		{
			Debug.Assert (region.Start < endLocation, "End must be greater than start.");
			Debug.Assert (region.Start < region.End, "XObject cannot be ended multiple times.");
			region.End = endLocation;
		}
		
		public bool IsEnded {
			get { return region.End > region.Start; }
		}
		
		public virtual bool IsComplete {
			get { return region.End > region.Start; }
		}
		
		public virtual void BuildTreeString (System.Text.StringBuilder builder, int indentLevel)
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
			Debug.Assert (copy.GetType () == this.GetType ());
			copy.ShallowCopyFrom (this);
			Debug.Assert (copy.region == this.region);
			return copy;
		}
		
		protected abstract XObject NewInstance ();
		
		protected virtual void ShallowCopyFrom (XObject copyFrom)
		{
			this.region = copyFrom.region; //immutable value type
		}
		
		protected XObject () {}
		
		public virtual string FriendlyPathRepresentation {
			get { return GetType ().ToString (); }
		}
	}
	
	public abstract class XNode : XObject
	{
		public XNode (DomLocation start) : base (start) {}
		protected XNode (DomRegion region) : base (region) {}
		
		XNode nextSibling;
		
		public XNode NextSibling {
			get { return nextSibling; }
			internal protected set {
				Debug.Assert (nextSibling == null, "The NextSibling cannot be changed after it is set.");
				Debug.Assert (value.Region.Start > Region.Start, "Start must greater than parent's end.");
				nextSibling = value;
			}
		}
		
		protected XNode () {}
	}
	
	public struct XName : IEquatable<XName>
	{
		string prefix;
		string name;
		
		public XName (string prefix, string name)
		{
			this.prefix = prefix;
			this.name = name;
			Debug.Assert (IsValid);
		}
		
		public XName (string name)
		{
			prefix = null;
			this.name = name;
			Debug.Assert (IsValid);
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
		
		public override bool Equals (object o)
		{
			if (!(o is XName))
				return false;
			return Equals ((XName) o); 
		}
		
		public override int GetHashCode ()
		{
			int hash = 0;
			if (prefix != null) hash += prefix.GetHashCode ();
			if (name != null) hash += name.GetHashCode ();
			return hash;
		}
		
		public XName ToLower ()
		{
			string lowerName = name == null? null : name.ToLower ();
			return prefix == null
				? new XName (lowerName)
				: new XName (prefix.ToLower (), lowerName);
		}
		
		public XName ToUpper ()
		{
			string upperName = name == null? null : name.ToUpper ();
			return prefix == null
				? new XName (upperName)
				: new XName (prefix.ToUpper (), upperName);
		}
		
		#endregion
	}
	
	public abstract class XContainer : XNode
	{
		public XContainer (DomLocation start) : base (start) {	}
		
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
		XName name;
		XAttributeCollection attributes;
		
		public XElement (DomLocation start) : base (start)
		{
			attributes = new XAttributeCollection (this);
		}
		
		public XElement (DomLocation start, XName name) : this (start)
		{
			this.name = name;
		}
		
		public XNode ClosingTag { get { return closingTag; } }
		public bool IsClosed { get { return closingTag != null; } }
		public bool IsSelfClosing { get { return closingTag == this; } }
		
		public void Close (XNode closingTag)
		{
			Debug.Assert (!IsClosed, "Element already closed.");
			Debug.Assert (closingTag == this | closingTag is XClosingTag);
			this.closingTag = closingTag;
			if (closingTag is XClosingTag)
				closingTag.Parent = this;
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
		
		public XAttributeCollection Attributes {
			get { return attributes; }
		}
		
		public override void AddChildNode (XNode newChild)
		{
			Debug.Assert (!IsClosed, "Cannot add children to a closed node.");
			base.AddChildNode (newChild);
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
			name = copyFromEl.name; //XName is immutable value type
		}
		
		public override string ToString ()
		{
			return string.Format ("[XElement Name='{0}' Location='{1}'",  name.FullName, this.Region);
		}
		
		public override void BuildTreeString (StringBuilder builder, int indentLevel)
		{
			builder.Append (' ', indentLevel * 2);
			builder.AppendFormat ("[XElement Name='{0}' Location='{1}' Children=", name.FullName, this.Region);
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
			get { return name.FullName; }
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
		XName name;
		string valu;
		
		public XAttribute (DomLocation start, XName name, string valu) : base (start)
		{
			this.name = name;
			this.valu = valu;
		}
		
		public XAttribute (DomLocation start) : base (start)
		{
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
		
		public string Value {
			get { return valu; }
			set {
				Debug.Assert (valu == null, "Should not set attribute value more than once.");
				valu = value;
			}
		}
		
		XAttribute nextSibling;
		
		public XAttribute NextSibling {
			get { return nextSibling; }
			internal protected set {
				Debug.Assert (nextSibling == null, "The NextSibling cannot be changed after it is set.");
				Debug.Assert (value.Region.Start > Region.Start, "Start must greater than parent's end.");
				nextSibling = value;
			}
		}
		
		protected XAttribute () {}
		protected override XObject NewInstance () { return new XAttribute (); }
		
		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			XAttribute copyFromAtt = (XAttribute) copyFrom;
			//immutable types
			name = copyFromAtt.name;
			valu = copyFromAtt.valu;
		}
		
		public override string ToString ()
		{
			return string.Format (
				"[XAttribute Name='{0}' Location='{1}' Value='{2}']", name.FullName, this.Region, this.valu);
		}
		
		public override string FriendlyPathRepresentation {
			get { return "@" + name.FullName; }
		}


	}
	
	public class XAttributeCollection : IEnumerable<XAttribute>
	{
		XObject parent;
		XAttribute firstChild;
		XAttribute lastChild;
		
		public XAttributeCollection (XObject parent)
		{
			Debug.Assert (parent != null);
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
		
		public void AddAttribute (XAttribute newChild)
		{
			System.Diagnostics.Debug.Assert (!parent.IsComplete, "Attributes cannot be added to a completed parent.");
			newChild.Parent = this.parent;
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
		public XCData (DomLocation start) : base (start) {}
		public XCData (DomRegion region) : base (region) {}
		
		protected XCData () {}
		protected override XObject NewInstance () { return new XCData (); }
		
		public override string FriendlyPathRepresentation {
			get { return "<![CDATA[ ]]>"; }
		}

	}
	
	public class XComment : XNode
	{
		public XComment (DomLocation start) : base (start) {}
		public XComment (DomRegion region) : base (region) {}
		
		protected XComment () {}
		protected override XObject NewInstance () { return new XComment (); }
		
		public override string FriendlyPathRepresentation {
			get { return "<!-- -->"; }
		}
	}
	
	public class XProcessingInstruction : XNode
	{
		public XProcessingInstruction (DomLocation start) : base (start) {}
		public XProcessingInstruction (DomRegion region) : base (region) {}
		
		protected XProcessingInstruction () {}
		protected override XObject NewInstance () { return new XProcessingInstruction (); }
		
		public override string FriendlyPathRepresentation {
			get { return "<? ?>"; }
		}
	}
	
	public class XDocType : XNode, INamedXObject 
	{
		public XDocType (DomLocation start) : base (start) {}
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
		XName name;
		
		public XClosingTag (DomLocation start) : base (start) {}
		
		public XClosingTag (XName name, DomLocation start) : base (start)
		{
			this.name = name;
		}
		
		public XName Name {
			get { return name; }
			set {
				Debug.Assert (string.IsNullOrEmpty (Name.Name) ||
				               (string.IsNullOrEmpty (Name.Prefix) && !string.IsNullOrEmpty (Name.Prefix)
				               && value.Name == Name.Name),
				             "Should not name node more than once.");
				name = value;
			}
		}
		
		public override bool IsComplete { get { return base.IsComplete && IsNamed; } }
		public bool IsNamed { get { return name.IsValid; } }
		
		protected XClosingTag () {}
		protected override XObject NewInstance () { return new XClosingTag (); }
		
		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			XClosingTag copyFromAtt = (XClosingTag) copyFrom;
			//immutable types
			name = copyFromAtt.name;
		}
		
		public override string FriendlyPathRepresentation {
			get { return "/" + name.FullName; }
		}

	}
	
	public class XDocument : XContainer
	{
		public XElement RootElement { get; private set; }
		
		public XDocument () : base (new DomLocation (1, 1)) {}
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
