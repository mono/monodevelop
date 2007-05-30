
using System;
using MonoDevelop.Projects;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal partial class CustomCommandPanelWidget : Gtk.Bin
	{
		CustomCommandCollection commands;
		CustomCommandWidget lastSlot;
		CombineEntry entry;
		
		public CustomCommandPanelWidget (CombineEntry entry, CustomCommandCollection commands)
		{
			this.Build();
			this.entry = entry;
			this.commands = commands;
			
			foreach (CustomCommand cmd in commands) {
				AddCommandSlot (cmd);
			}
			// Add an empty slot to allow adding more commands.
			AddCommandSlot (null);
		}
		
		void AddCommandSlot (CustomCommand cmd)
		{
			CustomCommandWidget widget = new CustomCommandWidget (entry, cmd);
			vboxCommands.PackStart (widget, false, false, 0);
			widget.CommandCreated += OnCommandCreated;
			widget.CommandRemoved += OnCommandRemoved;
			widget.Show ();
			lastSlot = widget;
		}
		
		void OnCommandCreated (object s, EventArgs args)
		{
			CustomCommandWidget widget = (CustomCommandWidget) s;
			commands.Add (widget.CustomCommand);
			
			// Add an empty slot to allow adding more commands.
			AddCommandSlot (null);
		}
		
		void OnCommandRemoved (object s, EventArgs args)
		{
			CustomCommandWidget widget = (CustomCommandWidget) s;
			commands.Remove (widget.CustomCommand);
			if (lastSlot != widget)
				vboxCommands.Remove (widget);
		}
	}
}
