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

using MonoDevelop.Components.AutoTest;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui.Dialogs;
using System;
using MonoDevelop.Ide.Updater;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Commands
{
	public enum ToolCommands
	{
		AddinManager,
		ToolList,
		InstrumentationViewer,
		ToggleSessionRecorder,
		ReplaySession,
		EditCustomTools,
	}

	internal class AddinManagerHandler : CommandHandler
	{
		protected override void Run ()
		{
			AddinsUpdateHandler.ShowManager ();
		}
	}

	internal class RunCustomToolHandler : CommandHandler
	{
		ExternalTools.ExternalTool tool;

		public RunCustomToolHandler (ExternalTools.ExternalTool tool)
		{
			this.tool = tool;
		}

		protected override void Run ()
		{
			tool.Run ();
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
				commandInfo.Description = GettextCatalog.GetString ("Start tool {0}", string.Join (string.Empty, externalTool.MenuCommand.Split('&')));
				commandInfo.AccelKey = externalTool.AccelKey;

				//Add menu item
				info.Add (commandInfo, externalTool);

			}
			if (info.Count > 0)
				info.AddSeparator ();
		}

		protected override void Run (object dataItem)
		{
			ExternalTools.ExternalTool tool = (ExternalTools.ExternalTool)dataItem;
			tool.Run ();
		}
	}

	internal class EditCustomToolsHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Text = ExternalTools.ExternalToolService.Tools.Count > 0 ? GettextCatalog.GetString ("Edit Custom Tools...") : GettextCatalog.GetString ("Add Custom Tool...");
		}
		
		protected override void Run ()
		{
			IdeApp.Workbench.ShowGlobalPreferencesDialog (IdeServices.DesktopService.GetFocusedTopLevelWindow (), "ExternalTools");
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

	internal class ToggleSessionRecorderHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (AutoTestService.CurrentRecordSession == null) {
				AutoTestService.StartRecordingSession ();
			} else {
				var selector = new FileSelectorDialog ("Save session as...", Gtk.FileChooserAction.Save);
				try {
					var result = MessageService.RunCustomDialog (selector, MessageService.RootWindow);

					if (result == (int)Gtk.ResponseType.Cancel) {
						return;
					}

					AutoTestService.StopRecordingSession (selector.Filename);
				} finally {
					selector.Destroy ();
				}
			}
		}

		protected override void Update (CommandInfo info)
		{
			info.Visible = IdeApp.Preferences.EnableAutomatedTesting;
			info.Text = AutoTestService.CurrentRecordSession == null ? "Start Session Recorder" : "Stop Session Recorder";
		}
	}

	internal class ReplaySessionHandler : CommandHandler
	{
		protected override void Run ()
		{
			var selector = new FileSelectorDialog ("Open session");
			string filename = null;
			try {
				var result = MessageService.RunCustomDialog (selector, MessageService.RootWindow);

				if (result == (int)Gtk.ResponseType.Cancel) {
					return;
				}

				filename = selector.Filename;
			} finally {
				selector.Destroy ();
			}
			AutoTestService.ReplaySessionFromFile (filename);
		}

		protected override void Update (CommandInfo info)
		{
			info.Visible = IdeApp.Preferences.EnableAutomatedTesting;
		}
	}
}
