using System;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Command handler for a Connected Service node.
	/// </summary>
	sealed class ConnectedServiceCommandHandler : DotNetProjectNodeCommandHandler
	{
		[CommandUpdateHandler (Commands.OpenServiceDetails)]
		public void UpdateOpenServiceDetailsCommand (CommandInfo info)
		{
			info.Visible = info.Enabled = true;
		}

		[CommandHandler (Commands.OpenServiceDetails)]
		public override void ActivateItem ()
		{
			var service = this.CurrentNode.DataItem as ConnectedServiceNode;
			ConnectedServices.OpenServicesTab (this.Project, service.Id);
		}

		[CommandUpdateHandler (Commands.RemoveService)]
		public void UpdateRemoveServiceCommand (CommandInfo info)
		{
			info.Visible = info.Enabled = true;
		}

		[CommandHandler (Commands.RemoveService)]
		public void RemoveService()
		{
			var service = this.CurrentNode.DataItem as ConnectedServiceNode;
			ConnectedServices.RemoveServiceFromProject (this.Project, service.Id);
		}
	}
}
