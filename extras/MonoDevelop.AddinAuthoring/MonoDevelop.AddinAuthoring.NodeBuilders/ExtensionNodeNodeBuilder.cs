// ExtensionPointsNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Components.Commands;
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Ide.Gui.Components;
using Gdk;
 
using System.Text;

namespace MonoDevelop.AddinAuthoring.NodeBuilders
{
	public class ExtensionNodeNodeBuilder: ExtensionModelTypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ExtensionNodeInfo); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ExtensionNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/AddinAuthoring/ContextMenu/ExtensionModelPad/ExtensionNode"; }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			ExtensionNodeInfo ep = (ExtensionNodeInfo) dataObject;
			return ep.Node.Id;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Pixbuf icon, ref Pixbuf closedIcon)
		{
			ExtensionNodeInfo ninfo = (ExtensionNodeInfo) dataObject;
			ExtensionNodeDescription node = ninfo.Node;
			
			label = GLib.Markup.EscapeText (node.NodeName);
			StringBuilder desc = new StringBuilder ();
			foreach (NodeAttribute at in node.Attributes) {
				if (desc.Length > 0)
					desc.Append ("  ");
				desc.Append (at.Name).Append ("=\"").Append (GLib.Markup.EscapeText (at.Value)).Append ('"');
			}
			if (desc.Length > 0)
				label += "(<i>" + desc + "</i>)";
			
			icon = Context.GetIcon ("md-extension-node");
			
			if (treeBuilder.Options ["ShowExistingNodes"] && !ninfo.CanModify) {
				Gdk.Pixbuf gicon = Context.GetComposedIcon (icon, "fade");
				if (gicon == null) {
					gicon = ImageService.MakeTransparent (icon, 0.5);
					Context.CacheComposedIcon (icon, "fade", gicon);
				}
				icon = gicon;
			}
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			ExtensionNodeInfo en = (ExtensionNodeInfo) dataObject;
			foreach (ExtensionNodeInfo child in en.Expand ())
				treeBuilder.AddChild (child);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			ExtensionNodeInfo en = (ExtensionNodeInfo) dataObject;
			return en.HasChildren;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			ExtensionNodeInfo n1 = thisNode.DataItem as ExtensionNodeInfo;
			ExtensionNodeInfo n2 = otherNode.DataItem as ExtensionNodeInfo;
			if (n1 != null && n2 != null)
				return n1.Order.CompareTo (n2.Order);
			else
				return base.CompareObjects (thisNode, otherNode);
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			ExtensionNodeInfo en = (ExtensionNodeInfo) dataObject;
			en.Changed += HandleNodeChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			ExtensionNodeInfo en = (ExtensionNodeInfo) dataObject;
			en.Changed -= HandleNodeChanged;
		}

		void HandleNodeChanged (object sender, EventArgs e)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (sender);
			if (builder != null)
				builder.UpdateAll ();
		}

	}
	
	class ExtensionNodeCommandHandler: NodeCommandHandler
	{
		public override DragOperation CanDragNode ()
		{
			ExtensionNodeInfo en = (ExtensionNodeInfo) CurrentNode.DataItem;
			if (en.CanModify)
				return DragOperation.Copy | DragOperation.Move;
			else
				return DragOperation.None;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation, DropPosition pos)
		{
			object parent1 = CurrentNode.GetParentDataItem (typeof(Extension), false);
			if (parent1 == null)
				parent1 = CurrentNode.GetParentDataItem (typeof(ExtensionPoint), false);
			
			ITreeNavigator nav = CurrentNode.Clone ();
			if (!nav.MoveToObject (dataObject))
				return false;
			object parent2 = nav.GetParentDataItem (parent1.GetType (), false);
			if (parent2 != parent1)
				return false;
			
			return true;
		}
		
		public override void OnMultipleNodeDrop (object[] dataObjects, DragOperation operation, DropPosition pos)
		{
			DotNetProject p = (DotNetProject) CurrentNode.GetParentDataItem (typeof(Project), false);
			AddinData adata = p.GetAddinData ();
			
			ExtensionNodeInfo en = (ExtensionNodeInfo) CurrentNode.DataItem;

			foreach (ExtensionNodeInfo newNode in dataObjects) {
				if (newNode.Node.Parent is ExtensionNodeDescription)
					((ExtensionNodeDescription)newNode.Node.Parent).ChildNodes.Remove (en.Node);
				else
					((Extension)newNode.Node.Parent).ExtensionNodes.Remove (newNode.Node);
				InsertNode (adata, en, pos, newNode.Node);
				
				// Add all other nodes after the first node
				en = newNode;
				pos = DropPosition.After;
			}
			
			adata.CachedAddinManifest.Save ();
			adata.NotifyChanged (false);
		}

		[CommandUpdateHandler (Commands.AddNodeAfter)]
		public void UpdateAddNodeAfter (CommandArrayInfo cinfo)
		{
			foreach (ExtensionNodeType ntype in GetAllowedChildTypes ()) {
				cinfo.Add (ntype.NodeName, ntype);
			}
		}
		
		[CommandHandler (Commands.AddNodeAfter)]
		public void AddNodeAfter (object data)
		{
			DotNetProject p = (DotNetProject) CurrentNode.GetParentDataItem (typeof(Project), false);
			AddinData adata = p.GetAddinData ();
			
			ExtensionNodeInfo en = (ExtensionNodeInfo) CurrentNode.DataItem;
			ExtensionNodeType ntype = (ExtensionNodeType) data;
			ExtensionNodeDescription newNode = new ExtensionNodeDescription (ntype.NodeName);
			InsertNode (adata, en, DropPosition.After, newNode);
			
			adata.CachedAddinManifest.Save ();
			adata.NotifyChanged (false);
		}

		
		[CommandUpdateHandler (Commands.AddNodeBefore)]
		public void UpdateAddNodeBefore (CommandArrayInfo cinfo)
		{
			foreach (ExtensionNodeType ntype in GetAllowedChildTypes ()) {
				cinfo.Add (ntype.NodeName, ntype);
			}
		}
		
		[CommandHandler (Commands.AddNodeBefore)]
		public void AddNodeBefore (object data)
		{
			DotNetProject p = (DotNetProject) CurrentNode.GetParentDataItem (typeof(Project), false);
			AddinData adata = p.GetAddinData ();
			
			ExtensionNodeInfo en = (ExtensionNodeInfo) CurrentNode.DataItem;
			ExtensionNodeType ntype = (ExtensionNodeType) data;
			ExtensionNodeDescription newNode = new ExtensionNodeDescription (ntype.NodeName);
			InsertNode (adata, en, DropPosition.Before, newNode);
			
			adata.CachedAddinManifest.Save ();
			adata.NotifyChanged (false);
		}
		
		ExtensionNodeDescription InsertNode (AddinData adata, ExtensionNodeInfo refNode, DropPosition pos, ExtensionNodeDescription newNode)
		{
			ExtensionNodeDescriptionCollection nodes = null;
			newNode.InsertBefore = "";
			newNode.InsertAfter = "";
			
			if (refNode.CanModify) {
				if (pos == DropPosition.Into)
					nodes = refNode.Node.ChildNodes;
				else if (refNode.Node.Parent is ExtensionNodeDescription)
					nodes = ((ExtensionNodeDescription)refNode.Node.Parent).ChildNodes;
			} else {
				if (pos == DropPosition.After)
				    newNode.InsertAfter = refNode.Node.Id;
				else if (pos == DropPosition.Before)
					newNode.InsertBefore = refNode.Node.Id;
			}
			if (nodes == null) {
				string path = refNode.Node.GetParentPath ();
				if (pos == DropPosition.Into)
					path += "/" + refNode.Node.Id;
				Extension ext = adata.CachedAddinManifest.MainModule.GetExtension (path);
				nodes = ext.ExtensionNodes;
			}

			for (int n = 0; n < nodes.Count; n++) {
				ExtensionNodeDescription node = nodes [n];
				if (node == refNode.Node) {
					if (pos == DropPosition.After) n++;
					nodes.Insert (n, newNode);
					return newNode;
				}
			}
			nodes.Add (newNode);
			return newNode;
		}
		
		ExtensionNodeTypeCollection GetAllowedChildTypes ()
		{
			ExtensionNodeInfo en = (ExtensionNodeInfo) CurrentNode.DataItem;
			object parent = en.Node.Parent;
			
			Extension ext = parent as Extension;
			if (ext != null)
				return ext.GetAllowedNodeTypes ();
			else {
				ExtensionNodeDescription node = (ExtensionNodeDescription) parent;
				if (node != null) {
					ExtensionNodeType tn = node.GetNodeType ();
					if (tn != null)
						return tn.GetAllowedNodeTypes ();
				}
			}
			return new ExtensionNodeTypeCollection ();
		}		
		
		public override bool CanDeleteMultipleItems ()
		{
			foreach (var nav in CurrentNodes) {
				ExtensionNodeInfo en = (ExtensionNodeInfo) nav.DataItem;
				if (!en.CanModify)
					return false;
			}
			return true;
		}
		
		public override void DeleteMultipleItems ()
		{
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the selected nodes?"), AlertButton.Delete)) {
				foreach (var nav in CurrentNodes) {
					ExtensionNodeInfo en = (ExtensionNodeInfo) nav.DataItem;
					if (en.Node.Parent is Extension)
						((Extension)en.Node.Parent).ExtensionNodes.Remove (en.Node);
					else if (en.Node.Parent is ExtensionNodeDescription)
						((ExtensionNodeDescription)en.Node.Parent).ChildNodes.Remove (en.Node);
				}
			}
			DotNetProject p = (DotNetProject) CurrentNode.GetParentDataItem (typeof(Project), false);
			AddinData adata = p.GetAddinData ();
			adata.SaveAddinManifest ();
			adata.NotifyChanged (false);
		}
	}
}
