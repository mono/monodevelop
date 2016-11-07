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
using System.IO.Compression;
using System.Net;
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
using Mono.Unix.Native;

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

		protected override async void OnRun (DebuggerStartInfo startInfo)
		{
			if (File.Exists (DebugAdapterPath)) {
				Launch (startInfo);
				return;
			}

			try {
				cancelEngineDownload = new CancellationTokenSource ();
				var installSuccess = await InstallDotNetCoreDebugger (cancelEngineDownload.Token);

				if (installSuccess && !cancelEngineDownload.IsCancellationRequested) {
					Launch (startInfo);
				} else {
					OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited));
				}
			} catch (OperationCanceledException) {
			} catch (Exception e) {
				LoggingService.LogError ("Downloading .Net Core debugger adaptor", e);
			} finally {
				cancelEngineDownload = null;
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
							progressMonitor.ReportError ("Cancelled");
							return false;
						}
					} catch (Exception ex) {
						ex = ex.FlattenAggregate ();
						if (ex is OperationCanceledException) {
							return false;
						}
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

			var url = GetDebuggerZipUrl ();
			var tempZipPath = UserProfile.Current.CacheDir.Combine ("coreclr-debug-osx.10.11-x64.zip");

			//TODO: check whether the file was downloaded already
			//TODO: resume partial downloads?
			progressMonitor.BeginStep ("Downloading...");
			var response = await WebRequestHelper.GetResponseAsync (
				() => WebRequest.CreateHttp (url),
				null,
				token
			);

			//TODO: report progress
			using (var fs = File.OpenWrite (tempZipPath)) {
				await response.GetResponseStream ().CopyToAsync (fs, 4096, token);
			}

			//TODO: unpack
			progressMonitor.BeginStep ("Extracting...");
			Directory.CreateDirectory (DebugAdapterDir);
			using (var archive = ZipFile.Open (tempZipPath, ZipArchiveMode.Read)) {
				foreach (var entry in archive.Entries) {
					entry.ExtractToFile (Path.Combine (DebugAdapterDir, entry.FullName), true);
				}
			}

			if (File.Exists (DebugAdapterPath)) {
				foreach (var file in Directory.GetFiles (DebugAdapterDir, "*", SearchOption.AllDirectories)) {
					Syscall.chmod (file, FilePermissions.S_IRWXU | FilePermissions.S_IRGRP | FilePermissions.S_IXGRP | FilePermissions.S_IROTH | FilePermissions.S_IXOTH);
				}
			} else {
				progressMonitor.ReportError ("Failed to extract files");
				return false;
			}

			return true;
		}

		ProgressMonitor CreateProgressMonitor ()
		{
			//TODO: make cancellable
			return IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				GettextCatalog.GetString ("Installing .NET Core debugger..."),
				Stock.StatusSolutionOperation,
				true,
				false,
				false,
				null,
				false);
		}

		string GetDebuggerZipUrl ()
		{
			if (Platform.IsMac)
				return "https://dotnetcoredebugadaptor.blob.core.windows.net/download/CoreClrAdaptor.zip";
			//TODO: other platforms
			throw new NotImplementedException ();
		}
	}
}