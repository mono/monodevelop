// ToolsCommands.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//


using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui;
using System;
using MonoDevelop.Ide.Updater;

namespace MonoDevelop.Ide.Commands
{
	public enum ToolCommands
	{
		AddinManager,
		ToolList,
		InstrumentationViewer
	}

	internal class AddinManagerHandler : CommandHandler
	{
		protected override void Run ()
		{
			AddinsUpdateHandler.ShowManager ();
		}
	}

	internal class ToolListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			foreach (ExternalTools.ExternalTool externalTool in ExternalTools.ExternalToolService.Tools) {
				//Create CommandInfo object
				CommandInfo commandInfo = new CommandInfo ();
				commandInfo.Text = externalTool.MenuCommand;
				commandInfo.Description = GettextCatalog.GetString ("Start tool") + " " + string.Join (string.Empty, externalTool.MenuCommand.Split('&'));

				//Add menu item
				info.Add (commandInfo, externalTool);

			}
		}

		protected override void Run (object dataItem)
		{
			ExternalTools.ExternalTool tool = (ExternalTools.ExternalTool)dataItem;
			
			string argumentsTool = StringParserService.Parse (tool.Arguments, IdeApp.Workbench.GetStringTagModel ());
			
			//Save current file checkbox
			if (tool.SaveCurrentFile && IdeApp.Workbench.ActiveDocument != null)
				IdeApp.Workbench.ActiveDocument.Save ();

			if (tool.PromptForArguments) {
				string customerArguments = MessageService.GetTextResponse (GettextCatalog.GetString ("Enter any arguments you want to use while launching tool, {0}:", tool.MenuCommand), GettextCatalog.GetString ("Command Arguments for {0}", tool.MenuCommand), "");
				if (customerArguments != String.Empty)
					argumentsTool = StringParserService.Parse (customerArguments, IdeApp.Workbench.GetStringTagModel ());
			}

			DispatchService.BackgroundDispatch (delegate {
				RunExternalTool (tool, argumentsTool);
			});
		}

		void RunExternalTool (ExternalTools.ExternalTool tool, string argumentsTool)
		{
			string commandTool = StringParserService.Parse (tool.Command, IdeApp.Workbench.GetStringTagModel ());
			string initialDirectoryTool = StringParserService.Parse (tool.InitialDirectory, IdeApp.Workbench.GetStringTagModel ());

			//Execute tool
			IProgressMonitor progressMonitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();
			try {
				progressMonitor.Log.WriteLine (GettextCatalog.GetString ("Running: {0} {1}", (commandTool), (argumentsTool)));
				progressMonitor.Log.WriteLine ();

				ProcessWrapper processWrapper;
				if (tool.UseOutputPad)
					processWrapper = Runtime.ProcessService.StartProcess (commandTool, argumentsTool, initialDirectoryTool, progressMonitor.Log, progressMonitor.Log, null);
				else
					processWrapper = Runtime.ProcessService.StartProcess (commandTool, argumentsTool, initialDirectoryTool, null);

				string processName = System.IO.Path.GetFileName (commandTool);
				try {
					processName = processWrapper.ProcessName;
				} catch (SystemException) {
				}

				processWrapper.WaitForOutput ();

				if (processWrapper.ExitCode == 0) {
					progressMonitor.Log.WriteLine (GettextCatalog.GetString ("Process '{0}' has completed succesfully", processName));
				} else {
					progressMonitor.Log.WriteLine (GettextCatalog.GetString ("Process '{0}' has exited with error code {1}", processName, processWrapper.ExitCode));
				}
			} catch (Exception ex) {
				progressMonitor.ReportError (GettextCatalog.GetString ("External program execution failed.\nError while starting:\n '{0} {1}'", commandTool, argumentsTool), ex);
			} finally {
				progressMonitor.Dispose ();
			}

		}
	}

	internal class InstrumentationViewerHandler : CommandHandler
	{
		protected override void Run ()
		{
			MonoDevelop.Core.Instrumentation.InstrumentationService.StartMonitor ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Visible = MonoDevelop.Core.Instrumentation.InstrumentationService.Enabled;
		}
	}
}
