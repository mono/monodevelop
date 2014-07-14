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
using Foundation;

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
		const string APP_SERVICES = "/System/Library/Frameworks/ApplicationServices.framework/Versions/A/ApplicationServices";
		const string CFLIB = "/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation";
		
		[DllImport (APP_SERVICES)]
		static extern OSStatus LSOpenApplication (ref LSApplicationParameters appParams, out ProcessSerialNumber psn);
		
		public static ProcessSerialNumber OpenApplication (string application)
		{
			return OpenApplication (new ApplicationStartInfo (application));
		}
			
		public static ProcessSerialNumber OpenApplication (ApplicationStartInfo application)
		{
			if (application == null)
				throw new ArgumentNullException ("application");
			
			if (string.IsNullOrEmpty (application.Application) || !System.IO.Directory.Exists (application.Application))
				throw new ArgumentException ("Application is not valid");
			
			var appParams = new LSApplicationParameters ();
			if (application.NewInstance)
				appParams.flags |= LSLaunchFlags.NewInstance;
			if (application.Async)
				appParams.flags |= LSLaunchFlags.Async;
			
			NSArray argv = null;
			if (application.Args != null && application.Args.Length > 0) {
				var args = application.Args;
				NSObject[] arr = new NSObject[args.Length];
				for (int i = 0; i < args.Length; i++)
					arr[i] = new NSString (args[i]);
				argv = NSArray.FromNSObjects (arr);
				appParams.argv = argv.Handle;
			}
			
			NSDictionary dict = null;
			if (application.Environment.Count > 0) {
				dict = new NSMutableDictionary ();
				foreach (var kvp in application.Environment)
					dict.SetValueForKey (new NSString (kvp.Value), new NSString (kvp.Key));
				appParams.environment = dict.Handle;
			}
			
			var cfUrl = global::CoreFoundation.CFUrl.FromFile (application.Application);
			ProcessSerialNumber psn;
			
			try {
				appParams.application = Marshal.AllocHGlobal (Marshal.SizeOf (typeof (FSRef)));
			
				if (!CoreFoundation.CFURLGetFSRef (cfUrl.Handle, appParams.application))
					throw new Exception ("Could not create FSRef from CFUrl");
				
				var status = LSOpenApplication (ref appParams, out psn);
				if (status != OSStatus.Ok)
					throw new LaunchServicesException ((int)status);
			} finally {
				if (appParams.application != IntPtr.Zero)
					Marshal.FreeHGlobal (appParams.application);
				appParams.application = IntPtr.Zero;
				if (dict != null)
					dict.Dispose (); //also ensures the NSDictionary is kept alive for the params
				if (argv != null)
					argv.Dispose (); //also ensures the NSArray is kept alive for the params
			}
			
			return psn;
		}
		
		struct LSApplicationParameters
		{
			public IntPtr version; // CFIndex, must be 0
			public LSLaunchFlags flags;
			public IntPtr application; //FSRef *
			public IntPtr asyncLaunchRefCon; // void *
			public IntPtr environment; // CFDictionaryRef
			public IntPtr argv; // CFArrayRef
			public IntPtr initialEvent; // AppleEvent *
		}
		
		[Flags]
		public enum LSLaunchFlags : uint
		{
			Defaults = 0x00000001,
			AndPrint = 0x00000002,
			AndDisplayErrors = 0x00000040,
			InhibitBGOnly = 0x00000080,
			DontAddToRecents = 0x00000100,
			DontSwitch = 0x00000200,
			NoParams = 0x00000800,
			Async = 0x00010000,
			StartClassic = 0x00020000,
			InClassic = 0x00040000,
			NewInstance = 0x00080000,
			AndHide = 0x00100000,
			AndHideOthers = 0x00200000,
			HasUntrustedContents = 0x00400000
		}
		
		static class CoreFoundation
		{
			[DllImport (CFLIB)]
			public static extern bool CFURLGetFSRef (IntPtr urlPtr, IntPtr fsRefPtr);
		}
		
		//this is an 80-byte opaque object
		[StructLayout(LayoutKind.Sequential, Size = 80)]
		struct FSRef
		{
		}
		
		enum OSStatus
		{
			Ok = 0
		}
	}
}
