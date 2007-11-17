//
// ExtensionTree.cs
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
using System.Reflection;
using System.Xml;
using Mono.Addins.Description;

namespace Mono.Addins
{
	internal class ExtensionTree: TreeNode
	{
		int internalId;
		internal const string AutoIdPrefix = "__nid_";
		ExtensionContext context;
		
		public ExtensionTree (ExtensionContext context): base ("")
		{
			this.context = context;
		}
		
		public override ExtensionContext Context {
			get { return context; }
		}

		
		public void LoadExtension (string addin, Extension extension, ArrayList addedNodes)
		{
			TreeNode tnode = GetNode (extension.Path);
			if (tnode == null) {
				AddinManager.ReportError ("Can't load extensions for path '" + extension.Path + "'. Extension point not defined.", addin, null, false);
				return;
			}
			
			int curPos = tnode.ChildCount;
			LoadExtensionElement (tnode, addin, extension.ExtensionNodes, ref curPos, tnode.Condition, false, addedNodes);
		}

		void LoadExtensionElement (TreeNode tnode, string addin, ExtensionNodeDescriptionCollection extension, ref int curPos, BaseCondition parentCondition, bool inComplextCondition, ArrayList addedNodes)
		{
			foreach (ExtensionNodeDescription elem in extension) {
					
				if (inComplextCondition) {
					parentCondition = ReadComplexCondition (elem, parentCondition);
					inComplextCondition = false;
					continue;
				}

				if (elem.NodeName == "ComplexCondition") {
					LoadExtensionElement (tnode, addin, elem.ChildNodes, ref curPos, parentCondition, true, addedNodes);
					continue;
				}
					
				if (elem.NodeName == "Condition") {
					Condition cond = new Condition (elem, parentCondition);
					LoadExtensionElement (tnode, addin, elem.ChildNodes, ref curPos, cond, false, addedNodes);
					continue;
				}
					
				string after = elem.GetAttribute ("insertafter");
				if (after.Length > 0) {
					int i = tnode.Children.IndexOfNode (after);
					if (i != -1)
						curPos = i+1;
				}
				string before = elem.GetAttribute ("insertbefore");
				if (before.Length > 0) {
					int i = tnode.Children.IndexOfNode (before);
					if (i != -1)
						curPos = i;
				}
				
				// Find the type of the node in this extension
				ExtensionNodeType ntype = AddinManager.SessionService.FindType (tnode.ExtensionNodeSet, elem.NodeName, addin);
				
				if (ntype == null) {
					AddinManager.ReportError ("Node '" + elem.NodeName + "' not allowed in extension: " + tnode.GetPath (), addin, null, false);
					continue;
				}
				
				string id = elem.GetAttribute ("id");
				if (id.Length == 0)
					id = AutoIdPrefix + (++internalId);

				TreeNode cnode = new TreeNode (id);
				
				ExtensionNode enode = ReadNode (cnode, addin, ntype, elem);
				if (enode == null)
					continue;

				cnode.Condition = parentCondition;
				cnode.ExtensionNodeSet = ntype;
				tnode.InsertChildNode (curPos, cnode);
				addedNodes.Add (cnode);
				
				if (cnode.Condition != null)
					Context.RegisterNodeCondition (cnode, cnode.Condition);

				// Load children
				if (elem.ChildNodes.Count > 0) {
					int cp = 0;
					LoadExtensionElement (cnode, addin, elem.ChildNodes, ref cp, parentCondition, false, addedNodes);
				}
				
				curPos++;
			}
			if (Context.FireEvents)
				tnode.NotifyChildrenChanged ();
		}
		
		BaseCondition ReadComplexCondition (ExtensionNodeDescription elem, BaseCondition parentCondition)
		{
			if (elem.NodeName == "Or" || elem.NodeName == "And") {
				ArrayList conds = new ArrayList ();
				foreach (ExtensionNodeDescription celem in elem.ChildNodes) {
					conds.Add (ReadComplexCondition (celem, null));
				}
				if (elem.NodeName == "Or")
					return new OrCondition ((BaseCondition[]) conds.ToArray (typeof(BaseCondition)), parentCondition);
				else
					return new AndCondition ((BaseCondition[]) conds.ToArray (typeof(BaseCondition)), parentCondition);
			}
			if (elem.NodeName == "Condition") {
				return new Condition (elem, parentCondition);
			}
			AddinManager.ReportError ("Invalid complex condition element '" + elem.NodeName + "'.", null, null, false);
			return new NullCondition ();
		}
		
		public ExtensionNode ReadNode (TreeNode tnode, string addin, ExtensionNodeType ntype, ExtensionNodeDescription elem)
		{
			try {
				if (ntype.Type == null) {
					if (!InitializeNodeType (ntype))
						return null;
				}

				ExtensionNode node;
				node = Activator.CreateInstance (ntype.Type) as ExtensionNode;
				if (node == null) {
					AddinManager.ReportError ("Extension node type '" + ntype.Type + "' must be a subclass of ExtensionNode", addin, null, false);
					return null;
				}
				
				tnode.AttachExtensionNode (node);
				node.SetData (addin, ntype);
				node.Read (elem);
				return node;
			}
			catch (Exception ex) {
				AddinManager.ReportError ("Could not read extension node of type '" + ntype.Type + "' from extension path '" + tnode.GetPath() + "'", addin, ex, false);
				return null;
			}
		}
		
		bool InitializeNodeType (ExtensionNodeType ntype)
		{
			RuntimeAddin p = AddinManager.SessionService.GetAddin (ntype.AddinId);
			if (p == null) {
				if (!AddinManager.SessionService.IsAddinLoaded (ntype.AddinId)) {
					if (!AddinManager.SessionService.LoadAddin (null, ntype.AddinId, false))
						return false;
					p = AddinManager.SessionService.GetAddin (ntype.AddinId);
					if (p == null) {
						AddinManager.ReportError ("Add-in not found", ntype.AddinId, null, false);
						return false;
					}
				}
			}
			
			// If no type name is provided, use TypeExtensionNode by default
			if (ntype.TypeName == null || ntype.TypeName.Length == 0) {
				ntype.Type = typeof(TypeExtensionNode);
				return true;
			}
			
			ntype.Type = p.GetType (ntype.TypeName, false);
			if (ntype.Type == null) {
				AddinManager.ReportError ("Extension node type '" + ntype.TypeName + "' not found.", ntype.AddinId, null, false);
				return false;
			}
			
			Hashtable fields = new Hashtable ();
			
			// Check if the type has NodeAttribute attributes applied to fields.
			foreach (FieldInfo field in ntype.Type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				NodeAttributeAttribute at = (NodeAttributeAttribute) Attribute.GetCustomAttribute (field, typeof(NodeAttributeAttribute), true);
				if (at != null) {
					ExtensionNodeType.FieldData fdata = new ExtensionNodeType.FieldData ();
					fdata.Field = field;
					fdata.Required = at.Required;
					fdata.Localizable = at.Localizable;
					
					string name;
					if (at.Name != null && at.Name.Length > 0)
						name = at.Name;
					else
						name = field.Name;
					
					fields [name] = fdata;
				}
			}
			if (fields.Count > 0)
				ntype.Fields = fields;
				
			return true;
		}
	}
}
