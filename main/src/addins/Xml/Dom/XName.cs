//
// XName.cs
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

namespace MonoDevelop.Xml.Dom
{

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
			var comp = ignoreCase? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			return string.Equals (a.prefix, b.prefix, comp) && string.Equals (a.name, b.name, comp);
		}

		public override string ToString ()
		{
			if (!HasPrefix)
				return name;
			return prefix + ":" + name;
		}
	}
}
