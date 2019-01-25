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

	public class LaunchServicesException : Exception
	{
		public LaunchServicesException (int errorCode)
			 : base ("Failed to start process: " + LookupErrorMessage (errorCode))
		{
		}

		static string LookupErrorMessage (int errorCode)
		{
			switch (errorCode) {
			case -10660: return "Can not launch applications from trash folder";
			case -10661: return "Incorrect executable";
			case -10662: return "Attribute not found";
			case -10663: return "The attribute not settable";
			case -10664: return "Incompatible application version";
			case -10665: return "Required Rosetta environment not found";
			case -10810: return "Unknown error";
			case -10811: return "Not an application";
			case -10813: return "Data Unavailable";
			case -10814: return "Application not found";
			case -10815: return "Unknown item type";
			case -10816: return "Data too old";
			case -10817: return "Data error";
			case -10818: return "Launch in progress";
			case -10819: return "Not registered";
			case -10820: return "App does not claim type";
			case -10821: return "App does not support scheme";
			case -10822: return "Server communication error";
			case -10823: return "Cannot set info";
			case -10824: return "No registration info";
			case -10825: return "App is incompatible with the system version";
			case -10826: return "No launch permission";
			case -10827: return "Executable is missing";
			case -10828: return "Required Classic environment not found";
			case -10829: return "Multiple sessions not supported";
			default:
				return String.Format ("Unknown LaunchServices return code {0}", errorCode);
			}
		}
	}
	
	public static class LaunchServices
	{
		public static int OpenApplication (string application)
		{
			return OpenApplication (new ApplicationStartInfo (application));
		}

		// This function can be replaced by NSWorkspace.LaunchApplication but it currently doesn't work
		// https://bugzilla.xamarin.com/show_bug.cgi?id=32540

		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		static extern IntPtr IntPtr_objc_msgSend_IntPtr_UInt32_IntPtr_IntPtr (IntPtr receiver, IntPtr selector, IntPtr url, UInt32 options, IntPtr configuration, out IntPtr error);
		static readonly IntPtr launchApplicationAtURLOptionsConfigurationErrorSelector = ObjCRuntime.Selector.GetHandle ("launchApplicationAtURL:options:configuration:error:");
		public static int OpenApplication (ApplicationStartInfo application)
		{
			if (application == null)
				throw new ArgumentNullException ("application");

			if (string.IsNullOrEmpty (application.Application) || !System.IO.Directory.Exists (application.Application))
				throw new ArgumentException ("Application is not valid");

			NSUrl appUrl = NSUrl.FromFilename (application.Application);

			// TODO: Once the above bug is fixed, we can replace the code below with
			//NSRunningApplication app = NSWorkspace.SharedWorkspace.LaunchApplication (appUrl, 0, new NSDictionary (), null);

			var config = new NSMutableDictionary ();
			if (application.Args != null && application.Args.Length > 0) {
				var args = new NSMutableArray ();
				foreach (string arg in application.Args) {
					args.Add (new NSString (arg));
				}
				config.Add (new NSString ("NSWorkspaceLaunchConfigurationArguments"), args);
			}

			if (application.Environment != null && application.Environment.Count > 0) {
				var envValueStrings = application.Environment.Values.Select (t => new NSString (t)).ToArray ();
				var envKeyStrings = application.Environment.Keys.Select (t => new NSString (t)).ToArray ();

				var envDict = new NSMutableDictionary ();
				for (int i = 0; i < envValueStrings.Length; i++) {
					envDict.Add (envKeyStrings[i], envValueStrings[i]);
				}

				config.Add (new NSString ("NSWorkspaceLaunchConfigurationEnvironment"), envDict);
			}

			UInt32 options = 0;

			if (application.Async)
				options |= (UInt32) LaunchOptions.NSWorkspaceLaunchAsync;
			if (application.NewInstance)
				options |= (UInt32) LaunchOptions.NSWorkspaceLaunchNewInstance;
			if (application.HideFromRecentApps)
				options |= (UInt32) LaunchOptions.NSWorkspaceLaunchWithoutAddingToRecents;

			IntPtr error;
			var appHandle = IntPtr_objc_msgSend_IntPtr_UInt32_IntPtr_IntPtr (NSWorkspace.SharedWorkspace.Handle, launchApplicationAtURLOptionsConfigurationErrorSelector, appUrl.Handle, options, config.Handle, out error);
			if (appHandle == IntPtr.Zero)
				return -1;

			NSRunningApplication app = (NSRunningApplication)ObjCRuntime.Runtime.GetNSObject (appHandle);

			return app.ProcessIdentifier;
		}

		[Flags]
		enum LaunchOptions {
			NSWorkspaceLaunchAndPrint = 0x00000002,
			NSWorkspaceLaunchWithErrorPresentation = 0x00000040,
			NSWorkspaceLaunchInhibitingBackgroundOnly = 0x00000080,
			NSWorkspaceLaunchWithoutAddingToRecents = 0x00000100,
			NSWorkspaceLaunchWithoutActivation = 0x00000200,
			NSWorkspaceLaunchAsync = 0x00010000,
			NSWorkspaceLaunchAllowingClassicStartup = 0x00020000,
			NSWorkspaceLaunchPreferringClassic = 0x00040000,
			NSWorkspaceLaunchNewInstance = 0x00080000,
			NSWorkspaceLaunchAndHide = 0x00100000,
			NSWorkspaceLaunchAndHideOthers = 0x00200000,
			NSWorkspaceLaunchDefault = NSWorkspaceLaunchAsync | NSWorkspaceLaunchAllowingClassicStartup
		};
	}
}
