﻿//
// AzureFunctionsProjectExtension.cs

// Copyright (c) Microsoft Corp.
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
using System.IO;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

namespace MonoDevelop.AzureFunctions
{
	[ExportProjectModelExtension, AppliesTo ("AzureFunctions")]
	public class AzureFunctionsProjectExtension : DotNetProjectExtension
	{
		readonly string FuncExe;

		public AzureFunctionsProjectExtension ()
		{
			FuncExe = GetFuncExe ();
		}

		static string GetFuncExe ()
		{
			var funcEnv = Environment.GetEnvironmentVariable ("AZURE_FUNCTIONS_CLI_EXE");
			if (!string.IsNullOrEmpty (funcEnv)) {
				if (string.Equals (Path.GetFileName (funcEnv), "func.exe", StringComparison.OrdinalIgnoreCase)) {
					LoggingService.LogError ($"AZURE_FUNCTIONS_CLI_EXE is set but does not point to func.exe");
				} else if (!File.Exists (funcEnv)) {
					LoggingService.LogError ($"AZURE_FUNCTIONS_CLI_EXE is set but its target is missing");
				} else {
					return Path.GetFullPath (funcEnv);
				}
			}
			return AddinManager.CurrentAddin.GetFilePath (Path.Combine ("azure-functions-cli-66a932fb", "func.exe"));
		}

		protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			return true;
		}

		protected override ProjectFeatures OnGetSupportedFeatures ()
		{
			return (base.OnGetSupportedFeatures () | ProjectFeatures.Execute) ^ ProjectFeatures.RunConfigurations;
		}

		protected override DotNetProjectFlags OnGetDotNetProjectFlags ()
		{
			return base.OnGetDotNetProjectFlags () | DotNetProjectFlags.IsLibrary;
		}

		protected override ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			// Unless we pass a port it will spawn a child host, which won't be followed by the debugger.
			var cfg = (DotNetProjectConfiguration)Project.GetConfiguration (configSel);
			var cmd = new DotNetExecutionCommand (FuncExe) {
				Arguments = "host start -p 7071 --pause-on-error",
				WorkingDirectory = cfg.OutputDirectory,
				EnvironmentVariables = cfg.GetParsedEnvironmentVariables (),
			};

			// The Mono Mac filesystem watcher immediate fires a change event on the assembly,
			// causing the host to shutdown and restart as a new process.
			// However, it doesn't seem to be possible to disable the host's filesystem watcher.
			// Instead, force Mono to disable file watching.
			cmd.EnvironmentVariables ["MONO_MANAGED_WATCHER"] = "disabled";

			return cmd;
		}
	}
}
