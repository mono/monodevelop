
using System;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Gui.Widgets;

namespace MonoDevelop.Commands
{
	public enum DebugCommands
	{
		DebugApplication,
		ToggleBreakpoint,
		StepOver,
		StepInto,
		StepOut,
		Pause,
		ClearAllBreakpoints
	}
	
	internal class DebugApplicationHandler: CommandHandler
	{
		protected override void Run ()
		{
			using (FileSelector fs = new FileSelector (GettextCatalog.GetString ("Application to Debug"))) {
				int response = fs.Run ();
				string name = fs.Filename;
				fs.Hide ();
				if (response == (int)Gtk.ResponseType.Ok)
					Runtime.ProjectService.DebugApplication (name);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = Runtime.DebuggingService != null &&
							Runtime.ProjectService.CurrentRunOperation.IsCompleted;
		}
	}
	
	internal class StepOverHandler : CommandHandler
	{
		protected override void Run ()
		{
			Runtime.DebuggingService.StepOver();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Runtime.DebuggingService == null)
				info.Visible = false;
			else
				info.Enabled = Runtime.DebuggingService.IsDebugging;
		}
	}

	internal class StepIntoHandler : CommandHandler
	{
		protected override void Run ()
		{
			Runtime.DebuggingService.StepInto();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Runtime.DebuggingService == null)
				info.Visible = false;
			else
				info.Enabled = Runtime.DebuggingService.IsDebugging;
		}
	}
	
	internal class StepOutHandler : CommandHandler
	{
		protected override void Run ()
		{
			Runtime.DebuggingService.StepOut ();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Runtime.DebuggingService == null)
				info.Visible = false;
			else
				info.Enabled = Runtime.DebuggingService.IsDebugging;
		}
	}
	
	internal class PauseDebugHandler : CommandHandler
	{
		protected override void Run ()
		{
			Runtime.DebuggingService.Pause ();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Runtime.DebuggingService == null)
				info.Visible = false;
			else
				info.Enabled = Runtime.DebuggingService.IsRunning;
		}
	}
	
	internal class ClearAllBreakpointsHandler: CommandHandler
	{
		protected override void Run ()
		{
			Runtime.DebuggingService.ClearAllBreakpoints ();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Runtime.DebuggingService == null)
				info.Visible = false;
		}
	}
}
