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
	public class ExtensionPointNodeBuilder: ExtensionModelTypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ExtensionPoint); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ExtensionPointCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/AddinAuthoring/ContextMenu/ExtensionModelPad/ExtensionPoint"; }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			ExtensionPoint ep = (ExtensionPoint) dataObject;
			return ep.Path;
		}
		
		public override object GetParentObject (object dataObject)
		{
			ExtensionPoint ep = (ExtensionPoint) dataObject;
			return ep.ParentAddinDescription;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Pixbuf icon, ref Pixbuf closedIcon)
		{
			ExtensionPoint ep = (ExtensionPoint) dataObject;
			label = GLib.Markup.EscapeText (!string.IsNullOrEmpty (ep.Name) ? ep.Name : ep.Path);
			icon = Context.GetIcon ("md-extension-point");
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			ExtensionPoint ep = (ExtensionPoint) dataObject;
			
			if (treeBuilder.Options ["ShowExistingNodes"]) {
				AddinRegistry reg = GetRegistry (treeBuilder);
				if (reg == null)
					return;
				ExtensionNodeTree tree = new ExtensionNodeTree ();
				tree.Fill (reg, ep);
				foreach (var node in tree.Nodes)
					treeBuilder.AddChild (node);
			}
			else {
				int order = 0;
				foreach (Extension ext in ep.ParentAddinDescription.MainModule.Extensions) {
					if (ext.Path == ep.Path) {
						foreach (ExtensionNodeDescription node in ext.ExtensionNodes)
							treeBuilder.AddChild (new ExtensionNodeInfo (node, true, order++));
					}
				}
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (builder.Options ["ShowExistingNodes"])
				return true;
			else {
				ExtensionPoint ep = (ExtensionPoint) dataObject;
				foreach (Extension ext in ep.ParentAddinDescription.MainModule.Extensions) {
					if (ext.Path == ep.Path) {
						if (ext.ExtensionNodes.Count > 0)
							return true;
					}
				}
				return false;
			}
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
	
	class ExtensionPointCommandHandler: ExtensionCommandHandler
	{
		public override void ActivateItem ()
		{
			DotNetProject project = (DotNetProject) CurrentNode.GetParentDataItem (typeof(DotNetProject), true);
			if (project == null)
				return;
			AddinData data = project.GetAddinData ();
			if (data == null)
				return;
			
			ExtensionPoint ep = (ExtensionPoint) CurrentNode.DataItem;
			ExtensionPoint epc = new ExtensionPoint ();
			epc.CopyFrom (ep);
			NewExtensionPointDialog epdlg = new NewExtensionPointDialog (project, data.AddinRegistry, data.CachedAddinManifest, epc);
			if (epdlg.Run () == (int) Gtk.ResponseType.Ok) {
				ep.CopyFrom (epc);
				data.CachedAddinManifest.Save ();
			}
			epdlg.Destroy ();
		}
		
		protected override Extension GetExtension ()
		{
			ExtensionPoint ep = (ExtensionPoint) CurrentNode.DataItem;
			return ep.ParentAddinDescription.MainModule.GetExtension (ep.Path);
		}
		
		public override bool CanDeleteMultipleItems ()
		{
			return true;
		}
		
		public override void DeleteMultipleItems ()
		{
			string msg = GettextCatalog.GetString ("The following extension points and all the nodes they contain will be deleted:") + "\n\n";
			foreach (var nav in CurrentNodes) {
				msg += Util.GetDisplayName ((ExtensionPoint)nav.DataItem) + "\n";
			}
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the selected extension points?"), msg, AlertButton.Delete)) {
				foreach (var nav in CurrentNodes) {
					ExtensionPoint ep = (ExtensionPoint)nav.DataItem;
					ep.ParentAddinDescription.ExtensionPoints.Remove (ep);
					foreach (ModuleDescription module in ep.ParentAddinDescription.AllModules) {
						List<Extension> toDelete = new List<Extension> ();
						foreach (Extension ext in module.Extensions) {
							if (ext.Path == ep.Path || ext.Path.StartsWith (ep.Path + "/"))
								toDelete.Add (ext);
						}
						foreach (Extension ext in toDelete)
							module.Extensions.Remove (ext);
					}
				}
			}
			DotNetProject p = (DotNetProject) CurrentNode.GetParentDataItem (typeof(Project), false);
			AddinData adata = p.GetAddinData ();
			adata.SaveAddinManifest ();
			adata.NotifyChanged (false);
		}

	}
}
