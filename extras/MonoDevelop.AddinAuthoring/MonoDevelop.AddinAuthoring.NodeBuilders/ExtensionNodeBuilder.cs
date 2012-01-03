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
using System.Collections.Generic;

namespace MonoDevelop.AddinAuthoring.NodeBuilders
{
	public class ExtensionNodeBuilder: ExtensionModelTypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(Extension); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ExtensionCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/AddinAuthoring/ContextMenu/ExtensionModelPad/Extension"; }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			Extension ep = (Extension) dataObject;
			return ep.Path;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Pixbuf icon, ref Pixbuf closedIcon)
		{
			Extension ext = (Extension) dataObject;
			label = GLib.Markup.EscapeText (Util.GetDisplayName (ext));
			icon = Context.GetIcon ("md-extension");
		}	
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			Extension en = (Extension) dataObject;
			AddinRegistry reg = GetRegistry (treeBuilder);
			if (reg == null)
				return;
			
			if (treeBuilder.Options ["ShowExistingNodes"]) {
				
				ExtensionNodeTree tree = new ExtensionNodeTree ();
				tree.Fill (reg, en);
				
				foreach (var node in tree.Nodes)
					treeBuilder.AddChild (node);
			}
			else {
				int order = 0;
				foreach (ExtensionNodeDescription child in en.ExtensionNodes)
					treeBuilder.AddChild (new ExtensionNodeInfo (child, true, order++));
			}
		}

		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (builder.Options ["ShowExistingNodes"])
				return true;
			
			Extension en = (Extension) dataObject;
			return en.ExtensionNodes.Count > 0;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (thisNode.DataItem is TreeViewItem)
				return -1;
			if (otherNode.DataItem is TreeViewItem)
				return 1;
			return base.CompareObjects (thisNode, otherNode);
		}
	}
	
	class ExtensionCommandHandler: NodeCommandHandler
	{
		protected virtual Extension GetExtension ()
		{
			return (Extension) CurrentNode.DataItem;
		}
		
		[CommandUpdateHandler (Commands.AddNode)]
		public void UpdateAddNodeBefore (CommandArrayInfo cinfo)
		{
			Extension en = GetExtension ();
			foreach (ExtensionNodeType ntype in en.GetAllowedNodeTypes ())
				cinfo.Add (GettextCatalog.GetString ("Add extension '{0}'", ntype.NodeName), ntype);
		}
		
		[CommandHandler (Commands.AddNode)]
		public void AddNodeBefore (object data)
		{
			DotNetProject p = (DotNetProject) CurrentNode.GetParentDataItem (typeof(Project), false);
			AddinData adata = p.GetAddinData ();
			
			Extension en = GetExtension ();
			ExtensionNodeType ntype = (ExtensionNodeType) data;
			
			ExtensionNodeDescription newNode = new ExtensionNodeDescription (ntype.NodeName);
			en.ExtensionNodes.Add (newNode);
			CurrentNode.Expanded = true;
			
			adata.SaveAddinManifest ();
			adata.NotifyChanged (false);
			
			DispatchService.GuiDispatch (delegate {
				ITreeNavigator nav = Tree.GetNodeAtObject (new ExtensionNodeInfo (newNode, false));
				if (nav != null)
					nav.Selected = true;
			});
		}
		
		public override bool CanDeleteMultipleItems ()
		{
			return true;
		}
		
		public override void DeleteMultipleItems ()
		{
			string msg = GettextCatalog.GetString ("The following extensions and all the nodes they contain will be deleted:") + "\n\n";
			foreach (var nav in CurrentNodes) {
				msg += Util.GetDisplayName ((Extension)nav.DataItem) + "\n";
			}
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the selected extensions?"), msg, AlertButton.Delete)) {
				foreach (var nav in CurrentNodes) {
					Extension ex = (Extension)nav.DataItem;
					ModuleDescription module = (ModuleDescription) ex.Parent;
					module.Extensions.Remove (ex);
				}
			}
			DotNetProject p = (DotNetProject) CurrentNode.GetParentDataItem (typeof(Project), false);
			AddinData adata = p.GetAddinData ();
			adata.SaveAddinManifest ();
			adata.NotifyChanged (false);
		}
	}
}
