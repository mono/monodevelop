using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Assemblies;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Win32
{
	class CorDebuggerEngine: IDebuggerEngine
	{
		#region IDebuggerEngine Members

		public bool CanDebugCommand (ExecutionCommand command)
		{
			DotNetExecutionCommand cmd = command as DotNetExecutionCommand;
			return cmd != null && (cmd.TargetRuntime == null || cmd.TargetRuntime.RuntimeId == "MS.NET");
		}

		public DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand command)
		{
			DotNetExecutionCommand cmd = (DotNetExecutionCommand) command;
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

		public DebuggerSession CreateSession ( )
		{
			return new CorDebuggerSession ();
		}

		public ProcessInfo[] GetAttachablePocesses ( )
		{
			return new ProcessInfo[0];
		}

		public string Name
		{
			get { return "Microsoft .NET Debugger"; }
		}

		public DebuggerFeatures SupportedFeatures
		{
			get {
				return DebuggerFeatures.All;
			}
		}

		#endregion
	}
}
