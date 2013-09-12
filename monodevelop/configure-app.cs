using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace configure
{
	class Program
	{
		const string FSharpVersion = "3.2.16";

		static string[] LinuxPaths = {
			"/usr/lib/monodevelop",
			"/usr/local/monodevelop/lib/monodevelop",
			"/usr/local/lib/monodevelop",
			"/Applications/MonoDevelop.app/Contents/MacOS/lib/",
			"monodevelop",
			"/opt/mono/lib/monodevelop",
			"/Applications/Xamarin Studio.app/Contents/MacOS/lib/monodevelop"
		};

		static string[] WindowsPaths = {
			@"C:\Program Files\Xamarin Studio",
			@"C:\Program Files\MonoDevelop",
			@"C:\Program Files (x86)\Xamarin Studio",
			@"C:\Program Files (x86)\MonoDevelop"
		};

		static string MdCheckFile = "bin/MonoDevelop.Core.dll";

		static void Main (string[] args)
		{
			Console.WriteLine ("MonoDevelop F# add-in configuration script");
			Console.WriteLine ("------------------------------------------");

			string mdDir = null;
			string mdVersion = "4.1.6";

			// Look for the installation directory

			if (File.Exists (GetPath ("../../../monodevelop.pc.in"))) {
				// Local MonoDevelop build directory
				mdDir = GetPath (Environment.CurrentDirectory + "/../../../build");
				if (File.Exists (GetPath (mdDir, "../../main/configure.in")))
					mdVersion = Grep (GetPath (mdDir, "../../main/configure.in"), @"AC_INIT.*?(?<ver>([0-9]|\.)+)", "ver");
			}
			else {
				// Using installed MonoDevelop
				var searchPaths = IsWindows ? WindowsPaths : LinuxPaths;
				mdDir = searchPaths.FirstOrDefault (p => File.Exists (GetPath (p, MdCheckFile)));
				if (mdDir != null) {
					string mdExe = null;
					if (File.Exists (GetPath (mdDir, "XamarinStudio"))) {
						mdExe = GetPath (mdDir, "../../XamarinStudio");
					}
					else if (File.Exists (GetPath (mdDir, "MonoDevelop"))) {
						mdExe = GetPath (mdDir, "../../MonoDevelop");
					}
					if (mdExe != null) {
						var outp = Run (mdExe, "/?").ReadLine ();
						mdVersion = outp.Split (new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last ();
					}
				}
			}

			if (!IsWindows) {
				// Update the makefile. We don't use that on windows
				FileReplace ("Makefile.orig", "Makefile", "INSERT_MDROOT", mdDir);
				FileReplace ("Makefile", "Makefile", "INSERT_MDVERSION4", mdVersion);
				FileReplace ("Makefile", "Makefile", "INSERT_VERSION", FSharpVersion);
			}
			
			if (mdDir != null)
				Console.WriteLine ("MonoDevelop binaries found at: {0}", mdDir);
			else
				Console.WriteLine ("MonoDevelop binaries not found. Continuing anyway");
			
			Console.WriteLine ("Detected version: {0}", mdVersion);
			
			var tag = IsWindows ? "windows" : "local";

			var fsprojFile = "MonoDevelop.FSharpBinding/MonoDevelop.FSharp." + tag + ".fsproj";
			var xmlFile = "MonoDevelop.FSharpBinding/FSharpBinding." + tag + ".addin.xml";

			FileReplace ("MonoDevelop.FSharpBinding/MonoDevelop.FSharp.orig", fsprojFile, "INSERT_FSPROJ_MDROOT", mdDir);
			FileReplace (fsprojFile, fsprojFile, "INSERT_FSPROJ_MDVERSION4", mdVersion);
			FileReplace (fsprojFile, fsprojFile, "INSERT_FSPROJ_MDTAG", tag);
			FileReplace ("MonoDevelop.FSharpBinding/FSharpBinding.addin.xml.orig", xmlFile, "INSERT_FSPROJ_VERSION", FSharpVersion);
			FileReplace (xmlFile, xmlFile, "INSERT_FSPROJ_MDVERSION4", mdVersion);
			FileReplace (xmlFile, xmlFile, "INSERT_FSPROJ_MDTAG", tag);
		}

		public static bool IsWindows {
			get { return Path.DirectorySeparatorChar == '\\'; }
		}

		public static string GetPath (params string[] str)
		{
			return Path.GetFullPath (string.Join (Path.DirectorySeparatorChar.ToString (), str.Select (s => s.Replace ('/', Path.DirectorySeparatorChar))));
		}

		public static string Grep (string file, string regex, string group)
		{
			var m = Regex.Match (File.ReadAllText (GetPath(file)), regex);
			return m.Groups[group].Value;
		}

		public static void FileReplace (string file, string outFile, string toReplace, string replacement)
		{
			File.WriteAllText (GetPath(outFile), File.ReadAllText (GetPath(file)).Replace (toReplace, replacement));
		}

		public static TextReader Run (string file, string args)
		{
			currentProcess = new Process ();
			currentProcess.StartInfo.FileName = file;
			currentProcess.StartInfo.Arguments = args;
			currentProcess.StartInfo.RedirectStandardOutput = true;
			currentProcess.StartInfo.UseShellExecute = false;
			currentProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			currentProcess.Start ();
			return currentProcess.StandardOutput;
		}

		static Process currentProcess;
	}
}
