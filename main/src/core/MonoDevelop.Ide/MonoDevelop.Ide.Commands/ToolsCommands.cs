//  ToolsCommands.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using Mono.Addins;
using MonoDevelop.Components.Commands;

using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.ExternalTools;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui.ProgressMonitoring;

namespace MonoDevelop.Ide.Commands
{
	public enum ToolCommands
	{
		AddinManager,
		ToolList
	}
	
	internal class AddinManagerHandler: CommandHandler
	{
		protected override void Run ()
		{
			AddinUpdateHandler.ShowManager ();
		}
	}
	
	internal class ToolListHandler: CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			for (int i = 0; i < ExternalToolService.Tools.Count; ++i) {
				CommandInfo cmd = new CommandInfo (ExternalToolService.Tools[i].MenuCommand);
				cmd.Description = GettextCatalog.GetString ("Start tool") + " " + String.Join(String.Empty, ExternalToolService.Tools[i].MenuCommand.Split('&'));
				info.Add (cmd, ExternalToolService.Tools[i]);
			}
		}
		
		protected override void Run (object tool)
		{
			DispatchService.BackgroundDispatch (new StatefulMessageHandler (RunTool), tool);
		}

		private void RunTool (object ob)
		{
			ExternalTool tool = (ExternalTool) ob;
			
			// set the command
			string command = tool.Command;
			// set the args
			string args = StringParserService.Parse(tool.Arguments);
			// prompt for args if needed
			if (tool.PromptForArguments) {
				args = MessageService.GetTextResponse (
					GettextCatalog.GetString ("Enter any arguments you want to use while launching tool, {0}:", tool.MenuCommand),
					GettextCatalog.GetString ("Command Arguments for {0}", tool.MenuCommand), args);
					
				// if user selected cancel string will be null
				if (args == null) {
					args = StringParserService.Parse(tool.Arguments);
				}
			}
			if (tool.SaveCurrentFile && MonoDevelop.Ide.Gui.IdeApp.Workbench.ActiveDocument != null)
				MonoDevelop.Ide.Gui.IdeApp.Workbench.ActiveDocument.Save ();
			
			// create the process
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();
			monitor.Log.WriteLine ("Running: {0} {1} ...", command, args);
			monitor.Log.WriteLine ();
			
			try {
				ProcessWrapper p;
				string workingDirectory = StringParserService.Parse(tool.InitialDirectory);
				if (tool.UseOutputPad)
					p = Runtime.ProcessService.StartProcess (command, args, workingDirectory, monitor.Log, monitor.Log, null);
				else
					p = Runtime.ProcessService.StartProcess (command, args, workingDirectory, null);

				p.WaitForOutput ();
				
				monitor.Log.WriteLine ();
				if (p.ExitCode == 0) {
					monitor.Log.WriteLine ("Process '{0}' has completed succesfully.", p.ProcessName); 
				} else {
					monitor.Log.WriteLine ("Process '{0}' has exited with errorcode {1}.", p.ProcessName, p.ExitCode);
				}
				
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("External program execution failed.\nError while starting:\n '{0} {1}'", command, args), ex);
			} finally {
				monitor.Dispose ();
			}
		}
	}
}
