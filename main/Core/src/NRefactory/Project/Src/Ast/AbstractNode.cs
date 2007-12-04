// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1965 $</version>
// </file>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ICSharpCode.NRefactory.Ast
{
	public abstract class AbstractNode : INode
	{
		INode       parent;
		List<INode> children = new List<INode>();
		
		Location startLocation;
		Location endLocation;
		
		public INode Parent {
			get	{
				return parent;
			}
			set {
				parent = value;
			}
		}
		
		public Location StartLocation {
			get {
				return startLocation;
			}
			set {
				startLocation = value;
			}
		}
		
		public Location EndLocation {
			get {
				return endLocation;
			}
			set {
				endLocation = value;
			}
		}
		
		public List<INode> Children {
			get {
				return children;
			}
			set {
				Debug.Assert(value != null);
				children = value;
			}
		}
		
		public virtual void AddChild(INode childNode)
		{
			Debug.Assert(childNode != null);
			children.Add(childNode);
		}
		
		public abstract object AcceptVisitor(IAstVisitor visitor, object data);
		
		public virtual object AcceptChildren(IAstVisitor visitor, object data)
		{
			foreach (INode child in children) {
				Debug.Assert(child != null);
				child.AcceptVisitor(visitor, data);
			}
			return data;
		}
		
		public static string GetCollectionString(ICollection collection)
		{
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
					output.Append(en.Current == null ? "<null>" : en.Current.ToString());
				}
			} else {
				return "null";
			}
			
			output.Append('}');
			return output.ToString();
		}
	}
}
