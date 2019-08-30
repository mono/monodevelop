// 
// LaunchServices.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

using MonoDevelop.Core;
using Foundation;
using AppKit;

namespace MonoDevelop.MacInterop
{
	public class ApplicationStartInfo
	{
		public ApplicationStartInfo (string application)
		{
			this.Application = application;
			this.Environment = new Dictionary<string, string> ();
		}
		
		public string Application { get; set; }
		public Dictionary<string,string> Environment { get; private set; }
		public string[] Args { get; set; }
		public bool Async { get; set; }
		public bool NewInstance { get; set; }
		public bool HideFromRecentApps { get; set; }
	}
	
	public static class LaunchServices
	{
		public static int OpenApplication (string application)
			=> OpenApplication (new ApplicationStartInfo (application));

		public static int OpenApplication (ApplicationStartInfo application)
			=> OpenApplicationInternal (application)?.ProcessIdentifier ?? -1;

		internal static NSRunningApplication OpenApplicationInternal (ApplicationStartInfo application)
		{
			if (application == null)
				throw new ArgumentNullException (nameof (application));

			if (string.IsNullOrEmpty (application.Application) || !System.IO.Directory.Exists (application.Application))
				throw new ArgumentException ("Application is not valid", nameof(application));

			NSUrl appUrl = NSUrl.FromFilename (application.Application);

			var config = new NSMutableDictionary ();
			if (application.Args != null && application.Args.Length > 0) {
				config.Add (NSWorkspace.LaunchConfigurationArguments, NSArray.FromStrings (application.Args));
			}

			if (application.Environment != null && application.Environment.Count > 0) {
				var envValueStrings = application.Environment.Values.Select (t => new NSString (t)).ToArray ();
				var envKeyStrings = application.Environment.Keys.Select (t => new NSString (t)).ToArray ();

				config.Add (NSWorkspace.LaunchConfigurationEnvironment, NSDictionary.FromObjectsAndKeys (envValueStrings, envKeyStrings));
			}

			NSWorkspaceLaunchOptions options = 0;

			if (application.Async)
				options |= NSWorkspaceLaunchOptions.Async;
			if (application.NewInstance)
				options |= NSWorkspaceLaunchOptions.NewInstance;
			if (application.HideFromRecentApps)
				options |= NSWorkspaceLaunchOptions.WithoutAddingToRecents;

			var app = NSWorkspace.SharedWorkspace.LaunchApplication (appUrl, options, config, out NSError error);
			if (app == null) {
				LoggingService.LogError (error.LocalizedDescription);
			}

			return app;
		}
	}
}
