// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.CodeDom.Compiler;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Gui;
using MonoDevelop.Services;

namespace MonoDevelop.Commands
{
	public enum WindowCommands
	{
		NextWindow,
		PrevWindow,
		OpenWindowList
	}
	
	internal class NextWindowHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow == null) {
				return;
			}
			int index = WorkbenchSingleton.Workbench.ViewContentCollection.IndexOf(WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent);
			WorkbenchSingleton.Workbench.ViewContentCollection[(index + 1) % WorkbenchSingleton.Workbench.ViewContentCollection.Count].WorkbenchWindow.SelectWindow();
		}
	}
	
	internal class PrevWindowHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow == null) {
				return;
			}
			int index = WorkbenchSingleton.Workbench.ViewContentCollection.IndexOf(WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent);
			WorkbenchSingleton.Workbench.ViewContentCollection[(index + WorkbenchSingleton.Workbench.ViewContentCollection.Count - 1) % WorkbenchSingleton.Workbench.ViewContentCollection.Count].WorkbenchWindow.SelectWindow();
		}
	}
	
	internal class OpenWindowListHandler: CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			int contentCount = WorkbenchSingleton.Workbench.ViewContentCollection.Count;
			if (contentCount == 0) return;
			
			for (int i = 0; i < contentCount; ++i) {
				IViewContent content = (IViewContent)WorkbenchSingleton.Workbench.ViewContentCollection[i];
				
				CommandInfo item = null;
				if (content.WorkbenchWindow.ShowNotification) {
					item = new CommandInfo ("<span foreground=\"blue\">" + content.WorkbenchWindow.Title + "</span>");
					item.UseMarkup = true;
				} else {
					item = new CommandInfo (content.WorkbenchWindow.Title);
				}
				
				item.Checked = (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow == content.WorkbenchWindow);
				item.Description = GettextCatalog.GetString ("Activate this window");
				
				if (i + 1 <= 9)
					item.AccelKey = "Alt|" + (i+1);
				
				info.Add (item, content.WorkbenchWindow);
			}
		}
		
		protected override void Run (object window)
		{
			((IWorkbenchWindow)window).SelectWindow();
		}
	}
}
