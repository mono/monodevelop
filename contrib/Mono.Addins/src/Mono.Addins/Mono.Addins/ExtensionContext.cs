//
// ExtensionContext.cs
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
using System.Collections;
using System.Collections.Specialized;
using Mono.Addins.Description;

namespace Mono.Addins
{
	public class ExtensionContext
	{
		Hashtable conditionTypes = new Hashtable ();
		Hashtable conditionsToNodes = new Hashtable ();
		ArrayList childContexts;
		ExtensionContext parentContext;
		ExtensionTree tree;
		bool fireEvents = false;
		
		ArrayList runTimeEnabledAddins;
		ArrayList runTimeDisabledAddins;
		
		public event ExtensionEventHandler ExtensionChanged;
		
		~ExtensionContext ()
		{
			if (parentContext != null)
				parentContext.DisposeChildContext (this);
		}
		
		internal ExtensionContext ()
		{
			tree = new ExtensionTree (this);
		}
		
		internal ExtensionContext CreateChildContext ()
		{
			lock (conditionTypes) {
				if (childContexts == null)
					childContexts = new ArrayList ();
				ExtensionContext ctx = new ExtensionContext ();
				ctx.parentContext = this;
				WeakReference wref = new WeakReference (ctx);
				childContexts.Add (wref);
				return ctx;
			}
		}
		
		internal void DisposeChildContext (ExtensionContext ctx)
		{
			lock (conditionTypes) {
				foreach (WeakReference wref in childContexts) {
					if (wref.Target == ctx) {
						childContexts.Remove (wref);
						return;
					}
				}
			}
		}
		
		public void RegisterCondition (string id, ConditionType type)
		{
			type.Id = id;
			ConditionInfo info = CreateConditionInfo (id);
			ConditionType ot = info.CondType as ConditionType;
			if (ot != null)
				ot.Changed -= new EventHandler (OnConditionChanged);
			info.CondType = type;
			type.Changed += new EventHandler (OnConditionChanged);
		}
		
		public void RegisterCondition (string id, Type type)
		{
			// Allows delayed creation of condition types
			ConditionInfo info = CreateConditionInfo (id);
			ConditionType ot = info.CondType as ConditionType;
			if (ot != null)
				ot.Changed -= new EventHandler (OnConditionChanged);
			info.CondType = type;
		}
		
		ConditionInfo CreateConditionInfo (string id)
		{
			ConditionInfo info = conditionTypes [id] as ConditionInfo;
			if (info == null) {
				info = new ConditionInfo ();
				conditionTypes [id] = info;
			}
			return info;
		}
		
		internal bool FireEvents {
			get { return fireEvents; }
		}
		
		internal ConditionType GetCondition (string id)
		{
			ConditionType ct;
			ConditionInfo info = (ConditionInfo) conditionTypes [id];
			
			if (info != null) {
				if (info.CondType is Type) {
					// The condition was registered as a type, create an instance now
					ct = (ConditionType) Activator.CreateInstance ((Type)info.CondType);
					ct.Id = id;
					ct.Changed += new EventHandler (OnConditionChanged);
					info.CondType = ct;
				}
				else
					ct = info.CondType as ConditionType;

				if (ct != null)
					return ct;
			}
			
			if (parentContext != null)
				return parentContext.GetCondition (id);
			else
				return null;
		}
		
		internal void RegisterNodeCondition (TreeNode node, BaseCondition cond)
		{
			ArrayList list = (ArrayList) conditionsToNodes [cond];
			if (list == null) {
				list = new ArrayList ();
				conditionsToNodes [cond] = list;
				ArrayList conditionTypeIds = new ArrayList ();
				cond.GetConditionTypes (conditionTypeIds);
				
				foreach (string cid in conditionTypeIds) {
				
					// Make sure the condition is properly created
					GetCondition (cid);
					
					ConditionInfo info = CreateConditionInfo (cid);
					if (info.BoundConditions == null)
						info.BoundConditions = new ArrayList ();
						
					info.BoundConditions.Add (cond);
				}
			}
			list.Add (node);
		}
		
