// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Xml;

namespace MonoDevelop.Core.AddIns
{
	/// <summary>
	/// This class represents a node in the <see cref="IAddInTree"/>
	/// </summary>
	public class DefaultAddInTreeNode : IAddInTreeNode
	{
		Hashtable childNodes = new Hashtable();
		ArrayList sortedNodes;
		bool needsResort;
		ICodon    codon      = null;
		ConditionCollection conditionCollection = null;
		string[] allowedChildNodes;
		string description;
		AddIn ownerAddIn;
		
		/// <summary>
		/// Returns a hashtable containing the child nodes. Where the key is the
		/// node name and the value is a <see cref="IAddInTreeNode"/> object.
		/// </summary>
		public Hashtable ChildNodes {
			get {
				return childNodes;
			}
		}
		
		/// <summary>
		/// Returns a codon defined in this node, or <code>null</code> if no codon
		/// was defined.
		/// </summary>
		public ICodon Codon {
			get {
				return codon;
			}
			set {
				codon = value;
			}
		}
		
		/// <summary>
		/// Returns all conditions for this TreeNode.
		/// </summary>
		public ConditionCollection ConditionCollection {
			get {
				return conditionCollection;
			}
			set {
				conditionCollection = value;
			}
		}
		
		public string[] AllowedChildNodes {
			get { return allowedChildNodes; }
			set { allowedChildNodes = value; }
		}
		
		public string Description {
			get { return description; }
			set { description = value; }
		}
		
		public AddIn OwnerAddIn {
			get { return ownerAddIn; }
			set { ownerAddIn = value; }
		}
		
		internal void AddNode (string id, DefaultAddInTreeNode child)
		{
			ChildNodes [id] = child;
			if (sortedNodes == null)
				sortedNodes = new ArrayList ();
			sortedNodes.Add (child);
			needsResort = true;
		}

		
		/// <value>
		/// The current ConditionFailedAction of this node.
		/// </value>
		public ConditionFailedAction GetCurrentConditionFailedAction(object caller)
		{
			if (ConditionCollection == null) {
				return ConditionFailedAction.Nothing;
			}
			return ConditionCollection.GetCurrentConditionFailedAction(caller);
		}
		
		/// <summary>
		/// Get's the direct child nodes of the TreeNode node as an array. The 
		/// array is sorted acordingly to the insertafter and insertbefore preferences
		/// the node has using topoligical sort.
		/// </summary>
		/// <param name="node">
		/// The TreeNode which childs are given back.
		/// </param>
		/// <returns>
		/// A valid topological sorting of the childs of the TreeNode as an array.
		/// </returns>
		IAddInTreeNode[] GetSubnodesAsSortedArray()
		{
			if (!needsResort) {
				if (sortedNodes == null)
					return new IAddInTreeNode [0];
				else
					return (IAddInTreeNode[]) sortedNodes.ToArray (typeof(IAddInTreeNode));
			}
			
			needsResort = false;
			if (sortedNodes == null || sortedNodes.Count == 0)
				return new IAddInTreeNode [0];
			
			ArrayList sorted = new ArrayList ();
			foreach (IAddInTreeNode tnode in sortedNodes) {
				if (tnode.Codon == null) {
					sorted.Add (tnode);
					continue;
				}
				string[] insertAfters = tnode.Codon.InsertAfter;
				string[] insertBefores = tnode.Codon.InsertBefore;
				
				int bestPos = sorted.Count;

				if (insertAfters != null && insertAfters.Length > 0) {
					int numAfters = insertAfters.Length;
					int n;
					
					for (n=0; n<sorted.Count; n++) {
						IAddInTreeNode snode = (IAddInTreeNode) sorted [n];
						if (snode.Codon == null) continue;
						for (int i=0; i<insertAfters.Length; i++) {
							if (snode.Codon.ID == insertAfters [i])
								numAfters--;
						}
						if (numAfters == 0) {
							n++;
							break;
						}
					}
					bestPos = n;
				}
				
				if (insertBefores != null && insertBefores.Length != 0) {
					for (int n=0; n < bestPos; n++) {
						IAddInTreeNode snode = (IAddInTreeNode) sorted [n];
						if (snode.Codon == null) continue;
						for (int i=0; i<insertBefores.Length; i++) {
							if (snode.Codon.ID == insertBefores [i]) {
								bestPos = n;
								break;
							}
						}
					}
				}

				sorted.Insert (bestPos, tnode);
			}
			sortedNodes = sorted;
			return (IAddInTreeNode[]) sorted.ToArray (typeof(IAddInTreeNode));
		}
		
		public object Build (object caller)
		{
			ArrayList subItems = BuildChildItems (caller);
			return Codon.BuildItem (caller, subItems, null);
		}
		
		/// <summary>
		/// Builds one child item of this node using the <code>BuildItem</code>
		/// method of the codon in the child tree. The sub item with the <code>ID</code>
		/// <code>childItemID</code> will be build.
		/// </summary>
		public object BuildChildItem(string childItemID, object caller)
		{
			IAddInTreeNode[] sortedNodes = GetSubnodesAsSortedArray();
			
			foreach (IAddInTreeNode curNode in sortedNodes) {
				if (curNode.Codon.ID == childItemID) {
					ArrayList subItems = curNode.BuildChildItems(caller);
					return curNode.Codon.BuildItem(caller, subItems, null);
				}
				object o = curNode.BuildChildItem(childItemID, caller);
				if (o != null) {
					return o;
				}
			}
			
			return null;
		}
		
		
		public ArrayList BuildChildItems(object caller)
		{
			ArrayList items = new ArrayList();
			
			IAddInTreeNode[] sortedNodes = GetSubnodesAsSortedArray();
			
			foreach (IAddInTreeNode curNode in sortedNodes) {
				// don't include excluded childs.
				if (curNode.Codon == null) continue;
				ArrayList subItems = curNode.BuildChildItems(caller);
				object newItem = null;
				if (curNode.Codon.HandleConditions || curNode.ConditionCollection.GetCurrentConditionFailedAction(caller) == ConditionFailedAction.Nothing) {
					newItem = curNode.Codon.BuildItem(caller, subItems, curNode.ConditionCollection);
				}
				
				if (newItem != null) {
					items.Add(newItem); 
				}
			}
			return items;
		}
	}
}
