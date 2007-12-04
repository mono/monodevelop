//
// TreeNode.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Collections;
using Mono.Addins.Description;

namespace Mono.Addins
{
	class TreeNode
	{
		ArrayList childrenList;
		TreeNodeCollection children;
		ExtensionNode extensionNode;
		bool childrenLoaded;
		string id;
		TreeNode parent;
		ExtensionNodeSet nodeTypes;
		ExtensionPoint extensionPoint;
		BaseCondition condition;

		public TreeNode (string id)
		{
			this.id = id;
				
			// Root node
			if (id.Length == 0)
				childrenLoaded = true;
		}
		
		internal void AttachExtensionNode (ExtensionNode enode)
		{
			this.extensionNode = enode;
			if (extensionNode != null)
				extensionNode.SetTreeNode (this);
		}
		
		public string Id {
			get { return id; }
		}
		
		public ExtensionNode ExtensionNode {
			get {
				if (extensionNode == null && extensionPoint != null) {
					extensionNode = new ExtensionNode ();
					extensionNode.SetData (extensionPoint.RootAddin, null);
					AttachExtensionNode (extensionNode);
				}
				return extensionNode;
			}
		}
		
		public ExtensionPoint ExtensionPoint {
			get { return extensionPoint; }
			set { extensionPoint = value; }
		}
		
		public ExtensionNodeSet ExtensionNodeSet {
			get { return nodeTypes; }
			set { nodeTypes = value; }
		}
		
		public TreeNode Parent {
			get { return parent; }
		}
		
		public BaseCondition Condition {
			get { return condition; }
			set {
				condition = value;
			}
		}
		
		public virtual ExtensionContext Context {
			get {
				if (parent != null)
					return parent.Context;
				else
					return null;
			}
		}
		
		public bool IsEnabled {
			get {
				if (condition == null)
					return true;
				ExtensionContext ctx = Context;
				if (ctx == null)
					return true;
				else
					return condition.Evaluate (ctx);
			}
		}
		
		public bool ChildrenLoaded {
			get { return childrenLoaded; }
		}
		
		public void AddChildNode (TreeNode node)
		{
			node.parent = this;
			if (childrenList == null)
				childrenList = new ArrayList ();
			childrenList.Add (node);
		}
		
		public void InsertChildNode (int n, TreeNode node)
		{
			node.parent = this;
			if (childrenList == null)
				childrenList = new ArrayList ();
			childrenList.Insert (n, node);
			
			// Dont call NotifyChildrenChanged here. It is called by ExtensionTree,
			// after inserting all children of the node.
		}
		
		internal int ChildCount {
			get { return childrenList == null ? 0 : childrenList.Count; }
		}
		
		public ExtensionNode GetExtensionNode (string path, string childId)
		{
			TreeNode node = GetNode (path, childId);
			return node != null ? node.ExtensionNode : null;
		}
		
		public ExtensionNode GetExtensionNode (string path)
		{
			TreeNode node = GetNode (path);
			return node != null ? node.ExtensionNode : null;
		}
		
		public TreeNode GetNode (string path, string childId)
		{
			if (childId == null || childId.Length == 0)
				return GetNode (path);
			else
				return GetNode (path + "/" + childId);
		}
		
		public TreeNode GetNode (string path)
		{
			return GetNode (path, false);
		}
		
		public TreeNode GetNode (string path, bool buildPath)
		{
			if (path.StartsWith ("/"))
				path = path.Substring (1);

			string[] parts = path.Split ('/');
			TreeNode curNode = this;

			foreach (string part in parts) {
				int i = curNode.Children.IndexOfNode (part);
				if (i != -1) {
					curNode = curNode.Children [i];
					continue;
				}
				
				if (buildPath) {
					TreeNode newNode = new TreeNode (part);
					curNode.AddChildNode (newNode);
					curNode = newNode;
				} else
					return null;
			}
			return curNode;
		}
		
