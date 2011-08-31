using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Xml;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;

namespace SetupConfig
{
	public class Config
	{
		public string MonoDevelopPath;
		public int MajorVersion;
		public int MinorVersion;
		public int PointVersion;
		public int BuildVersion;
		public int InstallerVersion;
		public string ProductVersionText;
		public int AssemblyMajorVersion;
		public int AssemblyMinorVersion;
		public int AssemblyPointVersion;
		public int AssemblyBuildVersion;
	}

	class Program
	{
		static StreamWriter logFile;

		static Config config;

		static int Main (string[] args)
		{
			logFile = new StreamWriter ("config.log");
			try {
				XmlSerializer ser = new XmlSerializer (typeof (Config));

				if (!File.Exists ("config.xml")) {
					Config c = new Config () {
						MajorVersion = 2,
						MinorVersion = 6,
						PointVersion = 0,
						BuildVersion = 0,
						InstallerVersion = 1,
						ProductVersionText = "2.6",
						AssemblyMajorVersion = 2,
						AssemblyMinorVersion = 6,
						AssemblyPointVersion = 0,
						AssemblyBuildVersion = 0
					};
					using (StreamWriter sw = new StreamWriter ("config.xml")) {
						XmlTextWriter tw = new XmlTextWriter (sw);
						tw.Formatting = Formatting.Indented;
						ser.Serialize (tw, c);
					}
					ReportError ("config.xml file not found");
					ReportInfo ("A dummy config.xml file has been created. Please edit the file and run SetupConfig again");
					return 1;
				}

				using (var sr = File.OpenRead ("config.xml")) {
					config = (Config)ser.Deserialize (sr);
				}

				if (string.IsNullOrEmpty (config.MonoDevelopPath))
					config.MonoDevelopPath = "..\\..";
				Run ();
				return 0;
			}
			catch (Exception ex) {
				ReportError (ex.Message);
				ReportInfo ("See config.log for details.");
				return 1;
			}
			finally {
				logFile.Close ();
			}
		}

