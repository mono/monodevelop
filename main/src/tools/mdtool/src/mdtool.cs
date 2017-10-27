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
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

class MonoDevelopProcessHost
{
	[STAThread]
	public static int Main (string[] args)
	{
		try {
			var sc = new ConsoleSynchronizationContext ();
			SynchronizationContext.SetSynchronizationContext (sc);

			string exeName = Path.GetFileNameWithoutExtension (Assembly.GetEntryAssembly ().Location);
			if (!Platform.IsMac && !Platform.IsWindows)
				exeName = exeName.ToLower ();

			Runtime.SetProcessName (exeName);

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
				ShowHelp (badInput, exeName);
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

			var task = tool.Run (toolArgs);
			task.ContinueWith ((t) => sc.ExitLoop ());
			sc.RunMainLoop ();
			return task.Result;

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

	static void ShowHelp (bool shortHelp, string exeName)
	{
		if (shortHelp) {
			Console.WriteLine ();
			Console.WriteLine ("Run `{0} --help` to show usage information.", exeName);
			Console.WriteLine ();
			return;
		}
		Console.WriteLine ();
		Console.WriteLine (BrandingService.BrandApplicationName ("MonoDevelop Tool Runner"));
		Console.WriteLine ();
		Console.WriteLine ("Usage: {0} [options] <tool> ... : Runs a tool.", exeName);
		Console.WriteLine ("       {0} setup ... : Runs the setup utility.", exeName);
		Console.WriteLine ("       {0} -q : Lists available tools.", exeName);
		Console.WriteLine ();
		Console.WriteLine ("Options:");
		Console.WriteLine ("  --verbose (-v)   Increases log verbosity. Can be used multiple times.");
		Console.WriteLine ("  --no-reg-update  Skip updating extension registry. Faster but results in");
		Console.WriteLine ("                   random errors if registry is not up to date.");
		ShowAvailableTools ();
	}
	
	static int RunSetup (string[] args)
	{
		Console.WriteLine (BrandingService.BrandApplicationName ("MonoDevelop Extension Setup Utility"));
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

	class ConsoleSynchronizationContext: SynchronizationContext
	{
		// This class implements a threading context based on a basic message loop, which emulates the
		// behavior of a normal UI loop. This is necessary since there is no UI loop when running mdtool.

		Queue<Tuple<SendOrPostCallback,object>> work = new Queue<Tuple<SendOrPostCallback, object>> ();
		bool endLoop;

		public override void Post (SendOrPostCallback d, object state)
		{
			lock (work) {
				work.Enqueue (new Tuple<SendOrPostCallback, object> (d, state));
				Monitor.Pulse (work);
			}
		}

		public override void Send (SendOrPostCallback d, object state)
		{
			var evt = new ManualResetEventSlim (false);
			Exception exception = null;
			Post (s => {
				try {
					d.Invoke (state);
				} catch (Exception ex) {
					exception = ex;
				} finally {
					Thread.MemoryBarrier ();
					evt.Set ();
				}
			}, null);
			evt.Wait ();
			if (exception != null)
				throw exception;
		}

		public void RunMainLoop ()
		{
			do {
				Tuple<SendOrPostCallback,object> next = null;
				lock (work) {
					if (work.Count > 0 && !endLoop)
						next = work.Dequeue ();
					else if (!endLoop)
						Monitor.Wait (work);
				}
				if (next != null) {
					try {
						next.Item1 (next.Item2);
					} catch (Exception ex) {
						Console.WriteLine (ex);
					}
				}
			} while (!endLoop);
		}

		public void ExitLoop ()
		{
			lock (work) {
				endLoop = true;
				Monitor.Pulse (work);
			}
		}
	}
}
