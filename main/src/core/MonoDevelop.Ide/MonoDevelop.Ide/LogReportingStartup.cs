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
		protected override void Run ()
		{
			
			// Process cached crash reports if there are any and uploading is enabled
			LogReportingService.ProcessCache ();

			// Attach a handler for when exceptions need to be processed
			LogReportingService.UnhandledErrorOccured = (enabled, ex, willShutdown) => {
				AlertButton[] buttons = null;
				string message = null;
				string title = GettextCatalog.GetString ("An error has occurred");

				if (enabled.HasValue) {
					if (enabled.Value) {
						message = GettextCatalog.GetString ("Details of this error have been automatically submitted for analysis.");
					} else {
						message = GettextCatalog.GetString ("Details of this error have not been submitted as error reporting is disabled.");
					}
					if (willShutdown)
						message += GettextCatalog.GetString (" MonoDevelop will now close.");

					buttons = new [] { AlertButton.Ok };
				} else {
					var part1 = GettextCatalog.GetString ("Details of this error, along with anonymous installation " +
								"information, can be uploaded to Xamarin to help diagnose the issue. " +
							    "Do you wish to automatically upload this information for this and future crashes?");
					var part2 = GettextCatalog.GetString ("This setting can be changed in the 'Log Agent' section of the MonoDevelop preferences.");
					message = string.Format ("{0}{1}{1}{2}", part1, Environment.NewLine, part2);
					buttons = new [] { AlertButton.Never, AlertButton.ThisTimeOnly, AlertButton.Always };
				}

				var result = MessageService.ShowException (ex, message, title, buttons);
				if (enabled.HasValue) {
					// In this case we will not change the value
					return enabled;
				} else if (result == AlertButton.Always) {
					return true;
				} else if (result == AlertButton.Never) {
					return false;
				} else {
					// The user has decided to submit this one only
					return null;
				}
			};
		}
	}
}

