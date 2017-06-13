using System;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Builds ConnectedServices folder node and adds additional nodes for each enabled service
	/// </summary>
	sealed class ConnectedServicesFolderNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { 
				return typeof (ConnectedServiceFolderNode); 
			}
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "ConnectedServicesFolder";
		}

		/// <summary>
		/// Gets the extension point that defines the context menu commands for this node
		/// </summary>
		public override string ContextMenuAddinPath {
			get {
				return "/MonoDevelop/ConnectedServices/ContextMenu/ProjectPad/ConnectedServicesFolder";
			}
		}

		public override Type CommandHandlerType {
			get {
				return typeof (ConnectedServicesFolderCommandHandler);
			}
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			nodeInfo.Label = ConnectedServices.SolutionTreeNodeName;
			nodeInfo.Icon = Context.GetIcon ("md-folder-services");
			nodeInfo.ClosedIcon = Context.GetIcon ("md-folder-services");
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var folder = (ConnectedServiceFolderNode)dataObject;

			var connectedServices = folder.Project.GetConnectedServicesBinding ();
			return connectedServices != null && connectedServices.HasAddedServices;
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			if (HasChildNodes(treeBuilder, dataObject)) {
				foreach (var node in ((ConnectedServiceFolderNode)dataObject).GetChildNodes ()) {
					treeBuilder.AddChild (node);
				}
			}
		}

		public override int GetSortIndex (ITreeNavigator node)
		{
			return -1500;
		}

		public override void OnNodeAdded (object dataObject)
		{
			var services = (ConnectedServiceFolderNode)dataObject;
			services.ServicesChanged += ServicesChanged;
			services.SelectRequested += ServicesSelectRequested;
			services.ExpandRequested += ServicesExpandRequested;

			services.Project.GetConnectedServicesBinding ().ServicesNode = services;

			base.OnNodeAdded (dataObject);
		}

		public override void OnNodeRemoved (object dataObject)
		{
			var services = (ConnectedServiceFolderNode)dataObject;
			services.ServicesChanged -= ServicesChanged;
			services.SelectRequested -= ServicesSelectRequested;
			services.ExpandRequested -= ServicesExpandRequested;

			var binding = services.Project?.GetConnectedServicesBinding ();
			if (binding != null)
				binding.ServicesNode = null;

			base.OnNodeRemoved (dataObject);
		}

		/// <summary>
		/// Handles the services that have been added to the project by updating the services node
		/// </summary>
		void ServicesChanged (object sender, ServicesChangedEventArgs e)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (sender);
			if (builder != null) {
				builder.UpdateAll ();
				builder.Expanded = true;
			}
		}

		/// <summary>
		/// Selects the tree node on request
		/// </summary>
		void ServicesSelectRequested (object sender, EventArgs e)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (sender);
			if (builder != null)
				builder.Selected = true;
		}

		void ServicesExpandRequested (object sender, EventArgs e)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (sender);
			if (builder != null)
				builder.Expanded = true;
		}
	}
}
