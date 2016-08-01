using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Command handler for the Connected Services node.
	/// </summary>
	sealed class ConnectedServicesFolderCommandHandler : NodeCommandHandler
	{
		[CommandUpdateHandler (Commands.AddService)]
		public void UpdateAddCommand (CommandInfo info)
		{
			info.Visible = info.Enabled = true;
		}

		[CommandHandler (Commands.AddService)]
		public void AddService ()
		{
			//var project = (DotNetProject)CurrentNode.GetParentDataItem (typeof (DotNetProject), true);
			// TODO: open up the tab that lists the available services that can be applied to the project.
		}
	}
}
