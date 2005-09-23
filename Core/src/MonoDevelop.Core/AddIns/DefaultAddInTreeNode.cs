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

using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Core.AddIns.Codons;

namespace MonoDevelop.Core.AddIns
{
	/// <summary>
	/// This class represents a node in the <see cref="IAddInTree"/>
	/// </summary>
	public class DefaultAddInTreeNode : IAddInTreeNode
	{
		Hashtable childNodes = new Hashtable();
		ICodon    codon      = null;
		ConditionCollection conditionCollection = null;
		
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
			IAddInTreeNode node = this;
			int index = node.ChildNodes.Count;
			IAddInTreeNode[] sortedNodes = new IAddInTreeNode[index];
			Hashtable  visited   = new Hashtable(index);
			Hashtable  anchestor = new Hashtable(index);
			
			foreach(string key in node.ChildNodes.Keys) {
				visited[key] = false;
				anchestor[key] = new ArrayList();
			}
			
			foreach(DictionaryEntry child in node.ChildNodes){
				if(((IAddInTreeNode)child.Value).Codon.InsertAfter != null){
					for(int i = 0; i < ((IAddInTreeNode)child.Value).Codon.InsertAfter.Length; ++i){
//						Console.WriteLine(((IAddInTreeNode)child.Value).Codon.ID + " " + ((IAddInTreeNode)child.Value).Codon.InsertAfter[i].ToString());
						if(anchestor.Contains(((IAddInTreeNode)child.Value).Codon.InsertAfter[i].ToString())){
							((ArrayList)anchestor[((IAddInTreeNode)child.Value).Codon.InsertAfter[i].ToString()]).Add(child.Key);
						}
					}
				}
				
				if(((IAddInTreeNode)child.Value).Codon.InsertBefore != null){
					for(int i = 0; i < ((IAddInTreeNode)child.Value).Codon.InsertBefore.Length; ++i){
						if(anchestor.Contains(child.Key)){
							((ArrayList)anchestor[child.Key]).Add(((IAddInTreeNode)child.Value).Codon.InsertBefore[i]);
						}
					}
				}
			}
			
			string[] keyarray = new string[visited.Keys.Count];
			visited.Keys.CopyTo(keyarray, 0);
			
			for (int i = 0; i < keyarray.Length; ++i) {
				if((bool)visited[keyarray[i]] == false){
					index = Visit(keyarray[i], node.ChildNodes, sortedNodes, visited, anchestor, index);
				}
			}
			return sortedNodes;
		}
		
		int Visit(string key, Hashtable nodes, IAddInTreeNode[] sortedNodes, Hashtable visited, Hashtable anchestor, int index)
		{
			visited[key] = true;
			foreach (string anch in (ArrayList)anchestor[key]) {
				if ((bool)visited[anch] == false) {
					index = Visit(anch, nodes, sortedNodes, visited, anchestor, index);
				}
			}
			
			sortedNodes[--index] = (IAddInTreeNode)nodes[key];
			return index;
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
