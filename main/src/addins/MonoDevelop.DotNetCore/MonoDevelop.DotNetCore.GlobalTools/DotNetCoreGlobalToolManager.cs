//
// DotNetCoreGlobalToolManager.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019 Microsoft, Corp. (http://microsoft.com)
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DotNetCore.GlobalTools
{
	public static class DotNetCoreGlobalToolManager
	{
		public static bool IsInstalled (string packageId)
		{
			FilePath toolsDirectory = GetToolsDirectory ();
			if (toolsDirectory.IsNullOrEmpty)
				return false;

			var tool = toolsDirectory.Combine (packageId);
			return File.Exists (tool);
		}

		public static async Task<bool> Install (string packageId, CancellationToken cancellationToken)
		{
			if (IsInstalled (packageId)) {
				LoggingService.LogInfo ($".NET Core global tool {packageId} already installed, skipping installation");
				return true;
			}

			using (var progressMonitor = CreateProgressMonitor ()) {
				try {
					string arguments = $"tool install {packageId} -g";
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
					LoggingService.LogError ($"Failed to install {packageId}.", ex);
					return false;
				}
			}
		}

		static FilePath GetToolsDirectory ()
		{
			FilePath homePath = Environment.GetEnvironmentVariable ("HOME");
			if (homePath.IsNotNull) {
				homePath = homePath.Combine (".dotnet", "tools");
			}
			return homePath;
		}

		static OutputProgressMonitor CreateProgressMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				"DotNetCoreGlobalToolConsole",
				GettextCatalog.GetString (".NET Core Global tool manager"),
				Stock.Console,
				false,
				true);
		}
	}
}
