
using System;
using MonoDevelop.Deployment;

namespace MonoDevelop.Deployment.Gui
{
	public partial class CommandDeployEditorWidget : Gtk.Bin
	{
		CommandPackageBuilder target;
		
		public CommandDeployEditorWidget (CommandPackageBuilder target)
		{
			this.target = target;
			this.Build();
			cmdEntry.Text = target.Command;
			argsEntry.Text = target.Arguments;
			checkExternal.Active = target.ExternalConsole;
			checkDisposeExternal.Active = target.CloseConsoleWhenDone;
			checkDisposeExternal.Sensitive = checkExternal.Active;
		}

		protected virtual void OnCmdEntryChanged(object sender, System.EventArgs e)
		{
			target.Command = cmdEntry.Text;
		}

		protected virtual void OnCheckExternalClicked(object sender, System.EventArgs e)
		{
			target.ExternalConsole = checkExternal.Active;
			checkDisposeExternal.Sensitive = checkExternal.Active;
		}

		protected virtual void OnCheckDisposeExternalClicked(object sender, System.EventArgs e)
		{
			target.CloseConsoleWhenDone = checkDisposeExternal.Active;
		}

		protected virtual void OnArgsEntryChanged(object sender, System.EventArgs e)
		{
			target.Arguments = argsEntry.Text;
		}
	}
}
