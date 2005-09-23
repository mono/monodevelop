// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.CodeDom.Compiler;

using MonoDevelop.Services;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;

using MonoDevelop.Gui;
using MonoDevelop.Gui.Dialogs;

namespace MonoDevelop.Commands
{
	public enum ViewCommands
	{
		ViewList,
		LayoutList,
		NewLayout,
		DeleteCurrentLayout,
		FullScreen,
		Open,
		TreeDisplayOptionList,
		ResetTreeDisplayOptions
	}
	
	internal class FullScreenHandler: CommandHandler
	{
		protected override void Run ()
		{
			((DefaultWorkbench)WorkbenchSingleton.Workbench).FullScreen = !((DefaultWorkbench)WorkbenchSingleton.Workbench).FullScreen;
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
			if (Runtime.MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to delete the active layout?"), "MonoDevelop")) {
				string clayout = WorkbenchSingleton.Workbench.WorkbenchLayout.CurrentLayout;
				WorkbenchSingleton.Workbench.WorkbenchLayout.CurrentLayout = "Default";
				WorkbenchSingleton.Workbench.WorkbenchLayout.DeleteLayout (clayout);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = WorkbenchSingleton.Workbench.WorkbenchLayout != null && WorkbenchSingleton.Workbench.WorkbenchLayout.CurrentLayout != "Default";
		}
	}
	
	internal class ViewListHandler: CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			IWorkbench wb = WorkbenchSingleton.Workbench;
			if (wb.WorkbenchLayout != null) {
				PadContentCollection pads = wb.WorkbenchLayout.PadContentCollection;
				foreach (IPadContent padContent in pads) {
					CommandInfo cmd = new CommandInfo (padContent.Title);
					cmd.UseMarkup = true;
					cmd.Checked = WorkbenchSingleton.Workbench.WorkbenchLayout.IsVisible (padContent);
					info.Add (cmd, padContent);
				}
			}
		}
		
		protected override void Run (object ob)
		{
			IPadContent padContent = (IPadContent) ob;
			if (WorkbenchSingleton.Workbench.WorkbenchLayout.IsVisible (padContent)) {
				WorkbenchSingleton.Workbench.WorkbenchLayout.HidePad (padContent);
			} else {
				WorkbenchSingleton.Workbench.WorkbenchLayout.ShowPad (padContent);
			}
		}
	}
	
	internal class LayoutListHandler: CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			IWorkbench wb = WorkbenchSingleton.Workbench;
			if (wb.WorkbenchLayout != null) {
				string[] layouts = wb.WorkbenchLayout.Layouts;
				Array.Sort (layouts);
				foreach (string layout in layouts) {
					CommandInfo cmd = new CommandInfo (layout);
					cmd.Checked = (layout == wb.WorkbenchLayout.CurrentLayout);
					info.Add (cmd, layout);
				}
			}
		}
		
		protected override void Run (object layout)
		{
			WorkbenchSingleton.Workbench.WorkbenchLayout.CurrentLayout = (string) layout;
		}
	}
}
