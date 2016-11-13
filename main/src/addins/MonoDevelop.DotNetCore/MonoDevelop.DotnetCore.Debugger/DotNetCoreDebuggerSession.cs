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

		//TODO: version the download
		static string DebugAdapterPath = Path.Combine (UserProfile.Current.CacheDir, "CoreClrAdaptor", "OpenDebugAD7");
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
					{"enableStepFiltering", JToken.FromObject (Options.StepOverPropertiesAndOperators)},
					{"externalConsole", JToken.FromObject (startInfo.UseExternalConsole)}
				}
			);
			return launchRequest;
		}

		protected override AttachRequest CreateAttachRequest (long processId)
		{
			var attachRequest = new AttachRequest (
				new Dictionary<string, JToken> () {
					{"name" , JToken.FromObject (".NET Core Attach")},
					{"type" , JToken.FromObject ("coreclr")},
					{"request" , JToken.FromObject ("launch")},
					{"processId" , JToken.FromObject (processId)},
					{"justMyCode", JToken.FromObject (Options.ProjectAssembliesOnly)},
					{"requireExactSource", JToken.FromObject (false)},//Mimic XS behavior
					{"enableStepFiltering", JToken.FromObject (Options.StepOverPropertiesAndOperators)}
				}
			);
			return attachRequest;
		}

		protected override void OnAttachToProcess (long processId)
		{
			Download (() => Attach (processId));
		}

		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			Download (() => Launch (startInfo));
		}

		async void Download(Action callback)
		{
			if (File.Exists (DebugAdapterPath)) {
				callback ();
				return;
			}

			try {
				cancelEngineDownload = new CancellationTokenSource ();
				var installSuccess = await InstallDotNetCoreDebugger (cancelEngineDownload.Token);

				if (installSuccess && !cancelEngineDownload.IsCancellationRequested) {
					callback ();
				} else {
					OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited));
				}
			} catch (OperationCanceledException) {
			} catch (Exception e) {
				LoggingService.LogError ("Error downloading .Net Core debugger adaptor", e);
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
				progressMonitor.CancellationToken.Register (() => {
					cancelEngineDownload?.Cancel ();
				});
				try {
					if (await InstallDebuggerFilesInternal (progressMonitor, token)) {
						return true;
					}
					if (token.IsCancellationRequested) {
						return false;
					}
				} catch (Exception ex) {
					ex = ex.FlattenAggregate ();
					if (ex is OperationCanceledException) {
						return false;
					}
					LoggingService.LogInternalError (ex);
				}
				progressMonitor.ReportError (GettextCatalog.GetString ("Could not install .NET Core debugger adaptor"));
			}
			return false;
		}

		async Task<bool> InstallDebuggerFilesInternal (ProgressMonitor progressMonitor, CancellationToken token)
		{
			var dotnetPath = new DotNetCore.DotNetCorePath ().FileName;

			//TODO: check whether the file was downloaded already, check hash?
			//TODO: resume partial downloads?
			var url = GetDebuggerZipUrl ();
			var tempZipPath = UserProfile.Current.CacheDir.Combine ("coreclr-debug-osx.10.11-x64.zip");

			using (var progressTask = progressMonitor.BeginTask (GettextCatalog.GetString ("Downloading .NET Core debugger..."), 1000)) {
				int reported = 0;
				await DownloadWithProgress (
					url,
					tempZipPath,
					(p) => {
						int progress = (int) (1000f * p);
						if (reported < progress) {
							progressMonitor.Step (progress - reported);
							reported = progress;
						}
					},
					token
				);
			}

			using (progressMonitor.BeginTask (GettextCatalog.GetString ("Installing .NET Core debugger..."), 1)) {
				//clean up any old debugger files
				if (Directory.Exists (DebugAdapterDir)) {
					Directory.Delete (DebugAdapterDir, true);
				}

				Directory.CreateDirectory (DebugAdapterDir);
				using (var archive = ZipFile.Open (tempZipPath, ZipArchiveMode.Read)) {
					foreach (var entry in archive.Entries) {
						var name = Path.Combine (DebugAdapterDir, entry.FullName);
						if (name[name.Length-1] == Path.DirectorySeparatorChar) {
							Directory.CreateDirectory (name);
						} else {
							var dir = Path.GetDirectoryName (name);
							Directory.CreateDirectory (dir);
							entry.ExtractToFile (name, true);
						}
					}
				}

				if (File.Exists (DebugAdapterPath)) {
					foreach (var file in Directory.GetFiles (DebugAdapterDir, "*", SearchOption.AllDirectories)) {
						Syscall.chmod (file, FilePermissions.S_IRWXU | FilePermissions.S_IRGRP | FilePermissions.S_IXGRP | FilePermissions.S_IROTH | FilePermissions.S_IXOTH);
					}
				} else {
					progressMonitor.ReportError (GettextCatalog.GetString("Failed to extract files"));
					return false;
				}
			}

			return true;
		}

		static async Task DownloadWithProgress (string fromUrl, string toFile, Action<float> reportProgress, CancellationToken token)
		{
			try {
				Directory.CreateDirectory (Path.GetDirectoryName (toFile));

				var response = await WebRequestHelper.GetResponseAsync (
					() => WebRequest.CreateHttp (fromUrl),
					null,
					token
				);

				using (var fs = File.Open (toFile, FileMode.Create, FileAccess.ReadWrite)) {
					var stream = response.GetResponseStream ();

					long total = stream.Length;
					long copied = 0;
					float progressTotal = 0;
					byte [] buffer = new byte [4096];

					int read;
					while ((read = await stream.ReadAsync (buffer, 0, buffer.Length, token).ConfigureAwait (false)) != 0) {
						await fs.WriteAsync (buffer, 0, read, token).ConfigureAwait (false);
						copied += read;
						float progressIncrement = copied / (float)total - progressTotal;
						if (progressIncrement > 0.001f) {
							progressTotal += progressIncrement;
							reportProgress (progressTotal);
						}
					}
				}
			} catch (WebException wex) {
				if (wex.Status == WebExceptionStatus.RequestCanceled) {
					token.ThrowIfCancellationRequested ();
				}
			}
		}

		ProgressMonitor CreateProgressMonitor ()
		{
			//TODO: make cancellable
			return IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				GettextCatalog.GetString ("Installing .NET Core debugger..."),
				Stock.StatusSolutionOperation,
				true,
				true,
				false,
				null,
				true);
		}

		string GetDebuggerZipUrl ()
		{
			if (Platform.IsMac)
				return "https://vsdebugger.azureedge.net/coreclr-debug-1-5-0/coreclr-debug-osx.10.11-x64.zip";
			//TODO: other platforms
			throw new NotImplementedException ();
		}
	}
}