
using System;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Components;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	
	
	public partial class CustomCommandWidget : Gtk.Bin
	{
		CustomCommand cmd;
		CombineEntry entry;
		bool updating;
		
		public CustomCommandWidget (CombineEntry entry, CustomCommand cmd)
		{
			this.Build();
			this.cmd = cmd;
			if (cmd != null) {
				updating = true;
				comboType.RemoveText (0);
				updating = false;
			}
			this.entry = entry;
			UpdateControls ();
			this.WidgetFlags |= Gtk.WidgetFlags.NoShowAll;
		}
		
		public CustomCommand CustomCommand {
			get { return cmd; }
		}
		
		void UpdateControls ()
		{
			updating = true;
			
			boxData.Visible = tableData.Visible = buttonRemove.Visible = (cmd != null);
				
			if (cmd == null) {
				comboType.Active = 0;
			}
			else {
				Array array = Enum.GetValues (typeof (CustomCommandType));
				comboType.Active = Array.IndexOf (array, cmd.Type);
				labelName.Visible = entryName.Visible = (cmd.Type == CustomCommandType.Custom);
				entryName.Text = cmd.Name;
				entryCommand.Text = cmd.Command;
				checkExternalCons.Active = cmd.ExternalConsole;
				checkPauseCons.Active = cmd.PauseExternalConsole;
				checkPauseCons.Sensitive = cmd.ExternalConsole;
			}
			updating = false;
		}

		protected virtual void OnButtonBrowseClicked(object sender, System.EventArgs e)
		{
			FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Select File"));
			try {
				fdiag.SetCurrentFolder (entry.BaseDirectory);
				fdiag.SelectMultiple = false;
				if (fdiag.Run () == (int) Gtk.ResponseType.Ok) {
					if (System.IO.Path.IsPathRooted (fdiag.Filename))
						entryCommand.Text = Runtime.FileService.AbsoluteToRelativePath (entry.BaseDirectory, fdiag.Filename);
					else
						entryCommand.Text = fdiag.Filename;
				}
				fdiag.Hide ();
			} finally {
				fdiag.Destroy ();
			}
		}

		protected virtual void OnEntryCommandChanged(object sender, System.EventArgs e)
		{
			if (!updating)
				cmd.Command = entryCommand.Text;
		}

		protected virtual void OnEntryNameChanged(object sender, System.EventArgs e)
		{
			if (!updating)
				cmd.Name = entryName.Text;
		}

		protected virtual void OnComboTypeChanged(object sender, System.EventArgs e)
		{
			if (!updating) {
				if (cmd == null) {
					if (comboType.Active != 0) {
						// Selected a command type. Create the command now
						cmd = new CustomCommand ();
						cmd.Type = (CustomCommandType) (comboType.Active - 1);
						updating = true;
						comboType.RemoveText (0);
						updating = false;
						if (CommandCreated != null)
							CommandCreated (this, EventArgs.Empty);
					}
				} else
					cmd.Type = (CustomCommandType) (comboType.Active);
				UpdateControls ();
			}
		}

		protected virtual void OnCheckPauseConsClicked(object sender, System.EventArgs e)
		{
			if (!updating)
				cmd.PauseExternalConsole = checkPauseCons.Active;
		}

		protected virtual void OnCheckExternalConsClicked(object sender, System.EventArgs e)
		{
			if (!updating) {
				cmd.ExternalConsole = checkExternalCons.Active;
				UpdateControls ();
			}
		}

		protected virtual void OnButtonRemoveClicked(object sender, System.EventArgs e)
		{
			if (CommandRemoved != null)
				CommandRemoved (this, EventArgs.Empty);
		}

		public event EventHandler CommandCreated;
		public event EventHandler CommandRemoved;
	}
}
