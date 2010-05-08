// 
// AspNetExecutionHandler.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using System.IO;

namespace MonoDevelop.AspNet
{
	public class AspNetExecutionHandler: IExecutionHandler
	{
		string GetXspName (AspNetExecutionCommand cmd)
		{
			string xspName;
			switch (cmd.ClrVersion) {
			case ClrVersion.Net_1_1:
				return "xsp1";
			case ClrVersion.Net_2_0:
				return "xsp2";
			case ClrVersion.Net_4_0:
				return "xsp4";
			default:
				return null;
			}
		}
		
		FilePath GetXspPath (AspNetExecutionCommand cmd, string xspName)
		{
			FilePath xspPath = cmd.TargetRuntime.GetToolPath (cmd.TargetFramework, xspName);
			
			if (xspPath.IsNullOrEmpty && cmd.ClrVersion == ClrVersion.Net_1_1)
				xspPath = cmd.TargetRuntime.GetToolPath (cmd.TargetFramework, "xsp");
			
			//if the current runtime doesn't provide XSP, use one bundled alongside the addin
			if (xspPath.IsNullOrEmpty)
				 xspPath = Path.Combine (Path.GetDirectoryName (typeof (AspNetExecutionHandler).Assembly.CodeBase), xspName);
			
			return xspPath;
		}
		
		public bool CanExecute (ExecutionCommand command)
		{
			var cmd = command as AspNetExecutionCommand;
			return cmd != null && !string.IsNullOrEmpty (GetXspName (cmd));
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			var cmd = (AspNetExecutionCommand) command;
			var xspName = GetXspName (cmd);
			var xspPath = GetXspPath (cmd, xspName);
			
			if (xspPath.IsNullOrEmpty || !File.Exists (xspPath))
				throw new UserException (string.Format ("The \"{0}\" web server cannot be started. Please ensure that it is installed.", xspName), null);
		
			//if it's a script, use a native execution handler
			if (xspPath.Extension != ".exe") {
				//set mono debug mode if project's in debug mode
				var envVars = cmd.TargetRuntime.GetToolsEnvironmentVariables (cmd.TargetFramework); 
				if (cmd.DebugMode) {
					envVars = new Dictionary<string, string> (envVars);
					envVars ["MONO_OPTIONS"] = "--debug";
				}
				
				var ncmd = new NativeExecutionCommand (
					xspPath, cmd.XspParameters.GetXspParameters () + " --nonstop",
					cmd.BaseDirectory, envVars);
				
				return Runtime.ProcessService.GetDefaultExecutionHandler (ncmd).Execute (ncmd, console);
			}

			// Set DEVPATH when running on Windows (notice that this has no effect unless
			// <developmentMode developerInstallation="true" /> is set in xsp2.exe.config

			var evars = cmd.TargetRuntime.GetToolsEnvironmentVariables (cmd.TargetFramework);
			if (cmd.TargetRuntime is MsNetTargetRuntime)
				evars["DEVPATH"] = Path.GetDirectoryName (xspPath);
			
			var netCmd = new DotNetExecutionCommand (
				xspPath, cmd.XspParameters.GetXspParameters () + " --nonstop",
				cmd.BaseDirectory, evars);
			netCmd.DebugMode = cmd.DebugMode;
			
			return cmd.TargetRuntime.GetExecutionHandler ().Execute (netCmd, console);
		}
	}
}
