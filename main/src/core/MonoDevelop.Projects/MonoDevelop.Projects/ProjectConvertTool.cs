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
			
			foreach (string s in arguments)
			{
				if (s.StartsWith ("-d:"))
					destPath = s.Substring (3);
				else if (s.StartsWith ("-f:"))
					formatName = s.Substring (3);
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
			SolutionEntityItem entry = Services.ProjectService.ReadSolutionItem (monitor, projectFile);
			FileFormat[] formats = Services.ProjectService.FileFormats.GetFileFormatsForObject (entry);
			
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
			
			string ofile = Services.ProjectService.Export (monitor, projectFile, destPath, format);
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
