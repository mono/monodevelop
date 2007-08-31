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
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Commands
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
			if (IdeApp.Workbench.ActiveDocument == null) {
				return;
			}
			int index = IdeApp.Workbench.Documents.IndexOf (IdeApp.Workbench.ActiveDocument);
			IdeApp.Workbench.Documents [(index + 1) % IdeApp.Workbench.Documents.Count].Select ();
		}
	}
	
	internal class PrevWindowHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				return;
			}
			int index = IdeApp.Workbench.Documents.IndexOf (IdeApp.Workbench.ActiveDocument);
			IdeApp.Workbench.Documents [(index + IdeApp.Workbench.Documents.Count - 1) % IdeApp.Workbench.Documents.Count].Select ();
		}
	}
	
	internal class OpenWindowListHandler: CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			int contentCount = IdeApp.Workbench.Documents.Count;
			if (contentCount == 0) return;
			
			for (int i = 0; i < contentCount; ++i) {
				Document doc = IdeApp.Workbench.Documents [i];
				
				string escapedWindowTitle = doc.Window.Title.Replace("_", "__");
				CommandInfo item = null;
				if (doc.Window.ShowNotification) {
					item = new CommandInfo ("<span foreground=\"blue\">" + escapedWindowTitle + "</span>");
					item.UseMarkup = true;
				} else {
					item = new CommandInfo (escapedWindowTitle);
				}
				
				item.Checked = (IdeApp.Workbench.ActiveDocument == doc);
				item.Description = GettextCatalog.GetString ("Activate this window");
				
				if (i + 1 <= 9)
					item.AccelKey = "Alt|" + (i+1);
				
				info.Add (item, doc);
			}
		}
		
		protected override void Run (object doc)
		{
			((Document)doc).Select ();
		}
	}
}
