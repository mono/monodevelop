//
// XAttributeCollection.cs
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

using System;
using System.Collections.Generic;

namespace MonoDevelop.Xml.Dom
{
	public class XAttributeCollection : IEnumerable<XAttribute>
	{
		readonly XObject parent;

		public XAttribute Last { get; private set; }
		public XAttribute First { get; private set; }
		public int Count { get; private set; }

		public XAttributeCollection (XObject parent)
		{
			this.parent = parent;
		}

		public Dictionary<XName, XAttribute> ToDictionary ()
		{
			var dict = new Dictionary<XName,XAttribute> ();
			XAttribute current = First;
			while (current != null) {
				dict.Add (current.Name, current);
				current = current.NextSibling;
			}
			return dict;
		}

		public XAttribute this [XName name] {
			get {
				XAttribute current = First;
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
				XAttribute current = First;
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
			XAttribute current = First;
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
			if (Last != null) {
				Last.NextSibling = newChild;
			}
			if (First == null)
				First = newChild;
			Last = newChild;
			Count++;
		}

		public IEnumerator<XAttribute> GetEnumerator ()
		{
			XAttribute current = First;
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
}
