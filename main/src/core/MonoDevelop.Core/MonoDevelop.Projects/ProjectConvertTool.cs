// ProjectConvertTool.cs
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
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Projects
{
	class ProjectConvertTool: IApplication
	{
		public int Run (string[] arguments)
		{
			if (arguments.Length == 0 || arguments [0] == "--help") {
				Console.WriteLine ("");
				Console.WriteLine ("Project Export Tool");
				Console.WriteLine ("Usage: mdtool project-export <source-project-file> [-d:dest-path] [-f:format-name]");
				Console.WriteLine ("");
				Console.WriteLine ("Options");
				Console.WriteLine ("  -d:<dest-path>      Directory where the project will be exported.");
				Console.WriteLine ("  -f:\"<format-name>\"  Format to which export the project or solution.");
				Console.WriteLine ("  -l                  Show a list of all allowed target formats.");
				Console.WriteLine ("  -p:<project-name>   When exporting a solution, name of a project to be");
				Console.WriteLine ("                      included in the export. It can be specified multiple");
				Console.WriteLine ("                      times.");
				Console.WriteLine ("");
				Console.WriteLine ("  The format name is optional. A list of allowed file formats will be");
				Console.WriteLine ("  shown if none is provided.");
				Console.WriteLine ("");
				return 0;
			}
			
			string projectFile = null;
			string destPath = null;
			string formatName = null;
			bool formatList = false;
			List<string> projects = new List<string> ();
			string[] itemsToExport = null;
			
			foreach (string s in arguments)
			{
				if (s.StartsWith ("-d:"))
					destPath = s.Substring (3);
				else if (s.StartsWith ("-f:"))
					formatName = s.Substring (3);
				else if (s.StartsWith ("-p:"))
					projects.Add (s.Substring (3));
				else if (s == "-l")
					formatList = true;
				else if (projectFile != null) {
					Console.WriteLine ("Only one project can be converted at a time.");
					return 1;
				}
				else
					projectFile = s;
			}
			
			if (projectFile == null) {
				Console.WriteLine ("Project or solution file name not provided.");
				return 1;
			}
			
			projectFile = FileService.GetFullPath (projectFile);
			if (!File.Exists (projectFile)) {
				Console.WriteLine ("File {0} not found.", projectFile);
				return 1;
			}
			
			ConsoleProgressMonitor monitor = new ConsoleProgressMonitor ();
			monitor.IgnoreLogMessages = true;
			
			
			object item;
			if (Services.ProjectService.IsWorkspaceItemFile (projectFile)) {
				item = Services.ProjectService.ReadWorkspaceItem (monitor, projectFile);
				if (projects.Count > 0) {
					Solution sol = item as Solution;
					if (sol == null) {
						Console.WriteLine ("The -p option can only be used when exporting a solution.");
						return 1;
					}
					for (int n=0; n<projects.Count; n++) {
						string pname = projects [n];
						if (pname.Length == 0) {
							Console.WriteLine ("Project name not specified in -p option.");
							return 1;
						}
						Project p = sol.FindProjectByName (pname);
						if (p == null) {
							Console.WriteLine ("Project '" + pname + "' not found in solution.");
							return 1;
						}
						projects[n] = p.ItemId;
					}
					itemsToExport = projects.ToArray ();
				}
			}
			else {
				if (projects.Count > 0) {
					Console.WriteLine ("The -p option can't be used when exporting a single project");
					return 1;
				}
				item = Services.ProjectService.ReadSolutionItem (monitor, projectFile);
			}
			
			FileFormat[] formats = Services.ProjectService.FileFormats.GetFileFormatsForObject (item);
			
			if (formats.Length == 0) {
				Console.WriteLine ("Can't convert file to any format: " + projectFile);
				return 1;
			}
			
			FileFormat format = null;
			
			if (formatName == null || formatList) {
				Console.WriteLine ();
				Console.WriteLine ("Target formats:");
				for (int n=0; n<formats.Length; n++)
					Console.WriteLine ("  {0}. {1}", n + 1, formats [n].Name);
				Console.WriteLine ();
				if (formatList)
					return 0;
				
				int op = 0;
				do {
					Console.Write ("Convert to format: ");
					string s = Console.ReadLine ();
					if (s.Length == 0)
						return 1;
					if (int.TryParse (s, out op)) {
						if (op > 0 && op <= formats.Length)
							break;
					}
				} while (true);
				
				format = formats [op - 1];
			}
			else {
				foreach (FileFormat f in formats) {
					if (f.Name == formatName)
						format = f;
				}
				if (format == null) {
					Console.WriteLine ("Unknown file format: " + formatName);
					return 1;
				}
			}
			
			if (destPath == null)
				destPath = Path.GetDirectoryName (projectFile);
			destPath = FileService.GetFullPath (destPath);
			
			string ofile = Services.ProjectService.Export (monitor, projectFile, itemsToExport, destPath, format);
			if (ofile != null) {
				Console.WriteLine ("Saved file: " + ofile);
				return 0;
			}
			else {
				Console.WriteLine ("Project export failed.");
				return 1;
			}
		}
	}
}
