using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.GettingStarted
{
	public class GettingStartedNodeCommandHandler : NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			base.ActivateItem ();
			var gettingStartedNode = CurrentNode.DataItem as GettingStartedNode;
			if (gettingStartedNode != null) {
				GettingStarted.ShowGettingStarted (gettingStartedNode.Project);
			}
		}

		public override bool CanDeleteItem ()
		{
			return true;
		}

		[CommandUpdateHandler (Ide.Commands.EditCommands.Delete)]
		public void UpdateDelete (CommandInfo info)
		{
			info.Enabled = true;
			info.Text = GettextCatalog.GetString ("Remove");
		}

		[CommandHandler (Ide.Commands.EditCommands.Delete)]
		public override void DeleteItem ()
		{
			var gettingStartedNode = (GettingStartedNode)CurrentNode.DataItem;
			gettingStartedNode.Project.UserProperties.SetValue ("HideGettingStarted", true);
			gettingStartedNode.Remove ();
		}
	}
}

