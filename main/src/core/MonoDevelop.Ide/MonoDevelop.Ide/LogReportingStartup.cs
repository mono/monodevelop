// 
// LogReportingStartup.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2011, Xamarin Inc.
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
using System.Diagnostics;
using System.IO;
using System.Linq;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.LogReporting;


namespace MonoDevelop.Ide
{
	public class LogReportingStartup : CommandHandler
	{
		static bool ShouldPromptToOptIn = Environment.GetEnvironmentVariable ("MONODEVELOP_TEST_CRASH_REPORTING") == "prompt";

		protected override void Run ()
		{
			// Attach a handler for when exceptions need to be processed
			LoggingService.UnhandledErrorOccured = (enabled, ex, willShutdown) => {
				var doNotSend = new AlertButton (GettextCatalog.GetString ("Do _Not Send"));
				var sendOnce = new AlertButton (GettextCatalog.GetString ("_Send This Time"));
				var alwaysSend = new AlertButton (GettextCatalog.GetString ("_Always Send"));
				
				string message = null;
				string title = willShutdown
					? GettextCatalog.GetString ("A fatal error has occurred")
					: GettextCatalog.GetString ("An error has occurred");

				if (!ShouldPromptToOptIn && enabled.GetValueOrDefault ()) {
					if (willShutdown) {
						message = GettextCatalog.GetString (
							"Details of this error have been automatically sent to Microsoft for analysis.");
						message += GettextCatalog.GetString (" {0} will now close.", BrandingService.ApplicationName);
						MessageService.ShowError (null, title, message, ex, false, AlertButton.Ok);
					}
					return enabled;
				}

				message = GettextCatalog.GetString (
					"Details of errors, along with anonymous usage information, can be sent to Microsoft to " +
					"help improve {0}. Do you wish to send this information?", BrandingService.ApplicationName);
				var result = MessageService.ShowError (null, title, message, ex, false, doNotSend, sendOnce, alwaysSend);

				if (result == sendOnce) {
					return null;
				} else if (result == alwaysSend) {
					return true;
				} else {
					return false;
				}
			};
		}
	}
}

