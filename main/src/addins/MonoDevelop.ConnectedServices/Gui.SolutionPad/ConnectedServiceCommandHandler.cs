using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Command handler for a Connected Service node.
	/// </summary>
	sealed class ConnectedServiceCommandHandler : NodeCommandHandler
	{
		[CommandUpdateHandler (Commands.OpenServiceDetails)]
		public void UpdateOpenServiceDetailsommand (CommandInfo info)
		{
			info.Visible = info.Enabled = true;
		}

		[CommandHandler (Commands.OpenServiceDetails)]
		public void OpenServiceDetails ()
		{
			//var project = (DotNetProject)CurrentNode.GetParentDataItem (typeof (DotNetProject), true);
			// TODO: open the tab that displays the service details, or navigate to the details view of the tab if it is already open.
		}
	}
}
