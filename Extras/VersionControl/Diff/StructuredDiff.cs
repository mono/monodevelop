using System;
using System.Collections;
using System.Xml;

namespace Algorithm.Diff {
	public abstract class StructuredDiff {
		internal Hashtable nodeInterfaces = new Hashtable();
		internal Hashtable comparisonCache = new Hashtable();
		
		public virtual void AddInterface(Type type, NodeInterface nodeInterface) {
			nodeInterfaces[type] = nodeInterface;
		}
		
		internal NodeInterface GetInterface(object obj) {
			bool store = false;
			Type type = obj.GetType();
			while (type != null) {
				NodeInterface ret = (NodeInterface)nodeInterfaces[type];
				if (ret != null) {
					if (store) nodeInterfaces[obj.GetType()] = ret;
					return ret;
				}
				type = type.BaseType;
				store = true;
			}
			throw new ArgumentException("Node type has no interface defined: " + obj.GetType());
		}
		
		public float CompareLists(IList left, IList right) {
			return CompareLists(left, right, 0, false);
		}
		
		public void Compare(object left, object right) {
			comparisonCache.Clear();
			CompareLists(new object[] { left }, new object[] { right }, 0, true);
		}
		
		private float CompareLists(IList left, IList right, float threshold, bool output) {
			// Given two lists, find the elements in the list that correspond.
			// Two elements correspond if their 'difference metric' is less than
			// or equal to threshold.  For the hunks of correspondent items,
			// recursively descend into items not truly equal.  For hunks of
			// irreconsiliable material, raise the threshold to the next useful
			// level and rescan the items.
			
			if (left.Count == 0 && right.Count == 0)
				return 0;
			
			NodeComparerWrapper comparer = new NodeComparerWrapper(threshold, this);
			
			Diff diff = new Diff(left, right, comparer, new HashCodeProvider(this));
			
			int nitems = 0, ndiffs = 0;
			
			foreach (Diff.Hunk hunk in diff) {
				if (hunk.Same || (hunk.Left.Count == 1 && hunk.Right.Count == 1)) {
					// This comprises a block of correspondent items who
					// differ by no more than the threshold value.
					
					nitems += hunk.Left.Count;

					bool inSameRegion = false;
					
					for (int i = 0; i < hunk.Left.Count; i++) {
						object oleft = hunk.Left[i];
						object oright = hunk.Right[i];
						
						NodeInterface ileft = GetInterface(oleft);
						NodeInterface iright = GetInterface(oright);
						
						IList cleft = null, cright = null;
						cleft = ileft.GetChildren(oleft);
						cright = iright.GetChildren(oright);
						
						float comp = 0;
						if (ileft == iright)
							comp = ileft.Compare(oleft, oright, this);
						
						// If the nodes are equal, emit one node.
						if (ileft == iright && comp == 0) {
							if (output) {
								if (!inSameRegion) { WritePushSame(); inSameRegion = true; }
								WriteNodeSame(ileft, oleft, oright);
							}
							
						// Recurse into the lists of each node.
						} else if (ileft == iright && cleft != null && cright != null && cleft.Count > 0 && cright.Count > 0 && comp <= 1.0) {
							if (output && inSameRegion) { WritePopSame(); inSameRegion = false; }
							if (output) WritePushNode(ileft, oleft, oright);
							float d = CompareLists(cleft, cright, 0, output);
							d *= hunk.Left.Count;
							if (d < 1) d = 1;
							ndiffs += (int)d;
							if (output) WritePopNode();
							
						// The nodes are not equal, so emit removed and added nodes.
						} else {
							if (output && inSameRegion) { WritePopSame(); inSameRegion = false; }
							if (output) WriteNodeChange(ileft, oleft, iright, oright);
							ndiffs += hunk.Left.Count;
						}
					}
					
					if (output && inSameRegion) WritePopSame();
				} else {
					int ct = hunk.Left.Count + hunk.Right.Count;
					nitems += ct;
					ndiffs += ct;
					
					if (output) {
						bool noRecurse = comparer.minimumDifference >= 1;
						if (hunk.Right.Count == 0 || (hunk.Left.Count > 0 && noRecurse))
							WriteNodesRemoved(hunk.Left);
						if (hunk.Left.Count == 0 || (hunk.Right.Count > 0 && noRecurse))
							WriteNodesAdded(hunk.Right);
						if (hunk.Right.Count != 0 && hunk.Left.Count != 0 && !noRecurse)
							CompareLists(hunk.Left, hunk.Right, comparer.minimumDifference, output);
					}
				}
			}
			
			return (float)ndiffs / (float)nitems;
		}
		
		protected abstract void WritePushNode(NodeInterface nodeInterface, object left, object right);

		protected abstract void WritePushSame();
		
		protected abstract void WriteNodeSame(NodeInterface nodeInterface, object left, object right);

		protected abstract void WritePopSame();

		protected abstract void WriteNodeChange(NodeInterface leftInterface, object left, NodeInterface rightInterface, object right);

		protected abstract void WriteNodesRemoved(IList objects);

		protected abstract void WriteNodesAdded(IList objects);

		protected abstract void WritePopNode();
	}
		
	public class XmlOutputStructuredDiff : StructuredDiff {
		bool deepOutput, allContext;
		XmlWriter output;
		Hashtable contextNodes;
		
