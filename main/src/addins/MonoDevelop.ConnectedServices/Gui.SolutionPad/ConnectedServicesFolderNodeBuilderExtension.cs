using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Extends the solution pad and defines the Connected Services node in the solution tree
	/// </summary>
	sealed class ConnectedServicesFolderNodeBuilderExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (DotNetProject).IsAssignableFrom (dataType);
		}

		public override Type CommandHandlerType {
			get { return typeof (ConnectedServicesFolderCommandHandler); }
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var connectedServices = ((DotNetProject)dataObject).GetConnectedServicesBinding ();
			if (connectedServices != null) {
				return connectedServices.HasSupportedServices;
			}

			return false;
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			if (HasChildNodes (treeBuilder, dataObject)) {
				treeBuilder.AddChild (new ConnectedServiceFolderNode ((DotNetProject)dataObject));
			}
		}
	}
}
