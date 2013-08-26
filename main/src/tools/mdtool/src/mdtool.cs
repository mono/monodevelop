//
// mdtool.cs
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
using MonoDevelop.Core.Logging;

public class MonoDevelopProcessHost
{
	public static int Main (string[] args)
	{
		try {
			Runtime.SetProcessName ("mdtool");

			EnabledLoggingLevel verbosity = EnabledLoggingLevel.Fatal;
			bool regUpdate = true;
			bool listTools = false;
			bool showHelp = false;
			bool badInput = false;

			//read the arguments to mdtool itself
			int pi = 0;
			while (pi < args.Length && (args[pi][0] == '-' || args[pi][0] == '/')) {
				string arg = args[pi];
				pi++;
				switch (arg) {
				case "-v":
				case "/v":
				case "--verbose":
				case "/verbose":
					verbosity = (EnabledLoggingLevel)((int)verbosity << 1) | EnabledLoggingLevel.Fatal;
					continue;
				case "-q":
				case "/q":
					listTools = true;
					continue;
				case "--no-reg-update":
				case "/no-reg-update":
				case "-nru":
				case "/nru":
					regUpdate = false;
					continue;
				case "-h":
				case "/h":
				case "--help":
				case "/help":
					showHelp = true;
					continue;
				default:
					Console.Error.WriteLine ("Unknown argument '{0}'", arg);
					badInput = true;
					break;
				}
			}

			//determine the tool name and arguments
			string toolName = null;
			string[] toolArgs = null;
			if (!showHelp && !listTools && !badInput) { 
				if (pi < args.Length) {
					toolName = args[pi];
					toolArgs = new string [args.Length - 1 - pi];
					Array.Copy (args, pi + 1, toolArgs, 0, args.Length - 1 - pi);
				} else if (args.Length == 0) {
					showHelp = true;
				} else {
					Console.Error.WriteLine ("No tool specified");
					badInput = true;
				}
			}

			//setup app needs to skip runtime initialization or we get addin engine races
			if (toolName == "setup") {
				return RunSetup (toolArgs);
			}

			// Only log fatal errors unless verbosity is specified. Command line tools should already
			// be providing feedback using the console.
			var logger = (ConsoleLogger)LoggingService.GetLogger ("ConsoleLogger");
			logger.EnabledLevel = verbosity;

			Runtime.Initialize (regUpdate);

			if (showHelp || badInput) {
				ShowHelp (badInput);
				return badInput? 1 : 0;
			}

			var tool = Runtime.ApplicationService.GetApplication (toolName);
			if (tool == null) {
				Console.Error.WriteLine ("Tool '{0}' not found.", toolName);
				listTools = true;
				badInput = true;
			}

			if (listTools) {
				ShowAvailableTools ();
				return badInput? 1 : 0;
			}

			return tool.Run (toolArgs);
		} catch (UserException ex) {
			Console.WriteLine (ex.Message);
			return -1;
		} catch (Exception ex) {
			LoggingService.LogFatalError (ex.ToString ());
			return -1;
		} finally {
			try {
				Runtime.Shutdown ();
			} catch {
				// Ignore shutdown exceptions
			}
		}
	}

	static void ShowHelp (bool shortHelp)
	{
		if (shortHelp) {
			Console.WriteLine ();
			Console.WriteLine ("Run `mdtool --help` to show usage information.");
			Console.WriteLine ();
			return;
		}
		Console.WriteLine ();
		Console.WriteLine ("MonoDevelop Tool Runner");
		Console.WriteLine ();
		Console.WriteLine ("Usage: mdtool [options] <tool> ... : Runs a tool.");
		Console.WriteLine ("       mdtool setup ... : Runs the setup utility.");
		Console.WriteLine ("       mdtool -q : Lists available tools.");
		Console.WriteLine ();
		Console.WriteLine ("Options:");
		Console.WriteLine ("  --verbose (-v)   Increases log verbosity. Can be used multiple times.");
		Console.WriteLine ("  --no-reg-update  Skip updating addin registry. Faster but results in");
		Console.WriteLine ("                   random errors if registry is not up to date.");
		ShowAvailableTools ();
	}
	
	static int RunSetup (string[] args)
	{
		Console.WriteLine ("MonoDevelop Add-in Setup Utility");
		bool verbose = false;
		foreach (string a in args)
			if (a == "-v")
				verbose = true;
	
		string startupDir, configDir, addinsDir, databaseDir;
		string asmFile = new Uri (System.Reflection.Assembly.GetEntryAssembly ().CodeBase).LocalPath;
		startupDir = System.IO.Path.GetDirectoryName (asmFile);
		Runtime.GetAddinRegistryLocation (out configDir, out addinsDir, out databaseDir);
		SetupTool setupTool = new SetupTool (new AddinRegistry (configDir, startupDir, addinsDir, databaseDir));
		setupTool.VerboseOutput = verbose;
		return setupTool.Run (args);
	}

	static void ShowAvailableTools ()
	{
		Console.WriteLine ();
		Console.WriteLine ("Available tools:");
		foreach (IApplicationInfo ainfo in Runtime.ApplicationService.GetApplications ()) {
			Console.Write ("- " + ainfo.Id);
			if (!string.IsNullOrEmpty (ainfo.Description))
				Console.WriteLine (": " + ainfo.Description);
			else
				Console.WriteLine ();
		}
		Console.WriteLine ();
	}

}
