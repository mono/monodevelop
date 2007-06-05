
using System;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Core.Properties;
using Mono.Addins;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Commands
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
// TODO: Project Conversion
//				if (response == (int)Gtk.ResponseType.Ok)
//				IdeApp.ProjectOperations.DebugApplication (name);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = Services.DebuggingService != null && ProjectService.CurrentRunOperation.IsCompleted;
		}
	}
	
	internal class StepOverHandler : CommandHandler
	{
		protected override void Run ()
		{
			Services.DebuggingService.StepOver();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Services.DebuggingService == null)
				info.Visible = false;
			else
				info.Enabled = Services.DebuggingService.IsDebugging;
		}
	}

	internal class StepIntoHandler : CommandHandler
	{
		protected override void Run ()
		{
			Services.DebuggingService.StepInto();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Services.DebuggingService == null)
				info.Visible = false;
			else
				info.Enabled = Services.DebuggingService.IsDebugging;
		}
	}
	
	internal class StepOutHandler : CommandHandler
	{
		protected override void Run ()
		{
			Services.DebuggingService.StepOut ();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Services.DebuggingService == null)
				info.Visible = false;
			else
				info.Enabled = Services.DebuggingService.IsDebugging;
		}
	}
	
	internal class PauseDebugHandler : CommandHandler
	{
		protected override void Run ()
		{
			Services.DebuggingService.Pause ();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Services.DebuggingService == null)
				info.Visible = false;
			else
				info.Enabled = Services.DebuggingService.IsRunning;
		}
	}
	
	internal class ClearAllBreakpointsHandler: CommandHandler
	{
		protected override void Run ()
		{
			Services.DebuggingService.ClearAllBreakpoints ();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Services.DebuggingService == null)
				info.Visible = false;
		}
	}
}
