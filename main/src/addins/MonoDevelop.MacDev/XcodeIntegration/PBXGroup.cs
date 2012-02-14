// 
// PBXGroup.cs
//  
// Authors:
//       Geoff Norton <gnorton@novell.com>
//       Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2011 Novell, Inc.
// Copyright (c) 2011 Xamarin Inc.
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
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.MacDev.XcodeIntegration
{
	public class PBXGroup : XcodeObject, IEnumerable<XcodeObject>
	{
		XcodeObjectSortDirection sort;
		List<XcodeObject> children;
		string name;

		public PBXGroup (string name, XcodeObjectSortDirection sortDirection = XcodeObjectSortDirection.Ascending)
		{
			children = new List<XcodeObject> ();
			this.sort = sortDirection;
			this.name = name;
		}

		public override string Name {
			get {
				return name ?? "Main Group";
			}
		}

		public void AddChild (XcodeObject child)
		{
			children.Add (child);
		}
		
		public override XcodeType Type {
			get {
				return XcodeType.PBXGroup;
			}
		}
		
		public PBXGroup GetGroup (string name)
		{
			foreach (var v in children)
				if (v is PBXGroup && v.Name == name)
					return (PBXGroup) v;
			return null;
		}
		
		public override string ToString ()
		{
			var sb = new StringBuilder ();

			if (name != null)
				sb.AppendFormat ("{0} /* {1} */ = {{\n", Token, Name);
			else
				sb.AppendFormat ("{0} = {{\n", Token);
			sb.AppendFormat ("\t\t\tisa = {0};\n", Type);
			sb.AppendFormat ("\t\t\tchildren = (\n");
			if (sort != XcodeObjectSortDirection.None)
				children.Sort (new XcodeObjectComparer (sort));
			foreach (var child in children) 
				sb.AppendFormat ("\t\t\t\t{0} /* {1} */,\n", child.Token, child.Name);
			sb.AppendFormat ("\t\t\t);\n");
			if (name != null)
				sb.AppendFormat ("\t\t\tname = \"{0}\";\n", name);
			sb.AppendFormat ("\t\t\tsourceTree = \"<group>\";\n");
			sb.AppendFormat ("\t\t}};");

			return sb.ToString ();
		}

		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return ((System.Collections.IEnumerable)children).GetEnumerator ();
		}

		public IEnumerator<XcodeObject> GetEnumerator ()
		{
			return children.GetEnumerator ();
		}
		#endregion
	}
}
