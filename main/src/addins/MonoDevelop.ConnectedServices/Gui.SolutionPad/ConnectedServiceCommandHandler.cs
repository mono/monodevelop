using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;

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
			if (service != null)
				ConnectedServices.OpenServicesTab (this.Project, service.Id).Ignore ();
		}

		[CommandUpdateHandler (Commands.RemoveService)]
		public void UpdateRemoveServiceCommand (CommandInfo info)
		{
			info.Visible = info.Enabled = CanDeleteItem ();
		}

		public override bool CanDeleteItem ()
		{
			return true;
		}

		[CommandHandler (Commands.RemoveService)]
		public override async void DeleteItem ()
		{
			var service = this.CurrentNode.DataItem as ConnectedServiceNode;
			try {
				await ConnectedServices.RemoveServiceFromProject (this.Project, service.Id);
			} catch (Exception ex) {
				LoggingService.LogError ("Error during service removal", ex);
			}
		}
	}
}
