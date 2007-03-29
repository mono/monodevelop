
using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Projects
{
	class ProjectConvertTool: IApplication
	{
		public int Run (string[] arguments)
		{
			if (arguments.Length == 0 || arguments.Length > 2 || arguments [0] == "--help") {
				Console.WriteLine ("Project Conversion Tool");
				Console.WriteLine ("Usage: mdtool project-convert <source-project-file> [format name]");
				return 0;
			}
			if (!File.Exists (arguments[0])) {
				Console.WriteLine ("File {0} not found.", arguments [0]);
				return 1;
			}
			
			IProgressMonitor monitor = new ConsoleProgressMonitor ();
			CombineEntry entry = Services.ProjectService.ReadCombineEntry (arguments [0], monitor);
			IFileFormat[] formats = Services.ProjectService.FileFormats.GetFileFormatsForObject (entry);
			
			if (formats.Length == 0) {
				Console.WriteLine ("Can't convert file to any format: " + arguments [0]);
				return 1;
			}
			
			IFileFormat format = null;
			
			if (arguments.Length == 1) {
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
					if (f.Name == arguments[1])
						format = f;
				}
				if (format == null) {
					Console.WriteLine ("Invalid file format: " + arguments [1]);
					return 1;
				}
			}
			
			entry.FileFormat = format;
			string file = format.GetValidFormatName (arguments [0]);
			if (file == arguments [0]) {
				string ext = Path.GetExtension (arguments [0]);
				file = file.Substring (0, file.Length - ext.Length);
				file += ".converted" + ext;
			}
			entry.Save (file, monitor);
			Console.WriteLine ("Saved file: " + file);
			return 0;
		}
	}
}