		internal void UnregisterNodeCondition (TreeNode node, BaseCondition cond)
		{
			ArrayList list = (ArrayList) conditionsToNodes [cond];
			if (list == null)
				return;
			
			list.Remove (node);
			if (list.Count == 0) {
				conditionsToNodes.Remove (cond);
				ArrayList conditionTypeIds = new ArrayList ();
				cond.GetConditionTypes (conditionTypeIds);
				foreach (string cid in conditionTypes.Keys) {
					ConditionInfo info = conditionTypes [cid] as ConditionInfo;
					if (info != null && info.BoundConditions != null)
						info.BoundConditions.Remove (cond);
				}
			}
		}
		
		public ExtensionNode GetExtensionNode (string path)
		{
			TreeNode node = GetNode (path);
			if (node == null)
				return null;
			
			if (node.Condition == null || node.Condition.Evaluate (this))
				return node.ExtensionNode;
			else
				return null;
		}
		
		public ExtensionNodeList GetExtensionNodes (string path)
		{
			return GetExtensionNodes (path, null);
		}
		
		public ExtensionNodeList GetExtensionNodes (string path, Type expectedNodeType)
		{
			TreeNode node = GetNode (path);
			if (node == null || node.ExtensionNode == null)
				return ExtensionNodeList.Empty;
			
			ExtensionNodeList list = node.ExtensionNode.ChildNodes;
			
			if (expectedNodeType != null) {
				bool foundError = false;
				foreach (ExtensionNode cnode in list) {
					if (!expectedNodeType.IsInstanceOfType (cnode)) {
						foundError = true;
						AddinManager.ReportError ("Error while getting nodes for path '" + path + "'. Expected subclass of node type '" + expectedNodeType + "'. Found '" + cnode.GetType (), null, null, false);
					}
				}
				if (foundError) {
					// Create a new list excluding the elements that failed the test
					ArrayList newList = new ArrayList ();
					foreach (ExtensionNode cnode in list) {
						if (expectedNodeType.IsInstanceOfType (cnode))
							newList.Add (cnode);
					}
					return new ExtensionNodeList (newList);
				}
			}
			return list;
		}
		
		public object[] GetExtensionObjects (Type instanceType)
		{
			return GetExtensionObjects (instanceType, true);
		}
		
		public object[] GetExtensionObjects (Type instanceType, bool reuseCachedInstance)
		{
			string path = AddinManager.SessionService.GetAutoTypeExtensionPoint (instanceType);
			if (path == null)
				return (object[]) Array.CreateInstance (instanceType, 0);
			return GetExtensionObjects (path, instanceType, reuseCachedInstance);
		}
		
		public object[] GetExtensionObjects (string path)
		{
			return GetExtensionObjects (path, typeof(object), true);
		}
		
		public object[] GetExtensionObjects (string path, bool reuseCachedInstance)
		{
			return GetExtensionObjects (path, typeof(object), reuseCachedInstance);
		}
		
		public object[] GetExtensionObjects (string path, Type arrayElementType)
		{
			return GetExtensionObjects (path, arrayElementType, true);
		}
		
		public object[] GetExtensionObjects (string path, Type arrayElementType, bool reuseCachedInstance)
		{
			ExtensionNode node = GetExtensionNode (path);
			if (node == null)
				throw new InvalidOperationException ("Extension node not found in path: " + path);
			return node.GetChildObjects (arrayElementType, reuseCachedInstance);
		}
		
		public void AddExtensionNodeHandler (string path, ExtensionNodeEventHandler handler)
		{
			ExtensionNode node = GetExtensionNode (path);
			if (node == null)
				throw new InvalidOperationException ("Extension node not found in path: " + path);
			node.ExtensionNodeChanged += handler;
		}
		
		public void RemoveExtensionNodeHandler (string path, ExtensionNodeEventHandler handler)
		{
			ExtensionNode node = GetExtensionNode (path);
			if (node == null)
				throw new InvalidOperationException ("Extension node not found in path: " + path);
			node.ExtensionNodeChanged -= handler;
		}
		
