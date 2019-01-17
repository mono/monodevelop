using System;
using System.Collections.Generic;
using System.IO;
using Mono.Debugging.Client;
using Mono.Debugging.Win32;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
#if ASPNET
using MonoDevelop.AspNet.Execution;
#endif

namespace MonoDevelop.Debugger.Win32
{
	class CorDebuggerEngine: IDebuggerEngine
	{
		#region IDebuggerEngine Members

		public bool CanDebugCommand (ExecutionCommand command)
		{
			DotNetExecutionCommand cmd = command as DotNetExecutionCommand;
			if (cmd != null)
				return (cmd.TargetRuntime == null || cmd.TargetRuntime.RuntimeId == "MS.NET");
#if ASPNET
			var acmd = command as AspNetExecutionCommand;
			if (acmd != null)
				return (acmd.TargetRuntime == null || acmd.TargetRuntime.RuntimeId == "MS.NET");
#endif
			return false;
		}

		public DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand command)
		{
			DotNetExecutionCommand cmd = command as DotNetExecutionCommand;
			if (cmd != null) {
				DebuggerStartInfo startInfo = new DebuggerStartInfo ();
				startInfo.Command = cmd.Command;
				startInfo.Arguments = cmd.Arguments;
				startInfo.WorkingDirectory = cmd.WorkingDirectory;
				if (cmd.EnvironmentVariables.Count > 0) {
					foreach (KeyValuePair<string, string> val in cmd.EnvironmentVariables)
						startInfo.EnvironmentVariables[val.Key] = val.Value;
				}
				return startInfo;
			}
#if ASPNET
			var acmd = command as AspNetExecutionCommand;
			if (acmd != null) {
				DebuggerStartInfo startInfo = new DebuggerStartInfo ();
				string xspName = AspNetExecutionHandler.GetXspName (acmd);
				string xspPath = acmd.TargetRuntime.GetToolPath (acmd.TargetFramework, xspName);
				if (!File.Exists (xspPath))
					throw new UserException (string.Format ("The \"{0}\" web server cannot be started. Please ensure that it is installed.", xspName), null);

				startInfo.Command = xspPath;
				startInfo.Arguments = acmd.XspParameters.GetXspParameters () + " --nonstop";
				startInfo.WorkingDirectory = acmd.BaseDirectory;

				// Set DEVPATH when running on Windows (notice that this has no effect unless
				// <developmentMode developerInstallation="true" /> is set in xsp2.exe.config

				startInfo.EnvironmentVariables["DEVPATH"] = Path.GetDirectoryName (xspPath);
				return startInfo;
			}
#endif
			throw new NotSupportedException ();
		}

		public DebuggerSession CreateSession ( )
		{
			return MtaThread.Run(() => new CorDebuggerSession(FilePath.GetInvalidPathChars()));
		}

		public ProcessInfo[] GetAttachableProcesses ( )
		{
			return new ProcessInfo[0];
		}

		#endregion
	}
}
