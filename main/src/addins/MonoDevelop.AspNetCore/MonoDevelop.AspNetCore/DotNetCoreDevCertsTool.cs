//
// DotNetCoreDevCertsTool.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.AspNetCore
{
	static class DotNetCoreDevCertsTool
	{
		public static bool IsInstalled ()
		{
			FilePath toolsDirectory = GetToolsDirectory ();
			if (toolsDirectory.IsNullOrEmpty)
				return false;

			var devCertsTool = toolsDirectory.Combine (GetDevCertsToolFileName ());
			return File.Exists (devCertsTool);
		}

		static FilePath GetToolsDirectory ()
		{
			FilePath homePath = Environment.GetEnvironmentVariable ("HOME");
			if (homePath.IsNotNull) {
				homePath = homePath.Combine (".dotnet", "tools");
			}
			return homePath;
		}

		static string GetDevCertsToolFileName ()
		{
			if (Platform.IsWindows) {
				return "dotnet-dev-certs.exe";
			}
			return "dotnet-dev-certs";
		}

		/// <summary>
		/// Should not need to install the dev certs tool. For the preview we need to install it.
		/// </summary>
		public static async Task<bool> Install (CancellationToken cancellationToken)
		{
			using (var progressMonitor = CreateProgressMonitor ()) {
				try {
					string arguments = "install tool dotnet-dev-certs -g --version " + GetDevCertsVersion ();
					progressMonitor.Log.WriteLine ("{0} {1}", DotNetCoreRuntime.FileName, arguments);

					var process = Runtime.ProcessService.StartConsoleProcess (
						DotNetCoreRuntime.FileName,
						arguments,
						null,
						progressMonitor.Console
					);

					using (var customCancelToken = cancellationToken.Register (process.Cancel)) {
						await process.Task;
						if (process.ExitCode == 0) {
							return true;
						} else {
							progressMonitor.Log.WriteLine (GettextCatalog.GetString ("Install failed. dotnet install returned {0}", process.ExitCode));
							return false;
						}
					}
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception ex) {
					progressMonitor.Log.WriteLine (ex.Message);
					LoggingService.LogError ("Failed to install dotnet-dev-certs.", ex);
					return false;
				}
			}
		}

		/// <summary>
		/// Try to find the dev-certs version that is included with the .NET Core SDK
		/// and return that. Otherwise default to the preview version: 2.1.0-preview1-final
		/// </summary>
		static string GetDevCertsVersion ()
		{
			FilePath dotNetRuntime = DotNetCoreRuntime.FileName;
			var fallbackFolder = dotNetRuntime.ParentDirectory.Combine ("sdk", "NuGetFallbackFolder");
			var devCertsDirectory = fallbackFolder.Combine ("dotnet-dev-certs");

			if (Directory.Exists (devCertsDirectory)) {
				FilePath fullPath = Directory.EnumerateDirectories (devCertsDirectory).FirstOrDefault ();
				if (!fullPath.IsNullOrEmpty) {
					return fullPath.FileName;
				}
			}

			return "2.1.0-preview1-final";
		}

		static OutputProgressMonitor CreateProgressMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				"DotNetCoreDevCertsConsole",
				GettextCatalog.GetString (".NET Core Certificate Manager"),
				Stock.Console,
				false,
				true);
		}

		public static async Task<CertificateCheckResult> CheckCertificate (CancellationToken cancellationToken)
		{
			int exitCode = await RunDotNetCommand (
				"dev-certs https --trust --check",
				cancellationToken
			);

			// Check exit code is known.
			var result = (CertificateCheckResult)exitCode;
			if (Enum.IsDefined (typeof (CertificateCheckResult), result)) {
				return result;
			}

			LoggingService.LogError ($"Unknown exit code returned from 'dotnet dev-certs https --trust --check': {exitCode}");

			return CertificateCheckResult.Error;
		}

		static async Task<int> RunDotNetCommand (string command, CancellationToken cancellationToken)
		{
			var process = Runtime.ProcessService.StartProcess (
				DotNetCoreRuntime.FileName,
				command,
				null,
				null,
				null
			);

			using (process) {
				process.SetCancellationToken (cancellationToken);
				await process.Task;
				return process.ExitCode;
			}
		}

		/// <summary>
		/// To install and/or trust the certificate the following is done:
		///
		/// 1. Run the DevCertInstaller console app included with the IDE.
		///
		/// 2. DevCertInstaller will use the AuthorizationExecuteWithPrivileges
		/// Mac API provided by Xamarin.Mac to run the DevCertWrapper console app
		/// as root. The DevCertInstaller app cannot run dotnet as root here since
		/// it requires the user id to be set to 0, the same as running 'sudo dotnet'
		/// DevCertInstaller is needed since we want to wait until dotnet has
		/// finished trusting the certificate. To do that the DevCertInstaller
		/// uses the Posix wait APIs to detect when the child process started by
		/// AuthorizationExecuteWithPrivileges has finished. Since the IDE itself
		/// has other child processes a separate DevCertInstaller app is used.
		///
		/// 3. DevCertWrapper calls setuid to set the current id to be 0.
		///
		/// 4. DevCertWrapper then runs 'dotnet dev-certs https --trust' which
		/// will install the HTTPS development certificate if it is missing and
		/// ensure it is trusted.
		/// </summary>
		public static async Task TrustCertificate (CancellationToken cancellationToken)
		{
			using (var progressMonitor = CreateProgressMonitor ()) {
				try {
					string installerPath = GetInstallerPath ();

					var monoRuntime = Runtime.SystemAssemblyService.DefaultRuntime as MonoTargetRuntime;
					string monoPath = monoRuntime.GetMonoExecutableForAssembly (installerPath);

					var process = Runtime.ProcessService.StartConsoleProcess (
						monoPath,
						$"\"{installerPath}\" \"{DotNetCoreRuntime.FileName}\" \"{monoPath}\"",
						null,
						progressMonitor.Console
					);

					using (var customCancelToken = cancellationToken.Register (process.Cancel)) {
						await process.Task;
					}
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception ex) {
					progressMonitor.Log.WriteLine (ex.Message);
					LoggingService.LogError ("Failed to trust HTTPS certificate.", ex);
				}
			}
		}

		static string GetInstallerPath ()
		{
			string directory = Path.GetDirectoryName (typeof (IdeStartup).Assembly.Location);
			string fileName = Path.Combine (directory, "MonoDevelop.AspNetCore.DevCertInstaller.exe");
			if (File.Exists (fileName)) {
				return fileName;
			}

			throw new FileNotFoundException ("Unable to find dev certificate installer.", fileName);
		}
	}
}