		public XmlOutputStructuredDiff(XmlWriter output, string context) {
			this.output = output;
			this.deepOutput = (context == null);
			this.allContext = (context != null && context == "*");
			
			if (!deepOutput && !allContext) {
				contextNodes = new Hashtable();
				foreach (string name in context.Split(','))
					contextNodes[name.Trim()] = contextNodes;
			}
		}
		
		public override void AddInterface(Type type, NodeInterface nodeInterface) {
			if (!(nodeInterface is XmlOutputNodeInterface))
				throw new ArgumentException("Node interfaces for the XmlOutputStructuredDiff must implement XmlOutputNodeInterface.");
			base.AddInterface(type, nodeInterface);
		}
		
		protected override void WritePushSame() {
		}
		
		protected override void WritePopSame() {
		}

		protected override void WriteNodeSame(NodeInterface nodeInterface, object left, object right) {
			bool deep = deepOutput;
			
			if (left is XmlNode && !deepOutput && !allContext) {
				if (!contextNodes.ContainsKey(((XmlNode)left).Name)) {
					return;
				} else {
					deep = true;
				}
			}

			((XmlOutputNodeInterface)nodeInterface).WriteBeginNode(left, left, (XmlWriter)output);
			output.WriteAttributeString("Status", "Same");
			if (deep)
				((XmlOutputNodeInterface)nodeInterface).WriteNodeChildren(left, (XmlWriter)output);
			output.WriteEndElement();
		}

		protected override void WriteNodeChange(NodeInterface leftInterface, object left, NodeInterface rightInterface, object right) {
			((XmlOutputNodeInterface)leftInterface).WriteBeginNode(left, right, (XmlWriter)output);
			output.WriteAttributeString("Status", "Changed");
				((XmlOutputNodeInterface)leftInterface).WriteBeginNode(left, left, (XmlWriter)output);
					((XmlOutputNodeInterface)leftInterface).WriteNodeChildren(left, (XmlWriter)output);
				output.WriteEndElement();
				((XmlOutputNodeInterface)rightInterface).WriteBeginNode(right, right, (XmlWriter)output);
					((XmlOutputNodeInterface)rightInterface).WriteNodeChildren(right, (XmlWriter)output);
				output.WriteEndElement();
			output.WriteEndElement();
		}

		protected override void WritePushNode(NodeInterface nodeInterface, object left, object right) {
			((XmlOutputNodeInterface)nodeInterface).WriteBeginNode(left, right, output);
		}

		protected override void WritePopNode() {
			output.WriteEndElement();
		}
		
		void AddRemove(IList objects, string status) {
			foreach (object obj in objects) {
				XmlOutputNodeInterface i = (XmlOutputNodeInterface)GetInterface(obj);
				((XmlOutputNodeInterface)i).WriteBeginNode(obj, obj, output);
				output.WriteAttributeString("Status", status);
				i.WriteBeginNode(obj, obj, output);
				i.WriteNodeChildren(obj, output);
				output.WriteEndElement();
				output.WriteEndElement();
			}
		}

		protected override void WriteNodesRemoved(IList objects) {
			AddRemove(objects, "Removed");
		}
		protected override void WriteNodesAdded(IList objects) {
			AddRemove(objects, "Added");
		}
	}
	
	public abstract class NodeInterface {
		public abstract IList GetChildren(object node);
		public abstract float Compare(object left, object right, StructuredDiff comparer);
		public virtual int GetHashCode(object node) {
			return node.GetHashCode();
		}
	}
	
	public interface XmlOutputNodeInterface {
		void WriteNodeChildren(object node, XmlWriter output);
		void WriteBeginNode(object left, object right, XmlWriter output);
	}
	
	internal class HashCodeProvider : IHashCodeProvider {
		StructuredDiff differ;
		public HashCodeProvider(StructuredDiff differ) { this.differ = differ; }
		public int GetHashCode(object obj) {
			return differ.GetInterface(obj).GetHashCode(obj);
		}
	}
	
	internal class NodeComparerWrapper : IComparer {
		float threshold;
		StructuredDiff differ;
		
		public float minimumDifference = 1;
		
		bool useCache = true;
		
		public NodeComparerWrapper(float threshold, StructuredDiff differ) {
			this.threshold = threshold;
			this.differ = differ;
		}
		
		private class Pair {
			object left, right;
			int code;
			public Pair(object a, object b, StructuredDiff differ) {
				left = a; right = b;
				code = unchecked(differ.GetInterface(left).GetHashCode(left) + differ.GetInterface(right).GetHashCode(right));
			}
			public override bool Equals(object o) {
				return ((Pair)o).left == left && ((Pair)o).right == right;
			}
			public override int GetHashCode() {
				return code;
			}
		}
		
		int IComparer.Compare(object left, object right) {
			float ret;
			
			Pair pair = new Pair(left, right, differ);
			
			if (left.GetType() != right.GetType()) {
				ret = 1;
			} else if (useCache && differ.comparisonCache.ContainsKey(pair)) {
				ret = (float)differ.comparisonCache[pair];
			} else {
				NodeInterface comparer = differ.GetInterface(left);
				ret = comparer.Compare(left, right, differ);
			}
			
			if (useCache)
				differ.comparisonCache[pair] = ret;
			
			if (ret < minimumDifference && ret > threshold)
				minimumDifference = ret;
			
			if (ret <= threshold)
				return 0;
			else
				return 1;
		}
	}

}
