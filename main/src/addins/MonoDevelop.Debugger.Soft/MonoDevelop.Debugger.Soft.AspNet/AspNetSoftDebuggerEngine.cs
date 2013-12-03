// 
// AspNetSoftDebuggerEngine.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using MonoDevelop.Debugger;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.AspNet;
using Mono.Debugging.Client;
using MonoDevelop.Debugger.Soft;
using System.Net;
using MonoDevelop.Core.Assemblies;
using Mono.Debugging.Soft;

namespace MonoDevelop.Debugger.Soft.AspNet
{
	public class AspNetSoftDebuggerEngine: IDebuggerEngine
	{
		public bool CanDebugCommand (ExecutionCommand command)
		{
			var cmd = command as AspNetExecutionCommand;
			return cmd != null && SoftDebuggerEngine.CanDebugRuntime (cmd.TargetRuntime);
		}
		
		FilePath GetFxDir (MonoTargetRuntime runtime, ClrVersion version)
		{
			FilePath prefix = runtime.Prefix;
			switch (version) {
			case ClrVersion.Net_1_1:
				return prefix.Combine ("lib", "mono", "1.0");
			case ClrVersion.Net_2_0:
				return prefix.Combine ("lib", "mono", "2.0");
			case ClrVersion.Net_4_0:
				var net45Path = prefix.Combine ("lib", "mono", "4.5");
				if (Directory.Exists (net45Path) && !MonoDevelop.Core.Platform.IsWindows) return net45Path;
				return prefix.Combine ("lib", "mono", "4.0");
			case ClrVersion.Net_4_5:
				return prefix.Combine ("lib", "mono", "4.5");
			}
			throw new InvalidOperationException (string.Format ("Unknown runtime version '{0}'", version));
		}
		
		public DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand command)
		{
			var cmd = (AspNetExecutionCommand) command;
			var evars = new Dictionary<string, string>(cmd.EnvironmentVariables);
			var runtime = (MonoTargetRuntime) cmd.TargetRuntime;

			foreach (var v in runtime.EnvironmentVariables)
			{
				if (!evars.ContainsKey (v.Key))
					evars.Add (v.Key, v.Value);
			}
			
			var startInfo = new SoftDebuggerStartInfo (runtime.Prefix, evars) {
				WorkingDirectory = cmd.BaseDirectory,
				Arguments = cmd.XspParameters.GetXspParameters ().Trim (),
			};
			
			var xspName = AspNetExecutionHandler.GetXspName (cmd);
			
			FilePath fxDir = GetFxDir (runtime, cmd.ClrVersion);
			FilePath xspPath = fxDir.Combine (xspName).ChangeExtension (".exe");
			
			//no idea why xsp is sometimes relocated to a "winhack" dir on Windows
			if (MonoDevelop.Core.Platform.IsWindows && !File.Exists (xspPath)) {
				var winhack = fxDir.Combine ("winhack");
				if (Directory.Exists (winhack))
					xspPath = winhack.Combine (xspName).ChangeExtension (".exe");
			}
			
			if (!File.Exists (xspPath))
				throw new UserException (GettextCatalog.GetString (
					"The \"{0}\" web server cannot be started. Please ensure that it is installed.", xspName), null);
			
			startInfo.Command = xspPath;
			SoftDebuggerEngine.SetUserAssemblyNames (startInfo, cmd.UserAssemblyPaths);
			
			return startInfo;
		}

		public DebuggerSession CreateSession ()
		{
			return new SoftDebuggerSession ();
		}
		
		public ProcessInfo[] GetAttachableProcesses ()
		{
			return new ProcessInfo[0];
		}
	}
}
