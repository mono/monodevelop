using System;
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
			nodeInfo.Label = GettextCatalog.GetString (ConnectedServices.SolutionTreeNodeName);
			nodeInfo.Icon = Context.GetIcon (Stock.OpenReferenceFolder);
			nodeInfo.ClosedIcon = Context.GetIcon (Stock.ClosedReferenceFolder);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var folder = (ConnectedServiceFolderNode)dataObject;

			var connectedServices = folder.Project.GetConnectedServicesBinding ();
			return connectedServices != null && connectedServices.HasServices;
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			if (HasChildNodes(treeBuilder, dataObject)) {
				var connectedServices = ((ConnectedServiceFolderNode)dataObject).Project.GetConnectedServicesBinding ();
				foreach (var service in connectedServices.Services) {
					treeBuilder.AddChild (new ConnectedServiceNode (service.Id, service.DisplayName));
				}
			}
		}

		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			// we want to go after the project references node
			return (otherNode.DataItem is MonoDevelop.Projects.ProjectReferenceCollection) ? 1 : -1;
		}
	}
}
