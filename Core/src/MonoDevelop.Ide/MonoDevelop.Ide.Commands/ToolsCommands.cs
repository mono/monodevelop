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
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
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
			for (int i = 0; i < ToolLoader.Tool.Count; ++i) {
				CommandInfo cmd = new CommandInfo (ToolLoader.Tool[i].ToString());
				cmd.Description = GettextCatalog.GetString ("Start tool") + " " + String.Join(String.Empty, ToolLoader.Tool[i].ToString().Split('&'));
				info.Add (cmd, ToolLoader.Tool[i]);
			}
		}
		
		protected override void Run (object tool)
		{
			Services.DispatchService.BackgroundDispatch (new StatefulMessageHandler (RunTool), tool);
		}

		private void RunTool (object ob)
		{
			StringParserService stringParserService = Runtime.StringParserService;
			ExternalTool tool = (ExternalTool) ob;
			
			// set the command
			string command = tool.Command;
			// set the args
			string args = stringParserService.Parse(tool.Arguments);
			// prompt for args if needed
			if (tool.PromptForArguments) {
				args = Services.MessageService.GetTextResponse(String.Format (GettextCatalog.GetString ("Enter any arguments you want to use while launching tool, {0}:"), tool.MenuCommand), String.Format (GettextCatalog.GetString ("Command Arguments for {0}"), tool.MenuCommand), args);
					
				// if user selected cancel string will be null
				if (args == null) {
					args = stringParserService.Parse(tool.Arguments);
				}
			}
			
			// debug command and args
			Runtime.LoggingService.Debug("command : " + command);
			Runtime.LoggingService.Debug("args    : " + args);
			
			// create the process
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();
			monitor.Log.WriteLine ("Running: {0} {1} ...", command, args);
			monitor.Log.WriteLine ();
			
			try {
				ProcessWrapper p;
				string workingDirectory = stringParserService.Parse(tool.InitialDirectory);
				if (tool.UseOutputPad)
					p = Runtime.ProcessService.StartProcess (command, args, workingDirectory, monitor.Log, monitor.Log, null);
				else
					p = Runtime.ProcessService.StartProcess (command, args, workingDirectory, null);

				p.WaitForOutput ();
				Runtime.LoggingService.Debug ("DONE");
				
				monitor.Log.WriteLine ();
				if (p.ExitCode == 0) {
					monitor.Log.WriteLine ("Process '{0}' has completed succesfully.", p.ProcessName); 
				} else {
					monitor.Log.WriteLine ("Process '{0}' has exited with errorcode {1}.", p.ProcessName, p.ExitCode);
				}
				
			} catch (Exception ex) {
				monitor.ReportError (String.Format (GettextCatalog.GetString ("External program execution failed.\nError while starting:\n '{0} {1}'"), command, args), ex);
			} finally {
				monitor.Dispose ();
			}
		}
	}
}
