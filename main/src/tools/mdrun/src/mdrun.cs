//
// mdrun.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using Mono.Addins;
using Mono.Addins.Setup;
using System.IO;
using System.Collections;

public class MonoDevelopProcessHost
{
	public static int Main (string[] args)
	{
		try {
			Runtime.Initialize (false);
			Runtime.SetProcessName ("mdtool");
			
			if (args.Length == 0 || args [0] == "--help") {
				Console.WriteLine ();
				Console.WriteLine ("MonoDevelop Tool Runner");
				Console.WriteLine ();
				Console.WriteLine ("Usage: mdtool [options] <tool> ... : Runs a tool.");
				Console.WriteLine ("       mdtool setup ... : Runs the setup utility.");
				Console.WriteLine ("       mdtool -q : Lists available tools.");
				Console.WriteLine ();
				Console.WriteLine ("Options:");
				Console.WriteLine ("  --verbose (-v): Enable verbose log.");
				Console.WriteLine ();
				ShowAvailableApps ();
				return 0;
			}

			if (args [0] == "-q") {
				ShowAvailableApps ();
				return 0;
			}
			
			// Don't log to console unless verbose log is requested
			int pi = 0;
			if (args [0] == "-v" || args [0] == "--verbose")
				pi++;
			else
				LoggingService.RemoveLogger ("ConsoleLogger");
			
			string[] newArgs = new string [args.Length - 1 - pi];
			Array.Copy (args, pi + 1, newArgs, 0, args.Length - 1 - pi);
			if (args [pi] != "setup")
				return Runtime.ApplicationService.StartApplication (args[pi], newArgs);
			else
				return RunSetup (newArgs);
		} catch (UserException ex) {
			Console.WriteLine (ex.Message);
			return -1;
		} catch (Exception ex) {
			Console.WriteLine (ex);
			return -1;
		} finally {
			try {
				Runtime.Shutdown ();
			} catch {
				// Ignore shutdown exceptions
			}
		}
	}
	
	static int RunSetup (string[] args)
	{
		Console.WriteLine ("MonoDevelop Add-in Setup Utility");
		bool verbose = false;
		foreach (string a in args)
			if (a == "-v")
				verbose = true;
	
		SetupTool setupTool = new SetupTool (AddinManager.Registry);
		setupTool.VerboseOutput = verbose;
		return setupTool.Run (args);
	}

	static void ShowAvailableApps ()
	{
		Console.WriteLine ("Available tools:");
		foreach (IApplicationInfo ainfo in Runtime.ApplicationService.GetApplications ()) {
			Console.Write ("- " + ainfo.Id);
			if (ainfo.Description != null && ainfo.Description.Length > 0)
				Console.WriteLine (": " + ainfo.Description);
			else
				Console.WriteLine ();
		}
		Console.WriteLine ();
	}

}