		public TreeNodeCollection Children {
			get {
				if (!childrenLoaded) {
					childrenLoaded = true;
					if (extensionPoint != null)
						Context.LoadExtensions (GetPath ());
					// We have to keep the relation info, since add-ins may be loaded/unloaded
				}
				if (childrenList == null)
					return TreeNodeCollection.Empty;
				if (children == null)
					children = new TreeNodeCollection (childrenList);
				return children;
			}
		}
		
		public string GetPath ()
		{
			int num=0;
			TreeNode node = this;
			while (node != null) {
				num++;
				node = node.parent;
			}
			
			string[] ids = new string [num];
			
			node = this;
			while (node != null) {
				ids [--num] = node.id;
				node = node.parent;
			}
			return string.Join ("/", ids);
		}
		
		public void NotifyAddinLoaded (RuntimeAddin ad, bool recursive)
		{
			if (extensionNode != null && extensionNode.AddinId == ad.Addin.Id)
				extensionNode.OnAddinLoaded ();
			if (recursive && childrenLoaded) {
				foreach (TreeNode node in Children.Clone ())
					node.NotifyAddinLoaded (ad, true);
			}
		}
		
		public ExtensionPoint FindLoadedExtensionPoint (string path)
		{
			if (path.StartsWith ("/"))
				path = path.Substring (1);

			string[] parts = path.Split ('/');
			TreeNode curNode = this;

			foreach (string part in parts) {
				int i = curNode.Children.IndexOfNode (part);
				if (i != -1) {
					curNode = curNode.Children [i];
					if (!curNode.ChildrenLoaded)
						return null;
					if (curNode.ExtensionPoint != null)
						return curNode.ExtensionPoint;
					continue;
				}
				return null;
			}
			return null;
		}
		
		public void FindAddinNodes (string id, ArrayList nodes)
		{
			if (id != null && extensionPoint != null && extensionPoint.RootAddin == id) {
				// It is an extension point created by the add-in. All nodes below this
				// extension point will be added to the list, even if they come from other add-ins.
				id = null;
			}

			if (childrenLoaded) {
				// Deep-first search, to make sure children are removed before the parent.
				foreach (TreeNode node in Children)
					node.FindAddinNodes (id, nodes);
			}
			
			if (id == null || (ExtensionNode != null && ExtensionNode.AddinId == id))
				nodes.Add (this);
		}
		
		public bool FindExtensionPathByType (IProgressStatus monitor, Type type, string nodeName, out string path, out string pathNodeName)
		{
			if (extensionPoint != null) {
				foreach (ExtensionNodeType nt in extensionPoint.NodeSet.NodeTypes) {
					if (nt.ObjectTypeName.Length > 0 && (nodeName.Length == 0 || nodeName == nt.Id)) {
						RuntimeAddin addin = AddinManager.SessionService.GetAddin (extensionPoint.RootAddin);
						Type ot = addin.GetType (nt.ObjectTypeName);
						if (ot != null) {
							if (ot.IsAssignableFrom (type)) {
								path = extensionPoint.Path;
								pathNodeName = nt.Id;
								return true;
							}
						}
						else
							monitor.ReportError ("Type '" + nt.ObjectTypeName + "' not found in add-in '" + Id + "'", null);
					}
				}
			}
			else {
				foreach (TreeNode node in Children) {
					if (node.FindExtensionPathByType (monitor, type, nodeName, out path, out pathNodeName))
						return true;
				}
			}
			path = null;
			pathNodeName = null;
			return false;
		}
		
		public void Remove ()
		{
			if (parent != null) {
				if (Condition != null)
					Context.UnregisterNodeCondition (this, Condition);
				parent.childrenList.Remove (this);
				parent.NotifyChildrenChanged ();
			}
		}
		
		public bool NotifyChildrenChanged ()
		{
			if (extensionNode != null)
				return extensionNode.NotifyChildChanged ();
			else
				return false;
		}
	}
}
