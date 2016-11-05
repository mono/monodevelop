//
// DotNetCoreDebuggerSession.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Debugger.VsCodeDebugProtocol;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using Newtonsoft.Json.Linq;

namespace MonoDevelop.DotnetCore.Debugger
{
	public class DotNetCoreDebuggerSession : VSCodeDebuggerSession
	{
		CancellationTokenSource cancelEngineDownload;

		static string DebugAdapterPath = Path.Combine (UserProfile.Current.LocalInstallDir, "CoreClrAdaptor", "OpenDebugAD7");
		static string DebugAdapterDir = Path.GetDirectoryName (DebugAdapterPath);

		protected override string GetDebugAdapterPath ()
		{
			return DebugAdapterPath;
		}

		protected override InitializeRequest CreateInitRequest ()
		{
			var initRequest = new InitializeRequest (
				"coreclr",
				true,
				true,
				InitializeArguments.PathFormatValue.Path,
				true,
				false,//TODO: Add support for VariablePaging
				false//TODO: Add support for RunInTerminal
			);
			return initRequest;
		}

		protected override LaunchRequest CreateLaunchRequest (DebuggerStartInfo startInfo)
		{
			var cwd = string.IsNullOrWhiteSpace (startInfo.WorkingDirectory) ? Path.GetDirectoryName (startInfo.Command) : startInfo.WorkingDirectory;
			var launchRequest = new LaunchRequest (
				false,
				new Dictionary<string, JToken> () {
					{"name" , JToken.FromObject (".NET Core Launch")},
					{"type" , JToken.FromObject ("coreclr")},
					{"request" , JToken.FromObject ("launch")},
					{"preLaunchTask" , JToken.FromObject ("build")},
					{"program" , JToken.FromObject (startInfo.Command)},
					{"args" , JToken.FromObject (startInfo.Arguments.Split (new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries))},
					{"cwd" , JToken.FromObject (cwd)},
					{"env", JToken.FromObject (startInfo.EnvironmentVariables)},
					{"stopAtEntry" ,JToken.FromObject (false)},
					{"justMyCode", JToken.FromObject (Options.ProjectAssembliesOnly)},
					{"requireExactSource", JToken.FromObject (false)},//Mimic XS behavior
					{"enableStepFiltering",JToken.FromObject (Options.StepOverPropertiesAndOperators)}
				}
			);
			return launchRequest;
		}

		static bool updateChecked = false;

