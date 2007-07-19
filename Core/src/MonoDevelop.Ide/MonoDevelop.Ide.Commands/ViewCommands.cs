// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.Properties;
using MonoDevelop.Components.Commands;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.Ide.Commands
{
	public enum ViewCommands
	{
		ViewList,
		LayoutList,
		NewLayout,
		DeleteCurrentLayout,
		FullScreen,
		Open,
		OpenWithList,
		TreeDisplayOptionList,
		ResetTreeDisplayOptions,
		RefreshTree,
		CollapseAllTreeNodes,
		LayoutSelector,
		ShowNext,
		ShowPrevious
	}
	
	internal class FullScreenHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.FullScreen = !IdeApp.Workbench.FullScreen;
		}
	}
	
	internal class NewLayoutHandler: CommandHandler
	{
		protected override void Run ()
		{
			using (NewLayoutDialog dlg = new NewLayoutDialog ()) {
				dlg.Run ();
			}
		}
	}
	
	internal class DeleteCurrentLayoutHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (Services.MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to delete the active layout?"), "MonoDevelop")) {
				string clayout = IdeApp.Workbench.CurrentLayout;
				IdeApp.Workbench.CurrentLayout = "Default";
				IdeApp.Workbench.DeleteLayout (clayout);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workbench.CurrentLayout != "Default";
		}
	}
	
	internal class ViewListHandler: CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			foreach (Pad pad in IdeApp.Workbench.Pads) {
				CommandInfo cmd = new CommandInfo (pad.Title);
				cmd.Icon = pad.Icon;
				cmd.UseMarkup = true;
				info.Add (cmd, pad);
			}
		}
		
		protected override void Run (object ob)
		{
			Pad pad = (Pad) ob;
			pad.Visible = true;
			pad.BringToFront ();
		}
	}
	
	internal class LayoutListHandler: CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			string[] layouts = IdeApp.Workbench.Layouts;
			Array.Sort (layouts);
			foreach (string layout in layouts) {
				CommandInfo cmd = new CommandInfo (layout);
				cmd.Checked = (layout == IdeApp.Workbench.CurrentLayout);
				info.Add (cmd, layout);
			}
		}
		
		protected override void Run (object layout)
		{
			IdeApp.Workbench.CurrentLayout = (string) layout;
		}
	}
	
	internal class ShowNextHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ShowNext ();
		}
		
		protected override void Update (CommandInfo info)
		{
			Pad pad = IdeApp.Workbench.GetLocationListPad ();
			if (pad != null)
				info.Text = GettextCatalog.GetString ("Show Next ({0})", pad.Title);
			else
				info.Enabled = false;
		}
	}
	
	internal class ShowPreviousHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ShowPrevious ();
		}
		
		protected override void Update (CommandInfo info)
		{
			Pad pad = IdeApp.Workbench.GetLocationListPad ();
			if (pad != null)
				info.Text = GettextCatalog.GetString ("Show Previous ({0})", pad.Title);
			else
				info.Enabled = false;
		}
	}
}
