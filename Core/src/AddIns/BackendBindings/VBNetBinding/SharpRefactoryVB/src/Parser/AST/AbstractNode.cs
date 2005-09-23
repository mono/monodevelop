// AbstractNode.cs
// Copyright (C) 2003 Mike Krueger (mike@icsharpcode.net)
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.Drawing;
using System.Text;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public abstract class AbstractNode : INode
	{
		INode     parent;
		ArrayList children = new ArrayList();
		Hashtable specials = new Hashtable();
		Point     startLocation;
		Point     endLocation;
		
		public INode Parent {
			get	{
				return parent;
			}
			set {
				parent = value;
			}
		}
		
		public Point StartLocation {
			get {
				return startLocation;
			}
			set {
				startLocation = value;
			}
		}
		
		public Point EndLocation {
			get {
				return endLocation;
			}
			set {
				endLocation = value;
			}
		}
		
		public Hashtable Specials {
			get {
				return specials;
			}
		}
		
		public ArrayList Children {
			get {
				return children;
			}
		}
		
		public virtual void AddChild(INode childNode)
		{
			children.Add(childNode);
		}
		
		public virtual object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public object AcceptChildren(IASTVisitor visitor, object data)
		{
			foreach (INode child in children) {
				if (child != null) {
					child.AcceptVisitor(visitor, data);
				}
			}
			return data;
		}
		
		public static string GetCollectionString(ICollection collection)
		{
			if (collection == null) {
				return "null";
			}
			StringBuilder output = new StringBuilder();
			output.Append('{');
			
			if (collection != null) {
				IEnumerator en = collection.GetEnumerator();
				bool isFirst = true;
				while (en.MoveNext()) {
					if (!isFirst) {
						output.Append(", ");
					} else {
						isFirst = false;
					}
					if (en.Current == null) {
						output.Append("null");
					} else {
						output.Append(en.Current.ToString());
					}
				}
			} else {
				return "null";
			}
			
			output.Append('}');
			return output.ToString();
		}
	}
}
