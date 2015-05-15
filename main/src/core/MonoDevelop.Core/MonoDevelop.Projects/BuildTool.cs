//
// BuildTool.cs
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
using System.IO;
using MonoDevelop.Projects;
using Mono.Addins;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	internal class BuildTool : IApplication
	{
		bool help;
		string file;
		string project;
		string config = null;
		string command = ProjectService.BuildTarget;
		string runtime;
		
		public async Task<int> Run (string[] arguments)
		{
			Console.WriteLine (BrandingService.BrandApplicationName ("MonoDevelop Build Tool"));
			foreach (string s in arguments)
				ReadArgument (s);
			
			if (help) {
				Console.WriteLine ("build [options] [build-file]");
				Console.WriteLine ("-p --project:PROJECT  Name of the project to build.");
				Console.WriteLine ("-t --target:TARGET    Name of the target: Build or Clean.");
				Console.WriteLine ("-c --configuration:CONFIGURATION  Name of the solution configuration to build.");
				Console.WriteLine ("-r --runtime:PREFIX   Prefix of the Mono runtime to build against.");
				Console.WriteLine ();
				Console.WriteLine ("Supported targets:");
				Console.WriteLine ("  {0}: build the project (the default target).", ProjectService.BuildTarget);
				Console.WriteLine ("  {0}: clean the project.", ProjectService.CleanTarget);
				Console.WriteLine ();
				return 0;
			}
			
			string solFile = null;
			string itemFile = null;
			
			if (file == null) {
				string[] files = Directory.GetFiles (".");
				foreach (string f in files) {
					if (Services.ProjectService.IsWorkspaceItemFile (f)) {
						solFile = f;
						break;
					} else if (itemFile == null && Services.ProjectService.IsSolutionItemFile (f))
						itemFile = f;
				}
				if (solFile == null && itemFile == null) {
					Console.WriteLine ("Project file not found.");
					return 1;
				}
			} else {
				if (Services.ProjectService.IsWorkspaceItemFile (file))
				    solFile = file;
				else if (Services.ProjectService.IsSolutionItemFile (file))
					itemFile = file;
				else {
					Console.WriteLine ("File '{0}' is not a project or solution.", file);
					return 1;
				}
			}
			
			ProgressMonitor monitor = new ConsoleProjectLoadProgressMonitor (new ConsoleProgressMonitor ());

			TargetRuntime targetRuntime = null;
			TargetRuntime defaultRuntime = Runtime.SystemAssemblyService.DefaultRuntime;
			if (runtime != null)
			{
				targetRuntime = MonoTargetRuntimeFactory.RegisterRuntime(new MonoRuntimeInfo(runtime));
				if (targetRuntime != null)
					Runtime.SystemAssemblyService.DefaultRuntime = targetRuntime;
			}

			IBuildTarget item;
			if (solFile != null)
				item = await Services.ProjectService.ReadWorkspaceItem (monitor, solFile) as IBuildTarget;
			else
				item = await Services.ProjectService.ReadSolutionItem (monitor, itemFile);

			if (item == null) {
				Console.WriteLine ("The file '" + file + "' can't be built");
				return 1;
			}

			using (var readItem = (WorkspaceObject)item) {
				if (project != null) {
					Solution solution = item as Solution;
					item = null;
					
					if (solution != null) {
						item = solution.FindProjectByName (project);
					}
					if (item == null) {
						Console.WriteLine ("The project '" + project + "' could not be found in " + file);
						return 1;
					}
				}

				IConfigurationTarget configTarget = item as IConfigurationTarget;
				if (config == null && configTarget != null)
					config = configTarget.DefaultConfigurationId;
				
				monitor = new ConsoleProgressMonitor ();
				BuildResult res = null;
				if (item is SolutionItem && ((SolutionItem)item).ParentSolution == null) {
					ConfigurationSelector configuration = new ItemConfigurationSelector (config);
					if (command == ProjectService.BuildTarget)
						res = await item.Build (monitor, configuration);
					else if (command == ProjectService.CleanTarget)
						res = await item.Clean (monitor, configuration);
				} else {
					ConfigurationSelector configuration = new SolutionConfigurationSelector (config);

					if (command == ProjectService.BuildTarget)
						res = await item.Build (monitor, configuration, true);
					else if (command == ProjectService.CleanTarget)
						res = await item.Clean (monitor, configuration);
					else {
						var p = item as Project;
						if (p != null) {
							res = (await p.RunTarget (monitor, command, configuration)).BuildResult;
						} else {
							Console.WriteLine ("Target '" + command + " not supported");
							return 1;
						}
					}
				}

				if (targetRuntime != null)
				{
					Runtime.SystemAssemblyService.DefaultRuntime = defaultRuntime;
					MonoTargetRuntimeFactory.UnregisterRuntime((MonoTargetRuntime) targetRuntime);
				}

				if (res != null) {
					foreach (var err in res.Errors) {
						Console.Error.WriteLine (err);
					}
				}

				return (res == null || res.ErrorCount == 0) ? 0 : 1;
			}
		}
		
		void ReadArgument (string argument)
		{
			string optionValuePair;
			
			if (argument.StartsWith("--")) {
				optionValuePair = argument.Substring(2);
			}
			else if ((argument.StartsWith("/") || argument.StartsWith("-")) && !File.Exists (argument)) {
				optionValuePair = argument.Substring(1);
			}
			else {
				file = argument;
				return;
			}
			
			string option;
			string value;
			
			int indexOfEquals = optionValuePair.IndexOf(':');
			if (indexOfEquals > 0) {
				option = optionValuePair.Substring(0, indexOfEquals);
				value = optionValuePair.Substring(indexOfEquals + 1);
			}
			else {
				option = optionValuePair;
				value = null;
			}
			
			switch (option)
			{
				case "f":
				case "buildfile":
				    file = value;
				    break;

				case "help":
				case "?":
				    help = true;
				    break;

				case "p":
				case "project":
					if (string.IsNullOrEmpty (value))
						throw new Exception ("Project name not specified (syntax is: -p:PROJECT)");
				    project = value;
				    break;

				case "c":
				case "configuration":
					if (string.IsNullOrEmpty (value))
						throw new Exception ("Configuration name not specified (syntax is: -c:CONFIGURATION)");
				    config = value;
				    break;

				case "t":
				case "target":
					if (string.IsNullOrEmpty (value))
						throw new Exception ("Target name not specified (syntax is: -t:TARGET)");
				    command = value;
				    break;

				case "r":
				case "runtime":
					if (string.IsNullOrEmpty (value))
						throw new Exception ("Runtime prefix not specified (syntax is: -r:PREFIX)");
					runtime = value;
					break;

				default:
				    throw new Exception("Unknown option '" + option + "'");
			}
		}
	}
}
