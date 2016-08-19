using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Builds the nodes that are added for each enabled connected service
	/// </summary>
	sealed class ConnectedServiceNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof (ConnectedServiceNode); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "ConnectedService";
		}

		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/ConnectedServices/ContextMenu/ProjectPad/ConnectedService"; }
		}

		public override Type CommandHandlerType {
			get { return typeof (ConnectedServiceCommandHandler); }
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var service = (ConnectedServiceNode)dataObject;
			nodeInfo.Label = service.DisplayName;
			nodeInfo.Icon = Context.GetIcon ("md-connected-service");
			nodeInfo.ClosedIcon = Context.GetIcon ("md-connected-service");
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;

			// TODO: return true if we are showing all files -> we want to show the .json file
		}

		public override void OnNodeAdded (object dataObject)
		{
			var services = (ConnectedServiceNode)dataObject;
			services.SelectRequested += SelectServiceRequested;
			base.OnNodeAdded (dataObject);
		}

		public override void OnNodeRemoved (object dataObject)
		{
			var services = (ConnectedServiceNode)dataObject;
			services.SelectRequested -= SelectServiceRequested;
			base.OnNodeRemoved (dataObject);
		}

		void SelectServiceRequested (object sender, EventArgs e)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (sender);
			if (builder != null)
				builder.Selected = true;
		}
	}
}