		static void Run ()
		{
			string monoLibsPath = Environment.ExpandEnvironmentVariables ("%ProgramFiles(x86)%") + "\\MonoLibraries\\2.6";
			if (!Directory.Exists (monoLibsPath))
				monoLibsPath = Environment.ExpandEnvironmentVariables ("%ProgramFiles%") + "\\MonoLibraries\\2.6";
			if (!Directory.Exists (monoLibsPath))
				throw new Exception ("Mono libraries folder not found.\nGet latest from http://software.xamarin.com/files/MonoLibraries.msi");

			var productVersion = "" + config.MajorVersion + "." + config.MinorVersion + "." + config.PointVersion + (config.BuildVersion != 0 ? "." + config.BuildVersion : "");
			var assemblyVersion = config.AssemblyMajorVersion + "." + config.AssemblyMinorVersion + "." + config.AssemblyPointVersion + "." + config.AssemblyBuildVersion;
			var monoProductVersion = "" + config.MajorVersion + config.MinorVersion.ToString ("00") + config.PointVersion.ToString ("00") + config.BuildVersion.ToString ("000");

			ReportInfo ("Product version text: " + config.ProductVersionText);
			ReportInfo ("Product version:      " + productVersion);
			ReportInfo ("Assembly version:     " + assemblyVersion);
			ReportInfo ("Product version Id:   " + monoProductVersion);
			ReportInfo ("Installer version Id: " + config.InstallerVersion);
			ReportInfo ("---------------------");

			var fileDownloaded = new System.Threading.ManualResetEvent (true);

			if (!Directory.Exists ("ExtraFiles")) {
				fileDownloaded.Reset ();
				if (File.Exists ("ExtraFiles.zip"))
					File.Delete ("ExtraFiles.zip");
				ReportInfo ("Getting ExtraFiles.zip from http://monodevelop.com/files/setup/ExtraFiles.zip");
				WebClient w = new WebClient ();
				w.DownloadFileCompleted += delegate
				{
					fileDownloaded.Set ();
				};
				w.DownloadFileAsync (new Uri ("http://monodevelop.com/files/setup/ExtraFiles.zip"), "ExtraFiles.zip");
			}

/*			Build (config.MonoDevelopPath, "main\\main.sln /p:Configuration=DebugWin32 /p:Platform=\"x86\"");
			Build (config.MonoDevelopPath, "extras\\VersionControl.Subversion.Win32\\VersionControl.Subversion.Win32.sln");
			Build (config.MonoDevelopPath, "extras\\MonoDevelop.Debugger.Win32\\MonoDevelop.Debugger.Win32.sln");
			Build (config.MonoDevelopPath, "extras\\MonoDevelop.MonoDroid\\MonoDevelop.MonoDroid.sln");
*/
			// Copy support assemblies

			ReportInfo ("Copying support libraries");

			if (!Directory.Exists("Libraries"))
				Directory.CreateDirectory ("Libraries");

			File.Copy (monoLibsPath + "\\Mono.Addins.dll", "Libraries\\Mono.Addins.dll", true);
			File.Copy (monoLibsPath + "\\Mono.Addins.Gui.dll", "Libraries\\Mono.Addins.Gui.dll", true);
			File.Copy (monoLibsPath + "\\Mono.Addins.Setup.dll", "Libraries\\Mono.Addins.Setup.dll", true);
			File.Copy (monoLibsPath + "\\Mono.Addins.CecilReflector.dll", "Libraries\\Mono.Addins.CecilReflector.dll", true);
			File.Copy (monoLibsPath + "\\ICSharpCode.SharpZipLib.dll", "Libraries\\ICSharpCode.SharpZipLib.dll", true);
			File.Copy (monoLibsPath + "\\Mono.GetOptions.dll", "Libraries\\Mono.GetOptions.dll", true);
			File.Copy (monoLibsPath + "\\monodoc.dll", "Libraries\\monodoc.dll", true);
			File.Copy (monoLibsPath + "\\Mono.Security.dll", "Libraries\\Mono.Security.dll", true);

			// Copy support files

			ReportInfo ("Copying support files");

			if (!Directory.Exists ("ExtraFiles")) {
				ReportInfo ("Waiting for ExtraFiles.zip download to finish.");
				if (!fileDownloaded.WaitOne (TimeSpan.FromMinutes (10)))
					throw new Exception ("Timeout while downloading ExtraFiles.zip");
				if (!File.Exists ("ExtraFiles.zip"))
					throw new Exception ("./ExtraFiles folder not found.\nYou can get the contents of this folder from:\nhttp://monodevelop.com/files/setup/ExtraFiles.zip");
				FastZip zip = new FastZip ();
				Directory.CreateDirectory ("ExtraFiles");
				zip.ExtractZip ("ExtraFiles.zip", "ExtraFiles", null);
			}

			CopyFolderContent ("ExtraFiles", config.MonoDevelopPath + "\\main\\build\\bin\\");

			// Set the version numbers

			ReportInfo ("Updating version numbers");
			RegexReplace ("Product.wxs", "ProductVersionText = \".*?\"", "ProductVersionText = \"" + config.ProductVersionText + "\"");
			RegexReplace ("Product.wxs", "ProductVersion = \".*?\"", "ProductVersion = \"" + productVersion + "\"");
			RegexReplace ("Product.wxs", "AssemblyVersion = \".*?\"", "AssemblyVersion = \"" + assemblyVersion + "\"");
			RegexReplace ("Product.wxs", "BuildRoot = \".*?\"", "BuildRoot = \"" + config.MonoDevelopPath + "\\main\\build\"");

			// Create the updateinfo file

			ReportInfo ("Generating updateinfo file");
			File.WriteAllText ("updateinfo", "E55A5A70-C6F6-4845-8A01-89DAA5B6DA43 " + monoProductVersion);
			File.WriteAllText ("updateinfo.updater", "PlEhBk81kBfey9Va " + config.InstallerVersion);

			ReportInfo ("Setup file successfully updated");
		}

		static void ReportError (string msg)
		{
			logFile.WriteLine ("[ERROR] " + msg);
			Console.WriteLine ("[ERROR] " + msg);
		}

		static void ReportInfo (string msg)
		{
			logFile.WriteLine ("[INFO] " + msg);
			Console.WriteLine (msg);
		}

		static void Build (string prefix, string solution)
		{
			RunTarget (prefix, solution, "Clean");
			RunTarget (prefix, solution, "Build");
		}

		static void RunTarget (string prefix, string solution, string target)
		{
			ReportInfo (target + ": " + solution);
			Process p = new Process ();
			p.StartInfo.CreateNoWindow = true;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.FileName = "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\msbuild.exe";
			p.StartInfo.Arguments = "/t:" + target + " " + prefix + "\\" + solution;
			p.OutputDataReceived += delegate (object s, DataReceivedEventArgs e) {
				logFile.WriteLine (e.Data);
			};
			p.ErrorDataReceived += delegate (object s, DataReceivedEventArgs e) {
				logFile.WriteLine (e.Data);
			};
			p.Start ();
			p.BeginOutputReadLine ();
			p.BeginErrorReadLine ();
			p.WaitForExit ();
			if (p.ExitCode != 0)
				throw new Exception (target + " failed");
		}

		static void CopyFolderContent (string src, string dest)
		{
			foreach (string file in Directory.GetFileSystemEntries (src)) {
				string destPath = Path.Combine (dest, Path.GetFileName (file));
				if (Directory.Exists (file)) {
					if (!Directory.Exists (destPath))
						Directory.CreateDirectory (destPath);
					CopyFolderContent (file, destPath);
				}
				else {
					File.Copy (file, destPath, true);
				}
			}
		}

		static void RegexReplace (string file, string regex, string newText)
		{
			string txt = File.ReadAllText (file);
			txt = Regex.Replace (txt, regex, newText);
			File.WriteAllText (file, txt);
		}
	}
}