		void OnConditionChanged (object s, EventArgs a)
		{
			ConditionType cond = (ConditionType) s;
			NotifyConditionChanged (cond);
		}
		
		internal void NotifyConditionChanged (ConditionType cond)
		{
			try {
				fireEvents = true;
				
				ConditionInfo info = (ConditionInfo) conditionTypes [cond.Id];
				if (info != null && info.BoundConditions != null) {
					Hashtable parentsToNotify = new Hashtable ();
					foreach (BaseCondition c in info.BoundConditions) {
						ArrayList nodeList = (ArrayList) conditionsToNodes [c];
						if (nodeList != null) {
							foreach (TreeNode node in nodeList)
								parentsToNotify [node.Parent] = null;
						}
					}
					foreach (TreeNode node in parentsToNotify.Keys) {
						if (node.NotifyChildrenChanged ())
							NotifyExtensionsChanged (new ExtensionEventArgs (node.GetPath ()));
					}
				}
			}
			finally {
				fireEvents = false;
			}

			// Notify child contexts
			lock (conditionTypes) {
				if (childContexts != null) {
					foreach (WeakReference wref in childContexts) {
						ExtensionContext ctx = wref.Target as ExtensionContext;
						if (ctx != null)
							ctx.NotifyConditionChanged (cond);
					}
				}
			}
		}
		

		internal void NotifyExtensionsChanged (ExtensionEventArgs args)
		{
			if (!fireEvents)
				return;

			if (ExtensionChanged != null)
				ExtensionChanged (this, args);
		}
		
		internal void NotifyAddinLoaded (RuntimeAddin ad)
		{
			tree.NotifyAddinLoaded (ad, true);

			lock (conditionTypes) {
				if (childContexts != null) {
					foreach (WeakReference wref in childContexts) {
						ExtensionContext ctx = wref.Target as ExtensionContext;
						if (ctx != null)
							ctx.NotifyAddinLoaded (ad);
					}
				}
			}
		}
		
		internal void CreateExtensionPoint (ExtensionPoint ep)
		{
			TreeNode node = tree.GetNode (ep.Path, true);
			if (node.ExtensionPoint == null) {
				node.ExtensionPoint = ep;
				node.ExtensionNodeSet = ep.NodeSet;
			}
		}
		
		internal void ActivateAddinExtensions (string id)
		{
			// Looks for loaded extension points which are extended by the provided
			// add-in, and adds the new nodes
			
			try {
				fireEvents = true;
				
				Addin addin = AddinManager.Registry.GetAddin (id);
				if (addin == null) {
					AddinManager.ReportError ("Required add-in not found", id, null, false);
					return;
				}
				// Take note that his add-in has been enabled at run-time
				// Needed because loaded add-in descriptions may not include this add-in. 
				RegisterRuntimeEnabledAddin (id);
				
				// Look for loaded extension points
				Hashtable eps = new Hashtable ();
				foreach (ModuleDescription mod in addin.Description.AllModules) {
					foreach (Extension ext in mod.Extensions) {
						ExtensionPoint ep = tree.FindLoadedExtensionPoint (ext.Path);
						if (ep != null && !eps.Contains (ep))
							eps.Add (ep, ep);
					}
				}
				
				// Add the new nodes
				ArrayList loadedNodes = new ArrayList ();
				foreach (ExtensionPoint ep in eps.Keys) {
					ExtensionLoadData data = GetAddinExtensions (id, ep);
					if (data != null) {
						foreach (Extension ext in data.Extensions) {
							TreeNode node = GetNode (ext.Path);
							if (node != null && node.ExtensionNodeSet != null) {
								if (node.ChildrenLoaded)
									LoadModuleExtensionNodes (ext, data.AddinId, node.ExtensionNodeSet, loadedNodes);
							}
							else
								AddinManager.ReportError ("Extension node not found or not extensible: " + ext.Path, id, null, false);
						}
						
						// Global extension change event. Other events are fired by LoadModuleExtensionNodes.
						NotifyExtensionsChanged (new ExtensionEventArgs (ep.Path));
					}
				}
				
				// Call the OnAddinLoaded method on nodes, if the add-in is already loaded
				foreach (TreeNode nod in loadedNodes)
					nod.ExtensionNode.OnAddinLoaded ();
			}
			finally {
				fireEvents = false;
			}
			// Do the same in child contexts
			
			lock (conditionTypes) {
				if (childContexts != null) {
					foreach (WeakReference wref in childContexts) {
						ExtensionContext ctx = wref.Target as ExtensionContext;
						if (ctx != null)
							ctx.ActivateAddinExtensions (id);
					}
				}
			}
		}
		
