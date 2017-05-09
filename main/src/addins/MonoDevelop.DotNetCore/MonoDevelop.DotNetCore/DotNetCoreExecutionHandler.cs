//
// DotNetCoreExecutionHandler.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreExecutionHandler : IExecutionHandler
	{
		public bool CanExecute (ExecutionCommand command)
		{
			return command is DotNetCoreExecutionCommand;
		}

		public ProcessAsyncOperation Execute (ExecutionCommand command, OperationConsole console)
		{
			var dotNetCoreCommand = (DotNetCoreExecutionCommand)command;

			// ApplicationURL is passed to ASP.NET Core server via ASPNETCORE_URLS enviorment variable
			var envVariables = dotNetCoreCommand.EnvironmentVariables.ToDictionary ((arg) => arg.Key, (arg) => arg.Value);
			envVariables ["ASPNETCORE_URLS"] = dotNetCoreCommand.ApplicationURL;

			var process = Runtime.ProcessService.StartConsoleProcess (
				dotNetCoreCommand.Command,
				dotNetCoreCommand.Arguments,
				dotNetCoreCommand.WorkingDirectory,
				console,
				envVariables);
			if (dotNetCoreCommand.LaunchBrowser) {
				LaunchBrowser (dotNetCoreCommand.ApplicationURL, dotNetCoreCommand.LaunchURL, process.Task).Ignore ();
			}
			return process;
		}

		public static async Task LaunchBrowser (string appUrl, string launchUrl, Task processTask)
		{
			launchUrl = launchUrl ?? "";
			Uri launchUri;
			//Check if lanuchUrl is valid absolute url and use it if it is...
			if (!Uri.TryCreate (launchUrl, UriKind.Absolute, out launchUri)) {
				//Otherwise check if appUrl is valid absolute and lanuchUrl is relative then concat them...
				Uri appUri;
				if (!Uri.TryCreate (appUrl, UriKind.Absolute, out appUri)) {
					LoggingService.LogWarning ("Failed to launch browser because invalid launch and app urls.");
					return;
				}
				if (!Uri.TryCreate (launchUrl, UriKind.Relative, out launchUri)) {
					LoggingService.LogWarning ("Failed to launch browser because invalid launch url.");
					return;
				}
				launchUri = new Uri (appUri, launchUri);
			}

			//Try to connect every 50ms while process is running
			while (!processTask.IsCompleted) {
				await Task.Delay (50);
				using (var tcpClient = new TcpClient ()) {
					try {
						tcpClient.Connect (launchUri.Host, launchUri.Port);
						// pause briefly to allow the server process to initialize
						await Task.Delay (TimeSpan.FromSeconds (1));
						break;
					} catch {
					}
				}
			}

			if (processTask.IsCompleted) {
				LoggingService.LogDebug ("Failed to launch browser because process exited before server started listening.");
				return;
			}

			// Process is still alive hence we succesfully connected inside loop to web server, launch browser
			DesktopService.ShowUrl (launchUri.AbsoluteUri);
		}
	}
}