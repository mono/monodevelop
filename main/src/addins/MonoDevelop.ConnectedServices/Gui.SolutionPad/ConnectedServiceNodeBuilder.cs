using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using System.IO;

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
			nodeInfo.Icon = Context.GetIcon ("md-service");
			nodeInfo.ClosedIcon = Context.GetIcon ("md-service");
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return !string.IsNullOrEmpty (GetServiceStateFolder (builder, dataObject));
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var serviceFolder = GetServiceStateFolder (treeBuilder, dataObject);
			if (!string.IsNullOrEmpty (serviceFolder)) {
				var project = (Project)treeBuilder.GetParentDataItem (typeof (Project), true);
				foreach (var file in Directory.GetFiles (serviceFolder)) {
					treeBuilder.AddChild (new Ide.Gui.Pads.ProjectPad.SystemFile (file, project));
				}
			}
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

		/// <summary>
		/// Gets the folder that the service stores state in, or null if we don't need to show it
		/// </summary>
		static string GetServiceStateFolder(ITreeBuilder treeBuilder, object dataObject)
		{
			if (treeBuilder.Options ["ShowAllFiles"]) {
				// add the files in the folder
				var serviceNode = (ConnectedServiceNode)dataObject;
				var serviceFolder = Path.GetDirectoryName (JsonFileConnectedService.GetConnectedServiceJsonFilePath (serviceNode.Project, serviceNode.Id, false));
				if (Directory.Exists (serviceFolder)) {
					return serviceFolder;
				}
			}

			return null;
		}
	}
}