		internal void RemoveAddinExtensions (string id)
		{
			try {
				// Registers this add-in as disabled, so from now on extension from this
				// add-in will be ignored
				RegisterRuntimeDisabledAddin (id);
				
				fireEvents = true;

				// This method removes all extension nodes added by the add-in
				// Get all nodes created by the addin
				ArrayList list = new ArrayList ();
				tree.FindAddinNodes (id, list);
				
				// Remove each node and notify the change
				ArrayList paths = new ArrayList ();
				foreach (TreeNode node in list) {
					if (node.ExtensionNode == null) {
						// It's an extension point. Just remove it, no notifications are needed
						node.Remove ();
					}
					else {
						string path = node.Parent.GetPath ();
						if (!paths.Contains (path))
							paths.Add (path);
						node.ExtensionNode.OnAddinUnloaded ();
						node.Remove ();
					}
				}
				
				// Notify global extension point changes
				foreach (string path in paths)
					NotifyExtensionsChanged (new ExtensionEventArgs (path));
			} finally {
				fireEvents = false;
			}
		}
		
		void RegisterRuntimeDisabledAddin (string addinId)
		{
			if (runTimeDisabledAddins == null)
				runTimeDisabledAddins = new ArrayList ();
			if (!runTimeDisabledAddins.Contains (addinId))
				runTimeDisabledAddins.Add (addinId);
			
			if (runTimeEnabledAddins != null)
				runTimeEnabledAddins.Remove (addinId);
		}
		
		void RegisterRuntimeEnabledAddin (string addinId)
		{
			if (runTimeEnabledAddins == null)
				runTimeEnabledAddins = new ArrayList ();
			if (!runTimeEnabledAddins.Contains (addinId))
				runTimeEnabledAddins.Add (addinId);
			
			if (runTimeDisabledAddins != null)
				runTimeDisabledAddins.Remove (addinId);
		}
		
		internal ICollection GetAddinsForPath (string path, StringCollection col)
		{
			ArrayList newlist = null;
			
			// Always consider add-ins which have been enabled at runtime since
			// they may contain extensioin for this path.
			// Ignore addins disabled at run-time.
			
			if (runTimeEnabledAddins != null && runTimeEnabledAddins.Count > 0) {
				newlist = new ArrayList ();
				newlist.AddRange (col);
				foreach (string s in runTimeEnabledAddins)
					if (!newlist.Contains (s))
						newlist.Add (s);
			}
			
			if (runTimeDisabledAddins != null && runTimeDisabledAddins.Count > 0) {
				if (newlist == null) {
					newlist = new ArrayList ();
					newlist.AddRange (col);
				}
				foreach (string s in runTimeDisabledAddins)
					newlist.Remove (s);
			}
			
			return newlist != null ? (ICollection)newlist : (ICollection)col;
		}
		
		// Load the extension nodes at the specified path. If the path
		// contains extension nodes implemented in an add-in which is
		// not loaded, the add-in will be automatically loaded
		
