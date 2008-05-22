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

namespace MonoDevelop.Projects
{
	internal class BuildTool : IApplication
	{
		bool help;
		string file;
		string project;
		string command = "build";
		
		public int Run (string[] arguments)
		{
			Console.WriteLine ("MonoDevelop Build Tool");
			foreach (string s in arguments)
				ReadArgument (s);
			
			if (help) {
				Console.WriteLine ("build [options] [project-file]");
				Console.WriteLine ("--f --buildfile:FILE   Project or solution file to build.");
				Console.WriteLine ("--p --project:PROJECT  Name of the project to build.");
				Console.WriteLine ();
				return 0;
			}
			
			if (file == null) {
				string[] files = Directory.GetFiles (".", "*.mds");
				if (files.Length == 0)
					files = Directory.GetFiles (".", "*.mdp");
				if (files.Length == 0) {
					Console.WriteLine ("Project file not found.");
					return 1;
				}
				file = files [0];
			}
			
			ConsoleProgressMonitor monitor = new ConsoleProgressMonitor ();
			
			IBuildTarget item = Services.ProjectService.ReadWorkspaceItem (monitor, file);
			
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
			
			if (command == "build") {
				BuildResult res = item.RunTarget (monitor, ProjectService.BuildTarget, ProjectService.DefaultConfiguration);
				return (res.ErrorCount == 0) ? 0 : 1;
			}
			else if (command == "clean") {
				item.RunTarget (monitor, ProjectService.CleanTarget, ProjectService.DefaultConfiguration);
				return 0;
			} else {
				Console.WriteLine ("Unknown command '{0}'", command);
				return 1;
			}
		}
		
		void ReadArgument (string argument)
		{
			string optionValuePair;
			
			if (argument.StartsWith("--")) {
				optionValuePair = argument.Substring(2);
			}
			else if (argument.StartsWith("/") || argument.StartsWith("-")) {
				optionValuePair = argument.Substring(1);
			}
			else {
				command = argument;
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
				    project = value;
				    break;

				default:
				    throw new Exception("Unknown option '" + option + "'");
			}
		}
	}
}
