//
// AspNetCoreExecutionCommand.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using System.Threading.Tasks;
using System.Net.Sockets;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.DotNetCore;

namespace MonoDevelop.AspNetCore
{
	public class AspNetCoreExecutionCommand : DotNetCoreExecutionCommand
	{
		public AspNetCoreExecutionCommand (string directory, string outputPath, string arguments)
			: base (directory, outputPath, arguments)
		{
		}

		// Since we are now supporting more than one url, we added this property
		// so that it contains the raw value of AppUrl
		// which might provide more than one url i.e. https://localhost:5000;http://localhost:5001
		public string ApplicationURLs { get; set; }

		public override async Task PostLaunchAsync (Task processTask)
		{
			await base.PostLaunchAsync (processTask).ConfigureAwait (false);

			var aspNetCoreTarget = Target as AspNetCoreExecutionTarget;
			var launchUrl = LaunchURL ?? "";
			Uri launchUri;
			//Check if lanuchUrl is valid absolute url and use it if it is...
			if (!Uri.TryCreate (launchUrl, UriKind.Absolute, out launchUri)) {
				//Otherwise check if appUrl is valid absolute and lanuchUrl is relative then concat them...
				Uri appUri;
				if (!Uri.TryCreate (ApplicationURL, UriKind.Absolute, out appUri)) {
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
				await Task.Delay (50).ConfigureAwait (false);
				using (var tcpClient = new TcpClient ()) {
					try {
						await tcpClient.ConnectAsync (launchUri.Host, launchUri.Port).ConfigureAwait (false);
						// pause briefly to allow the server process to initialize
						await Task.Delay (TimeSpan.FromSeconds (1)).ConfigureAwait (false);
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
			if (aspNetCoreTarget != null && !aspNetCoreTarget.DesktopApplication.IsDefault) {
				aspNetCoreTarget.DesktopApplication.Launch (launchUri.AbsoluteUri);
			} else {
				IdeServices.DesktopService.ShowUrl (launchUri.AbsoluteUri);
			}
		}
	}
}
