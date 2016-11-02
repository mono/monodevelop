using System;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Command handler for the Connected Services node.
	/// </summary>
	sealed class ConnectedServicesFolderCommandHandler : DotNetProjectNodeCommandHandler
	{
		[CommandUpdateHandler (Commands.OpenServicesGalleryFromServicesNode)]
		public void UpdateOpenServicesGalleryCommand (CommandInfo info)
		{
			if (this.CurrentNode != null) {
				info.Visible = info.Enabled = this.Project.GetConnectedServicesBinding ().HasSupportedServices;
			} else {
				info.Visible = info.Enabled = false;
			}
		}

		[CommandHandler (Commands.OpenServicesGalleryFromServicesNode)]
		public override void ActivateItem ()
		{
			var connectedServiceFolderNode = CurrentNode.DataItem as ConnectedServiceFolderNode;
			if (connectedServiceFolderNode != null)
				ConnectedServices.OpenServicesTab (this.Project);
		}
	}
}
