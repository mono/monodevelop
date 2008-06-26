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
using System.Collections.Generic;

namespace MonoDevelop.Xml.StateEngine
{
	public class XObject
	{
		XPosition position;
		XObject parent;
		
		public XObject (int start)
		{
			position = new XPosition (start, -1);
		}
		
		public XObject Parent {
			get { return parent; }
			internal protected set {
				
#if DEBUG
				parent = value;
				if (parent == null) {
					if (start < 0)
						throw new ArgumentException ("When parent is null, start must not be negative.");
				} else if (!parent.IsComplete) {
					throw new ArgumentException ("Parent must be complete.");
				} else if (start <= parent.End) {
					throw new ArgumentException ("Start must greater than parent's end.");
				}
#endif
			}
		}
		
		public XPosition Position {
			get { return position; }	
		}
		
		public void End (int endPosition)
		{
			if (position.End < 0 || position.Start >= endPosition)
				throw new ArgumentException ();
			position = new XPosition (position.Start, endPosition);
		}
		
		public bool IsComplete {
			get { return position.End > position.Start; }
		}
	}
	
	public class XNode : XObject
	{
		public XNode (int start) : base (start) {}
		
		XNode nextSibling;
		
		public XNode NextSibling {
			get { return nextSibling; }
			internal protected set {
#if DEBUG
				if (nextSibling != null)
					throw new InvalidOperationException ("The NextSibling cannot be chnaged after it is set.");
				if (value.Position.Start <= Position.Start)
					throw new ArgumentException ("Start must greater than parent's end.");
#endif
				nextSibling = value;
			}
		}
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
	}
	
	public struct XName : IEquatable<XName>
	{
		string prefix;
		string name;
		
		public XName (string prefix, string name)
		{
			this.prefix = prefix;
			this.name = name;
		}
		
		public string Prefix { get { return prefix; } }
		public string Name { get { return name; } }
		public string FullName { get { return prefix == null? name : prefix + ':' + name; } }
		
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
		
		protected void AddChildNode (XNode newChild)
		{
			newChild.Parent = this;
			if (lastChild != null)
				lastChild.NextSibling = newChild;
			if (firstNode == null)
				firstNode = newChild;
			lastChild = newChild;
		}
	}
	
	public class XElement : XContainer
	{
		public XElement (int start, XName name) : base (start)
		{
			this.name = name;
			attributes = new AttributeCollection (this);
		}
		
		XElement closingElement;
		public XElement ClosingElement { get { return closingElement; } }
		public bool IsClosed { get { return closingElement != null; } }
		public bool IsSelfClosing { get { return closingElement == this; } }
		
		public void Close (XElement closingElement)
		{
#if DEBUG
			if (IsClosed)
				throw new InvalidOperationException ("Element already closed");
			//if (attributes
#endif
			this.closingElement = closingElement;
		}
		
		XName name;
		
		public XName Name {
			get { return name; }
		}
		
		AttributeCollection attributes;
		
		public AttributeCollection Attributes {
			get { return attributes; }
		}
	}
	
	public class XAttribute : XObject
	{
		XName name;
		string valu;
		
		public XAttribute (int start, XName name, string valu) : base (start)
		{
			this.name = name;
			this.valu = valu;
		}
		
		public XName Name { get { return name; } }
		public string Value { get { return valu; } }
		
		XAttribute nextSibling;
		
		public XAttribute NextSibling {
			get { return nextSibling; }
			internal protected set {
#if DEBUG
				if (nextSibling != null)
					throw new InvalidOperationException ("The NextSibling cannot be chnaged after it is set.");
				if (value.Position.Start <= Position.Start)
					throw new ArgumentException ("Start must greater than parent's end.");
#endif
				nextSibling = value;
			}
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
	}
	
	public class XProcessingInstruction : XNode
	{
		public XProcessingInstruction (int start) : base (start) {}
	}
	
	public class XDocType : XNode 
	{
		public XDocType (int start) : base (start) {}
	}
	
}
