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

namespace MonoDevelop.Xml.StateEngine
{
	public abstract class XObject
	{
		XPosition position;
		XObject parent;
		
		public XObject (int start)
		{
			position = new XPosition (start, -1);
		}
		
		protected XObject (int start, int end)
		{
			Debug.Assert (start < end, "End must be greater than start.");
			position = new XPosition (start, end);
		}
		
		public XObject Parent {
			get { return parent; }
			internal protected set {
				parent = value;
				Debug.Assert (parent != null || position.Start >= 0, "When parent is null, start must not be negative.");
				Debug.Assert (parent.IsComplete, "Parent must be complete.");
				Debug.Assert (position.Start > parent.Position.End, "Start must greater than parent's end.");
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
		
		public XPosition Position {
			get { return position; }	
		}
		
		public void End (int endPosition)
		{
			Debug.Assert (position.Start < endPosition, "End must be greater than start.");
			Debug.Assert (position.End < 0, "XObject cannot be ended multiple times.");
			position = new XPosition (position.Start, endPosition);
		}
		
		public virtual bool IsComplete {
			get { return position.End > position.Start; }
		}
		
		public virtual void BuildTreeString (System.Text.StringBuilder builder, int indentLevel)
		{
			builder.Append (' ', indentLevel * 2);
			builder.AppendFormat (ToString ());
			builder.AppendLine ();
		}
		
		public override string ToString ()
		{
			return string.Format ("[{0} Location='{1}']", GetType (), Position);
		}
		
		//creates a parallel tree -- should NOT retain references into old tree
		public XObject ShallowCopy ()
		{
			XObject copy = NewInstance ();
			Debug.Assert (copy.GetType () == this.GetType ());
			copy.ShallowCopyFrom (this);
			Debug.Assert (copy.position == this.position);
			return copy;
		}
		
		protected abstract XObject NewInstance ();
		
		protected virtual void ShallowCopyFrom (XObject copyFrom)
		{
			copyFrom.position = this.position; //immutable value type
		}
		
		protected XObject () {}
	}
	
	public abstract class XNode : XObject
	{
		public XNode (int start) : base (start) {}
		protected XNode (int start, int end) : base (start, end) {}
		
		XNode nextSibling;
		
		public XNode NextSibling {
			get { return nextSibling; }
			internal protected set {
				Debug.Assert (nextSibling == null, "The NextSibling cannot be changed after it is set.");
				Debug.Assert (value.Position.Start > Position.Start, "Start must greater than parent's end.");
				nextSibling = value;
			}
		}
		
		protected XNode () {}
	}
	
	public struct XPosition : IEquatable<XPosition>
	{
		int start, end;
		
		public XPosition (int start, int end)
		{
			this.start = start;
			this.end = end;
		}
		
		public int Start { get { return start; } }
		public int End { get { return end; } }
		
		#region Equality
		
		public static bool operator == (XPosition x, XPosition y)
		{
			return x.Equals (y);
		}
		
		public static bool operator != (XPosition x, XPosition y)
		{
			return !x.Equals (y);
		}
		
		public bool Equals (XPosition other)
		{
			return start == other.start && end == other.end;
		}
		
		public override bool Equals (object o)
		{
			if (!(o is XPosition))
				return false;
			return Equals ((XPosition) o); 
		}
		
		public override int GetHashCode ()
		{
			return start ^ end;
		}
		
		#endregion
		
		public override string ToString ()
		{
			return string.Format ("[{0}:{1}]", start, end);
		}

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
		
		#endregion
	}
	
	public abstract class XContainer : XNode
	{
		public XContainer (int start) : base (start) {	}
		
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
		
		public IEnumerable<XNode> RecursiveNodes {
			get {
				XNode next = firstNode;
				while (true) {
					yield return next;
					XContainer container = next as XContainer;
					if (container != null && container.FirstChild != null)
						next = ((XContainer)next).FirstChild;
					else if (next.NextSibling != null)
						next = next.NextSibling;
					else if (next.Parent != this)
						next = (XContainer) next.Parent;
					else
						break;
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
	
	public class XElement : XContainer, INamedXObject
	{
		XNode closingTag;
		XName name;
		AttributeCollection attributes;
		
		public XElement (int start) : base (start)
		{
			attributes = new AttributeCollection (this);
		}
		
		public XElement (int start, XName name) : base (start)
		{
			this.name = name;
			attributes = new AttributeCollection (this);
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
		
		public AttributeCollection Attributes {
			get { return attributes; }
		}
		
		public override void AddChildNode (XNode newChild)
		{
			Debug.Assert (!IsClosed, "Cannot add children to a closed node.");
			base.AddChildNode (newChild);
		}
		
		protected XElement () {}
		protected override XObject NewInstance () { return new XElement (); }
		
		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			XElement copyFromEl = (XElement) copyFrom;
			name = copyFromEl.name; //XName is immutable value type
		}
		
		public override string ToString ()
		{
			return string.Format ("[XElement Name='{0}' Location='{1}'",  name.FullName, this.Position);
		}
		
		public override void BuildTreeString (StringBuilder builder, int indentLevel)
		{
			builder.Append (' ', indentLevel * 2);
			builder.AppendFormat ("[XElement Name='{0}' Location='{1}' Children=", name.FullName, this.Position);
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

	}
	
	public class XAttribute : XObject, INamedXObject
	{
		XName name;
		string valu;
		
		public XAttribute (int start, XName name, string valu) : base (start)
		{
			this.name = name;
			this.valu = valu;
		}
		
		public XAttribute (int start) : base (start)
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
				Debug.Assert (value.Position.Start > Position.Start, "Start must greater than parent's end.");
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
				"[XAttribute Name='{0}' Location='{1}' Value='{2}']", name.FullName, this.Position, this.valu);
		}

	}
	
	public class AttributeCollection : IEnumerable<XAttribute>
	{
		XContainer parent;
		XAttribute firstChild;
		XAttribute lastChild;
		
		public AttributeCollection (XContainer parent)
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
		public XCData (int start) : base (start) {}
		public XCData (int start, int end) : base (start, end) {}
		
		protected XCData () {}
		protected override XObject NewInstance () { return new XCData (); }
	}
	
	public class XComment : XNode
	{
		public XComment (int start) : base (start) {}
		public XComment (int start, int end) : base (start, end) {}
		
		protected XComment () {}
		protected override XObject NewInstance () { return new XComment (); }
	}
	
	public class XProcessingInstruction : XNode
	{
		public XProcessingInstruction (int start) : base (start) {}
		public XProcessingInstruction (int start, int end) : base (start, end) {}
		
		protected XProcessingInstruction () {}
		protected override XObject NewInstance () { return new XProcessingInstruction (); }
	}
	
	public class XDocType : XNode 
	{
		public XDocType (int start) : base (start) {}
		public XDocType (int start, int end) : base (start, end) {}
		
		protected XDocType () {}
		protected override XObject NewInstance () { return new XDocType (); }
	}
	
	public class XClosingTag : XNode, INamedXObject
	{
		XName name;
		
		public XClosingTag (int start) : base (start) {}
		
		public XClosingTag (XName name, int start) : base (start)
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
	}
	
	public class XDocument : XContainer
	{
		public XDocument () : base (0) {}
		protected override XObject NewInstance () { return new XDocument (); }
	}
	
	public interface INamedXObject
	{
		XName Name { get; set; }
		bool IsNamed { get; }
	}
}