		internal void LoadExtensions (string requestedExtensionPath)
		{
			TreeNode node = GetNode (requestedExtensionPath);
			if (node == null)
				throw new InvalidOperationException ("Extension point not defined: " + requestedExtensionPath);

			ExtensionPoint ep = node.ExtensionPoint;

			if (ep != null) {
			
				// Collect extensions to be loaded from add-ins. Before loading the extensions,
				// they must be sorted, that's why loading is split in two steps (collecting + loading).
				
				ArrayList loadData = new ArrayList ();
				
				foreach (string addin in GetAddinsForPath (ep.Path, ep.Addins)) {
					ExtensionLoadData ed = GetAddinExtensions (addin, ep);
					if (ed != null) {
						// Insert the addin data taking into account dependencies.
						// An add-in must be processed after all its dependencies.
						bool added = false;
						for (int n=0; n<loadData.Count; n++) {
							ExtensionLoadData other = (ExtensionLoadData) loadData [n];
							if (AddinManager.Registry.AddinDependsOn (other.AddinId, ed.AddinId)) {
								loadData.Insert (n, ed);
								added = true;
								break;
							}
						}
						if (!added)
							loadData.Add (ed);
					}
				}
				
				// Now load the extensions
				
				ArrayList loadedNodes = new ArrayList ();
				foreach (ExtensionLoadData data in loadData) {
					foreach (Extension ext in data.Extensions) {
						TreeNode cnode = GetNode (ext.Path);
						if (cnode != null && cnode.ExtensionNodeSet != null)
							LoadModuleExtensionNodes (ext, data.AddinId, cnode.ExtensionNodeSet, loadedNodes);
						else
							AddinManager.ReportError ("Extension node not found or not extensible: " + ext.Path, data.AddinId, null, false);
					}
				}
				// Call the OnAddinLoaded method on nodes, if the add-in is already loaded
				foreach (TreeNode nod in loadedNodes)
					nod.ExtensionNode.OnAddinLoaded ();

				NotifyExtensionsChanged (new ExtensionEventArgs (requestedExtensionPath));
			}
		}
		
		ExtensionLoadData GetAddinExtensions (string id, ExtensionPoint ep)
		{
			Addin pinfo = null;

			// Root add-ins are not returned by GetInstalledAddin.
			RuntimeAddin addin = AddinManager.SessionService.GetAddin (id);
			if (addin != null)
				pinfo = addin.Addin;
			else
				pinfo = AddinManager.Registry.GetAddin (id);
			
			if (pinfo == null) {
				AddinManager.ReportError ("Required add-in not found", id, null, false);
				return null;
			}
			if (!pinfo.Enabled)
				return null;
				
			// Loads extensions defined in each module
			
			ExtensionLoadData data = null;
			AddinDescription conf = pinfo.Description;
			GetAddinExtensions (conf.MainModule, id, ep, ref data);
			
			foreach (ModuleDescription module in conf.OptionalModules) {
				if (CheckOptionalAddinDependencies (conf, module))
					GetAddinExtensions (module, id, ep, ref data);
			}
			if (data != null)
				data.Extensions.Sort ();

			return data;
		}
		
		void GetAddinExtensions (ModuleDescription module, string addinId, ExtensionPoint ep, ref ExtensionLoadData data)
		{
			string basePath = ep.Path + "/";
			
			foreach (Extension extension in module.Extensions) {
				if (extension.Path == ep.Path || extension.Path.StartsWith (basePath)) {
					if (data == null) {
						data = new ExtensionLoadData ();
						data.AddinId = addinId;
						data.Extensions = new ArrayList ();
					}
					data.Extensions.Add (extension);
				}
			}
		}
		
		void LoadModuleExtensionNodes (Extension extension, string addinId, ExtensionNodeSet nset, ArrayList loadedNodes)
		{
			// Now load the extensions
			ArrayList addedNodes = new ArrayList ();
			tree.LoadExtension (addinId, extension, addedNodes);
			
			RuntimeAddin ad = AddinManager.SessionService.GetAddin (addinId);
			if (ad != null) {
				foreach (TreeNode nod in addedNodes) {
					// Don't call OnAddinLoaded here. Do it when the entire extension point has been loaded.
					if (nod.ExtensionNode != null)
						loadedNodes.Add (nod);
				}
			}
		}
		
