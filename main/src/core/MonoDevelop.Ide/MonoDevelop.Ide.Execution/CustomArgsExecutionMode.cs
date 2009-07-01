// 
// CustomExecutionModeWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Execution
{
	[DataInclude (typeof(CustomArgsExecutionModeData))]
	class CustomArgsExecutionHandler: ParameterizedExecutionHandler
	{
		public override bool CanExecute (ExecutionCommand command)
		{
			if (!(command is ProcessExecutionCommand))
				return false;
			foreach (IExecutionModeSet mset in Runtime.ProcessService.GetExecutionModes ()) {
				foreach (IExecutionMode mode in mset.ExecutionModes) {
					if (mode.ExecutionHandler.CanExecute (command))
						return true;
				}
			}
			return Runtime.ProcessService.DefaultExecutionHandler.CanExecute (command);
		}
			
		public override IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console, CommandExecutionContext ctx, object configurationData)
		{
			CustomArgsExecutionModeData data = (CustomArgsExecutionModeData) configurationData;
			
			// Customize the command
			
			ProcessExecutionCommand cmd = (ProcessExecutionCommand) command;
			if (!string.IsNullOrEmpty (data.Arguments))
				cmd.Arguments = data.Arguments;
			if (!string.IsNullOrEmpty (data.WorkingDirectory))
				cmd.WorkingDirectory = data.WorkingDirectory;
			foreach (KeyValuePair<string,string> var in data.EnvironmentVariables)
				cmd.EnvironmentVariables [var.Key] = var.Value;
			
			if (string.IsNullOrEmpty (data.ModeId))
				return Runtime.ProcessService.DefaultExecutionHandler.Execute (command, console);
			else {
				IExecutionMode targetMode = ExecutionModeCommandService.GetExecutionMode (ctx, data.ModeId);
				return targetMode.ExecutionHandler.Execute (command, console);
			}
		}
		
		public override IExecutionModeEditor CreateEditor ()
		{
			return new MonoDevelop.Ide.Gui.Dialogs.CustomExecutionModeWidget ();
		}
	}
	
	class CustomArgsExecutionModeData
	{
		[ItemProperty (DefaultValue="")]
		public string Arguments { get; set; }
		[ItemProperty (DefaultValue="")]
		public string WorkingDirectory { get; set; }
		[ItemProperty (DefaultValue="")]
		public string ModeId;
		[ItemProperty]
		public Dictionary<string,string> EnvironmentVariables = new Dictionary<string, string> ();
	}
}
