
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
				Console.WriteLine (" -d:<dest-path>    Directory where the project will be exported.");
				Console.WriteLine (" -f:<format-name>  Format to which export the project or solution.");
				Console.WriteLine ("");
				return 0;
			}
			
			string projectFile = null;
			string destPath = null;
			string formatName = null;
			
			foreach (string s in arguments)
			{
				if (s.StartsWith ("-d:"))
					destPath = s.Substring (3);
				else if (s.StartsWith ("-f:"))
					formatName = s.Substring (3);
				else if (projectFile != null) {
					Console.WriteLine ("Only one project can be converted at a time");
					return 1;
				}
				else
					projectFile = s;
			}
			
			if (projectFile == null) {
				Console.WriteLine ("Project or solution file name not provided");
				return 1;
			}
			
			projectFile = Runtime.FileService.GetFullPath (projectFile);
			if (!File.Exists (projectFile)) {
				Console.WriteLine ("File {0} not found.", projectFile);
				return 1;
			}
			
			IProgressMonitor monitor = new ConsoleProgressMonitor ();
			CombineEntry entry = Services.ProjectService.ReadCombineEntry (projectFile, monitor);
			IFileFormat[] formats = Services.ProjectService.FileFormats.GetFileFormatsForObject (entry);
			
			if (formats.Length == 0) {
				Console.WriteLine ("Can't convert file to any format: " + projectFile);
				return 1;
			}
			
			IFileFormat format = null;
			
			if (formatName == null) {
				Console.WriteLine ();
				Console.WriteLine ("Target formats:");
				for (int n=0; n<formats.Length; n++)
					Console.WriteLine ("  {0}. {1}", n + 1, formats [n].Name);
				Console.WriteLine ();
				
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
				foreach (IFileFormat f in formats) {
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
			destPath = Runtime.FileService.GetFullPath (destPath);
			
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
