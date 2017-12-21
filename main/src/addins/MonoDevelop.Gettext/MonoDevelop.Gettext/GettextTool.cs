// GettextTool.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide;
using System.Threading.Tasks;

namespace MonoDevelop.Gettext
{
	class GettextTool: IApplication
	{
		bool help;
		string file;
		string project;
		bool sort;
		
		public async Task<int> Run (string[] arguments)
		{
			DesktopService.Initialize ();

			Console.WriteLine (BrandingService.BrandApplicationName ("MonoDevelop Gettext Update Tool"));
			foreach (string s in arguments)
				ReadArgument (s);
			
			if (help) {
				Console.WriteLine ("gettext-update [options] [project-file]");
				Console.WriteLine ("--f --file:FILE   Project or solution file to build.");
				Console.WriteLine ("--p --project:PROJECT  Name of the project to build.");
				Console.WriteLine ("--sort  Sorts the output po file");
				Console.WriteLine ();
				return 0;
			}
			
			if (file == null) {
				var files = Directory.EnumerateFiles (".");
				foreach (string f in files) {
					if (Services.ProjectService.IsWorkspaceItemFile (f)) {
						file = f;
						break;
					}
				}
				if (file == null) {
					Console.WriteLine ("Solution file not found.");
					return 1;
				}
			} else if (!Services.ProjectService.IsWorkspaceItemFile (file)) {
				Console.WriteLine ("File '{0}' is not a project or solution.", file);
				return 1;
			}
			
			ConsoleProgressMonitor monitor = new ConsoleProgressMonitor ();
			monitor.IgnoreLogMessages = true;
			
			WorkspaceItem centry = await Services.ProjectService.ReadWorkspaceItem (monitor, file);
			monitor.IgnoreLogMessages = false;
			
			Solution solution = centry as Solution;
			if (solution == null) {
				Console.WriteLine ("File is not a solution: " + file);
				return 1;
			}
			
			if (project != null) {
				SolutionItem item = solution.FindProjectByName (project);
				
				if (item == null) {
					Console.WriteLine ("The project '" + project + "' could not be found in " + file);
					return 1;
				}
				TranslationProject tp = item as TranslationProject;
				if (tp == null) {
					Console.WriteLine ("The project '" + item.FileName + "' is not a translation project");
					return 1;
				}
				tp.UpdateTranslations (monitor, sort);
			}
			else {
				foreach (TranslationProject p in solution.GetAllItems <TranslationProject>())
					p.UpdateTranslations (monitor, sort);
			}
			
			return 0;
		}
		
		void ReadArgument (string argument)
		{
			string optionValuePair;
			
			if (argument.StartsWith("--")) {
				optionValuePair = argument.Substring(2);
			}
			else if (argument.StartsWith("/") || argument.StartsWith("-")) {
				optionValuePair = argument.Substring(1);
			} else
				return;
			
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
				case "file":
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

				case "sort":
					sort = true;
					break;

				default:
				    throw new Exception("Unknown option '" + option + "'");
			}
		}
	}
}
