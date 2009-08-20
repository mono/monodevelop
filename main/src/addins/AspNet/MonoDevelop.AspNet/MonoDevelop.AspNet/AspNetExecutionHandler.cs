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
		
		public bool CanExecute (ExecutionCommand command)
		{
			return command is AspNetExecutionCommand;
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			AspNetExecutionCommand cmd = command as AspNetExecutionCommand;
			
			string xspName = (cmd.ClrVersion == ClrVersion.Net_1_1)? "xsp" : "xsp2";
			string xspPath = cmd.TargetRuntime.GetToolPath (cmd.TargetFramework, xspName);
			
			//if the current runtime doesn't provide XSP, use one bundled alongside the addin
			if (String.IsNullOrEmpty (xspPath))
				 xspPath = Path.Combine (Path.GetDirectoryName (typeof (AspNetExecutionHandler).Assembly.CodeBase), xspName);
			
			//if it's a script, use a native execution handler
			if (!xspPath.EndsWith (".exe")) {
				//set mono debug mode if project's in debug mode
				var envVars = cmd.TargetRuntime.GetToolsEnvironmentVariables (cmd.TargetFramework); 
				if (cmd.DebugMode) {
					envVars = new Dictionary<string, string> (envVars);
					envVars ["MONO_OPTIONS"] = "--debug";
				}
				
				var ncmd = new NativeExecutionCommand (
					xspPath, cmd.XspParameters.GetXspParameters (),
					cmd.BaseDirectory, envVars);
				
				return Runtime.ProcessService.GetDefaultExecutionHandler (ncmd).Execute (ncmd, console);
			}
			
			var netCmd = new DotNetExecutionCommand (
				xspPath, cmd.XspParameters.GetXspParameters (),
				cmd.BaseDirectory, cmd.TargetRuntime.GetToolsEnvironmentVariables (cmd.TargetFramework));
			netCmd.DebugMode = cmd.DebugMode;
			
			return cmd.TargetRuntime.GetExecutionHandler ().Execute (netCmd, console);
		}
	}
}