		protected override async void OnRun (DebuggerStartInfo startInfo)
		{
			if (!updateChecked && File.Exists (projectFilePath) && File.ReadAllText (projectFilePath) != GetProjectJsonContent ())
				Directory.Delete (DebugAdapterDir, true);

			updateChecked = true;

			if (File.Exists (DebugAdapterPath)) {
				Launch (startInfo);
				return;
			}

			cancelEngineDownload = new CancellationTokenSource ();
			var installSuccess = await InstallDotNetCoreDebugger (cancelEngineDownload.Token);
			cancelEngineDownload = null;

			if (installSuccess && !cancelEngineDownload.IsCancellationRequested) {
				Launch (startInfo);
			} else {
				OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited));
			}
		}

		protected override void OnExit ()
		{
			cancelEngineDownload?.Cancel ();
			base.OnExit ();
		}

		async Task<bool> InstallDotNetCoreDebugger (CancellationToken token)
		{
			using (var progressMonitor = CreateProgressMonitor ()) {
				using (progressMonitor.BeginTask (GettextCatalog.GetString ("Installing .NET Core debugger"), 4)) {
					try {
						if (await InstallDebuggerFilesInternal (progressMonitor, token)) {
							return true;
						}
						if (token.IsCancellationRequested) {
							progressMonitor.Log.WriteLine ("Cancelled");
							return false;
						}
					} catch (Exception ex) {
						LoggingService.LogInternalError (ex);
					}
					progressMonitor.ReportError (GettextCatalog.GetString ("Could not restore .NET Core debugger files."));
				}
			}
			return false;
		}

		async Task<bool> InstallDebuggerFilesInternal (ProgressMonitor progressMonitor, CancellationToken token)
		{
			var dotnetPath = new DotNetCore.DotNetCorePath ().FileName;

			Directory.CreateDirectory (DebugAdapterDir);
			WriteProjectJson ();

			progressMonitor.BeginStep ("dotnet restore");
			var proc = Runtime.ProcessService.StartProcess (
				dotnetPath,
				"--verbose restore --configfile NuGet.config",
				DebugAdapterDir,
				progressMonitor.Log,
				progressMonitor.ErrorLog,
				null);

			RegisterKillProcess (token, proc);
			await proc.Task;

			if (proc.ExitCode > 0) {
				progressMonitor.ErrorLog.WriteLine ($"Exited with code {proc.ExitCode}");
				return false;
			}

			progressMonitor.BeginStep ("dotnet publish");
			proc = Runtime.ProcessService.StartProcess (
				dotnetPath,
				$"--verbose publish -r {GetRuntimeId ()} -o \"{DebugAdapterDir}\"",
				DebugAdapterDir,
				progressMonitor.Log,
				progressMonitor.ErrorLog,
				null);

			RegisterKillProcess (token, proc);
			await proc.Task;

			if (proc.ExitCode > 0) {
				progressMonitor.ErrorLog.WriteLine ($"Exited with code {proc.ExitCode}");
				return false;
			}

			progressMonitor.BeginStep ("verifying publish");
			if (!File.Exists (Path.Combine (DebugAdapterDir, "coreclr.ad7Engine.json"))) {
				return false;
			}

			progressMonitor.BeginStep ("renaming files");
			var src = Path.Combine (DebugAdapterDir, "dummy");
			var dest = Path.Combine (DebugAdapterDir, "OpenDebugAD7");

			if (token.IsCancellationRequested)
				return false;

			if (!File.Exists (src)) {
				if (File.Exists (src + ".exe")) {
					src += ".exe";
					dest += ".exe";
				}
			}

			File.Move (src, dest);
			return true;
		}

		static void RegisterKillProcess (CancellationToken token, Core.Execution.ProcessWrapper proc)
		{
			token.Register (() => {
				if (!proc.HasExited) {
					try {
						proc.Kill ();
					} catch { }
				}
			});
		}

		ProgressMonitor CreateProgressMonitor ()
		{
			var outputProgressMonitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				GettextCatalog.GetString (".NET Core Debugger Install"),
				Stock.MessageLog,
				true,
				true);

			var pad = IdeApp.Workbench.ProgressMonitors.GetPadForMonitor (outputProgressMonitor);

			var monitor = new AggregatedProgressMonitor (outputProgressMonitor);
			monitor.AddFollowerMonitor (IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				GettextCatalog.GetString ("Installing .NET Core debugger..."),
				Stock.StatusSolutionOperation,
				true,
				false,
				false,
				pad,
				false));

			return monitor;
		}

		string projectFilePath = Path.Combine (DebugAdapterDir, "project.json");
		void WriteProjectJson ()
		{
			File.WriteAllText (Path.Combine (DebugAdapterDir, "NuGet.config"), @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <!--To inherit the global NuGet package sources remove the <clear/> line below -->
    <clear />
    <add key=""api.nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
    <!-- This dependency is not present in the release branch -->
    <add key=""coreclrdebug"" value=""https://www.myget.org/F/coreclr-debug/api/v3/index.json"" />
  </packageSources>
</configuration>");


			File.WriteAllText (projectFilePath, GetProjectJsonContent ());
		}

		string GetProjectJsonContent(){
			return @"{
  ""name"": ""dummy"",
  ""buildOptions"": {
    ""emitEntryPoint"": true
  },
  ""dependencies"": {
    ""Microsoft.VisualStudio.clrdbg"": ""15.0.25626-preview-3219185"",
    ""Microsoft.VisualStudio.clrdbg.MIEngine"": ""14.0.31028-preview-1"",
    ""Microsoft.VisualStudio.OpenDebugAD7"": ""1.0.21028-preview-2"",
    ""NETStandard.Library"": ""1.6.0"",
    ""Newtonsoft.Json"": ""7.0.1"",
    ""Microsoft.VisualStudio.Debugger.Interop.Portable"": ""1.0.1"",
    ""System.Collections.Specialized"": ""4.0.1"",
    ""System.Collections.Immutable"": ""1.2.0"",
    ""System.Diagnostics.Process"": ""4.1.0"",
    ""System.Dynamic.Runtime"": ""4.0.11"",
    ""Microsoft.CSharp"": ""4.0.1"",
    ""System.Threading.Tasks.Dataflow"": ""4.6.0"",
    ""System.Threading.Thread"": ""4.0.0"",
    ""System.Xml.XDocument"": ""4.0.11"",
    ""System.Xml.XmlDocument"": ""4.0.1"",
    ""System.Xml.XmlSerializer"": ""4.0.11"",
    ""System.ComponentModel"": ""4.0.1"",
    ""System.ComponentModel.Annotations"": ""4.1.0"",
    ""System.ComponentModel.EventBasedAsync"": ""4.0.11"",
    ""System.Runtime.Serialization.Primitives"": ""4.1.1"",
    ""System.Net.Http"": ""4.1.0""
  },
  ""frameworks"": {
    ""netcoreapp1.0"": {
      ""imports"": [
        ""dnxcore50"",
        ""portable-net45+win8""
      ]
    }
  },
  ""runtimes"": {
    """ + GetRuntimeId () + @""": {}
  }
}";
		}

		string GetRuntimeId ()
		{
			//FIXME: determine these properly
			if (Platform.IsMac)
				return "osx.10.11-x64";
			else if (Platform.IsWindows)
				return "win7-x64";
			else if (Platform.IsLinux) {
				string distro = null;
				try {
					using (var output = new StringWriter ())
					using (var error = new StringWriter ())
					using (var proc = Runtime.ProcessService.StartProcess ("/etc/os-release", null, null, output, error, null)) {
						if (proc.WaitForExit (5000)) {
							distro = output.ToString ();
						}
					}
				} catch {
				}
				if (string.IsNullOrEmpty (distro)) {
					try {
						using (var output = new StringWriter ())
						using (var error = new StringWriter ())
						using (var proc = Runtime.ProcessService.StartProcess ("/usr/lib/os-release", null, null, output, error, null)) {
							if (proc.WaitForExit (5000)) {
								distro = output.ToString ();
							}
						}
					} catch {
					}
				}
				string name = null;
				string version = null;
				var lines = distro.Split ('\n');
				foreach (var l in lines) {
					var line = l.Trim ();

					var equalsIndex = line.IndexOf ('=');
					if (equalsIndex >= 0) {
						var key = line.Substring (0, equalsIndex);
						var value = line.Substring (equalsIndex + 1);

						// Strip double quotes if necessary
						if (value.Length > 1 && value.StartsWith ("\"", StringComparison.Ordinal) && value.EndsWith ("\"", StringComparison.Ordinal)) {
							value = value.Substring (1, value.Length - 1);
						}

						if (key == "ID") {
							name = value;
						} else if (key == "VERSION_ID") {
							version = value;
						}

						if (name != null && version != null) {
							break;
						}
					}
				}
				switch (name) {
				case "ubuntu":
					if (version.StartsWith ("14", StringComparison.Ordinal)) {
						// This also works for Linux Mint
						return "ubuntu.14.04-x64";
					} else if (version.StartsWith ("16", StringComparison.Ordinal)) {
						return "ubuntu.16.04-x64";
					}

					break;
				case "centos":
					return "centos.7-x64";
				case "fedora":
					return "fedora.23-x64";
				case "opensuse":
					return "opensuse.13.2-x64";
				case "rhel":
					return "rhel.7-x64";
				case "debian":
					return "debian.8-x64";
				case "ol":
					// Oracle Linux is binary compatible with CentOS
					return "centos.7-x64";
				case "elementary":
				case "elementary OS":
					if (version.StartsWith ("0.3", StringComparison.Ordinal)) {
						// Elementary OS 0.3 Freya is binary compatible with Ubuntu 14.04
						return "ubuntu.14.04-x64";
					} else if (version.StartsWith ("0.4", StringComparison.Ordinal)) {
						// Elementary OS 0.4 Loki is binary compatible with Ubuntu 16.04
						return "ubuntu.16.04-x64";
					}

					break;
				case "linuxmint":
					if (version.StartsWith ("18", StringComparison.Ordinal)) {
						// Linux Mint 18 is binary compatible with Ubuntu 16.04
						return "ubuntu.16.04-x64";
					}
					break;
				}
				throw new Exception ("Unknown Linux Distro:" + distro);
			} else {
				throw new Exception ("Unknown platform");
			}
		}
	}
}