		bool CheckOptionalAddinDependencies (AddinDescription conf, ModuleDescription module)
		{
			foreach (Dependency dep in module.Dependencies) {
				AddinDependency pdep = dep as AddinDependency;
				if (pdep != null) {
					Addin pinfo = AddinManager.Registry.GetAddin (Addin.GetFullId (conf.Namespace, pdep.AddinId, pdep.Version));
					if (pinfo == null || !pinfo.Enabled)
						return false;
				}
			}
			return true;
		}

		
		TreeNode GetNode (string path)
		{
			TreeNode node = tree.GetNode (path);
			if (node != null || parentContext == null)
				return node;
			
			TreeNode supNode = parentContext.tree.GetNode (path);
			if (supNode == null)
				return null;
			
			if (path.StartsWith ("/"))
				path = path.Substring (1);

			string[] parts = path.Split ('/');
			TreeNode srcNode = parentContext.tree;
			TreeNode dstNode = tree;

			foreach (string part in parts) {
				
				// Look for the node in the source tree
				
				int i = srcNode.Children.IndexOfNode (part);
				if (i != -1)
					srcNode = srcNode.Children [i];
				else
					return null;

				// Now get the node in the target tree
				
				int j = dstNode.Children.IndexOfNode (part);
				if (j != -1) {
					dstNode = dstNode.Children [j];
				}
				else {
					// Create if not found
					TreeNode newNode = new TreeNode (part);
					dstNode.AddChildNode (newNode);
					dstNode = newNode;
					
					// Copy extension data
					dstNode.ExtensionNodeSet = srcNode.ExtensionNodeSet;
					dstNode.ExtensionPoint = srcNode.ExtensionPoint;
					dstNode.Condition = srcNode.Condition;
					
					if (dstNode.Condition != null)
						RegisterNodeCondition (dstNode, dstNode.Condition);
				}
			}
			
			return dstNode;
		}
		
		internal bool FindExtensionPathByType (IProgressStatus monitor, Type type, string nodeName, out string path, out string pathNodeName)
		{
			return tree.FindExtensionPathByType (monitor, type, nodeName, out path, out pathNodeName);
		}
	}
	
	class ConditionInfo
	{
		public object CondType;
		public ArrayList BoundConditions;
	}

	
	
	public delegate void ExtensionEventHandler (object sender, ExtensionEventArgs args);
	public delegate void ExtensionNodeEventHandler (object sender, ExtensionNodeEventArgs args);
	
	public class ExtensionEventArgs: EventArgs
	{
		string path;
		
		internal ExtensionEventArgs ()
		{
		}
		
		public ExtensionEventArgs (string path)
		{
			this.path = path;
		}
		
		public virtual string Path {
			get { return path; }
		}
		
		public bool PathChanged (string pathToCheck)
		{
			if (pathToCheck.EndsWith ("/"))
				return path.StartsWith (pathToCheck);
			else
				return path.StartsWith (pathToCheck) && (pathToCheck.Length == path.Length || path [pathToCheck.Length] == '/');
		}
	}
	
	public class ExtensionNodeEventArgs: ExtensionEventArgs
	{
		ExtensionNode node;
		ExtensionChange change;
		
		public ExtensionNodeEventArgs (ExtensionChange change, ExtensionNode node)
		{
			this.node = node;
			this.change = change;
		}
		
		public override string Path {
			get { return node.Path; }
		}
		
		public ExtensionChange Change {
			get { return change; }
		}
		
		public ExtensionNode ExtensionNode {
			get { return node; }
		}
		
		public object ExtensionObject {
			get {
				InstanceExtensionNode tnode = node as InstanceExtensionNode;
				if (tnode == null)
					throw new InvalidOperationException ("Node is not an InstanceExtensionNode");
				return tnode.GetInstance (); 
			}
		}
	}
	
	public enum ExtensionChange
	{
		Add,
		Remove
	}

	
	internal class ExtensionLoadData
	{
		public string AddinId;
		public ArrayList Extensions;
	}
}
