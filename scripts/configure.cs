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
using System.Reflection;

namespace SetupConfig
{
	public class Config
	{
		public string MonoDevelopPath;
		public int InstallerVersion;
		public string Version;
		public string ProductVersion;
		public string ProductVersionText;
		public string CompatVersion;
		public string AssemblyVersion = "4.0.0.0";
		public string ReleaseId;
		public string PlatformGuid;
		public string PlatformUpdaterGuid;
		public PlatformInfo PlatformInfo;
	}

	public class PlatformInfo
	{
		public string AppId;
		public string UpdaterId;
		public string UpdaterVersion;
	}

	public enum Platform
	{
		Mac,
		Windows,
		Linux
	}

	class Program
	{
		const string MacId = "a3140c14-ef90-4019-ae6c-9d93804d6611";
		const string MacUpdaterId = "42baf30f-edc9-4feb-b99d-d6d311271c65";
		const string WindowsId = "E55A5A70-C6F6-4845-8A01-89DAA5B6DA43";
		const string WindowsUpdaterId = "PlEhBk81kBfey9Va";

		static StreamWriter logFile;

		static Config config;

		static int Main (string[] args)
		{
			logFile = new StreamWriter ("config.log");
			try {
				ReadConfig ();
				return Run (args);
			}
			catch (UserException ex) {
				ReportError (ex.Message);
				ReportInfo ("See config.log for details.");
				return 1;
			}
			catch (Exception ex) {
				ReportError (ex.ToString ());
				ReportInfo ("See config.log for details.");
				return 1;
			}
			finally {
				logFile.Close ();
			}
		}

		static int Run (string[] args)
		{
			if (args.Length == 0) {
				PrintHelp ();
				return 0;
			}

			var cmd = args [0];
			args = args.Skip (1).ToArray ();

			switch (cmd) {
			case "get-version":
				GetVersion (args);
				break;
			case "get-releaseid":
				GetReleaseId (args);
				break;
			case "gen-updateinfo":
				GenerateUpdateInfo (args);
				break;
			case "gen-buildinfo":
				GenerateBuildInfo (args);
				break;
			default:
				Console.WriteLine ("Unknown command: " + cmd);
				return 1;
			}
			return 0;
		}

		static void GetVersion (string[] args)
		{
			Console.WriteLine (config.ProductVersion);
		}

		static void GetReleaseId (string[] args)
		{
			Console.WriteLine (config.ReleaseId);
		}

		static void GenerateUpdateInfo (string[] args)
		{
			if (args.Length == 0)
				throw new UserException ("Platform config file not provided");
			if (args.Length == 1)
				throw new UserException ("Target directory not provided");

			PlatformInfo pinfo;
			using (var sr = new StreamReader (args [0])) {
				XmlSerializer ser = new XmlSerializer (typeof(PlatformInfo));
				pinfo = (PlatformInfo)ser.Deserialize (sr);
			}

			if (pinfo.AppId != null)
				File.WriteAllText (Path.Combine (args [1], "updateinfo"), pinfo.AppId + " " + config.ReleaseId);

			if (pinfo.UpdaterId != null)
				File.WriteAllText (Path.Combine (args [1], "updateinfo.updater"), pinfo.UpdaterId + " " + pinfo.UpdaterVersion);
		}

		static void GenerateBuildInfo (string[] args)
		{
			if (args.Length == 0)
				throw new UserException ("Target directory not provided");

			string head = RunProcess (SystemInfo.GitExe, "rev-parse HEAD", config.MonoDevelopPath).Trim ();

			var txt = "Release ID: " + config.ReleaseId + "\n";
			txt += "Git revision: " + head + "\n";
			txt += "Build date: " + DateTime.Now.ToString ("yyyy-MM-dd HH:mm:sszz") + "\n";

			File.WriteAllText (Path.Combine (args [0], "buildinfo"), txt);
		}

		static void PrintHelp ()
		{
			Console.WriteLine ("MonoDevelop Configuration Script");
			Console.WriteLine ();
			Console.WriteLine ("Commands:");
			Console.WriteLine ("\tget-version: Prints the version of this release");
			Console.WriteLine ("\tget-releaseid: Prints the release id");
			Console.WriteLine ("\tgen-updateinfo <config-file> <path>: Generates the updateinfo file in the provided path");
			Console.WriteLine ("\tgen-buildinfo <path>: Generates the buildinfo file in the provided path");
			Console.WriteLine ();
		}

		static void ReadConfig ()
		{
			config = new Config () {
				InstallerVersion = 2
			};

			config.MonoDevelopPath = new FileInfo (Assembly.GetEntryAssembly ().Location).Directory.Parent.ToString ();

			string versionTxt = Path.Combine (config.MonoDevelopPath, "version.config");
			config.Version = Grep (versionTxt, @"Version=(.*)");
			config.ProductVersionText = Grep (versionTxt, "Label=(.*)");
			config.CompatVersion = Grep (versionTxt, "CompatVersion=(.*)");

			Version ver = new Version(config.Version);
			int vbuild = ver.Build != -1 ? ver.Build : 0;
			var cd = GetVersionCommitDistance (config.MonoDevelopPath);
			int vbrev = ver.Revision != -1 ? ver.Revision : cd;
			config.ReleaseId = "" + ver.Major + ver.Minor.ToString ("00") + vbuild.ToString ("00") + vbrev.ToString ("0000");
			config.ProductVersion = ver.Major + "." + ver.Minor + "." + vbuild + "." + vbrev;
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

		static int GetVersionCommitDistance (string path)
		{
			var blame = new StringReader (RunProcess (SystemInfo.GitExe, "blame version.config", path));
			string line;
			while ((line = blame.ReadLine ()) != null && line.IndexOf ("Version=") == -1)
				;
			if (line != null) {
				string hash = line.Substring (0, line.IndexOf (' '));
				string dist = RunProcess (SystemInfo.GitExe, "rev-list --count " + hash + "..HEAD", path);
				return int.Parse (dist.Trim ());
			}
			return 0;
		}

		static string Grep (string file, string regex)
		{
			string txt = File.ReadAllText (file);
			var m = Regex.Match (txt, regex);
			if (m == null)
				throw new UserException ("Match not found for regex: " + regex);
			if (m.Groups.Count != 2)
				throw new UserException ("Invalid regex: expression must have a single capture group: " + regex);
			Group cap = m.Groups[1];
			return cap.Value;
		}

		static string RunProcess (string file, string args, string workingDir)
		{
			logFile.WriteLine (file + " " + args);

			Process p = new Process ();
			p.StartInfo.CreateNoWindow = true;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.WorkingDirectory = workingDir;
			p.StartInfo.FileName = file;
			p.StartInfo.Arguments = args;
			StringBuilder sb = new StringBuilder ();
			p.OutputDataReceived += delegate (object s, DataReceivedEventArgs e)
			{
				logFile.WriteLine (e.Data);
				sb.AppendLine (e.Data);
			};
			p.ErrorDataReceived += delegate (object s, DataReceivedEventArgs e)
			{
				logFile.WriteLine (e.Data);
			};
			p.Start ();
			p.BeginOutputReadLine ();
			p.BeginErrorReadLine ();
			p.WaitForExit ();
			if (p.ExitCode != 0)
				throw new UserException (file + " failed");
			return sb.ToString ();
		}
	}

	class UserException: Exception
	{
		public UserException (string msg)
			: base (msg)
		{
		}
	}

	static class SystemInfo
	{
		static SystemInfo ()
		{
			if (Path.DirectorySeparatorChar == '\\')
				Platform = Platform.Windows;
			else if (Directory.Exists ("/Library/Application Support"))
				Platform = Platform.Mac;
			else
				Platform = Platform.Linux;

			if (Platform == Platform.Windows) {
				if (File.Exists (@"c:\Program Files\Git\bin\git.exe"))
					GitExe = @"c:\Program Files\Git\bin\git.exe";
				else if (File.Exists (@"c:\Program Files (x86)\Git\bin\git.exe"))
					GitExe = @"c:\Program Files (x86)\Git\bin\git.exe";
				else if (File.Exists (@"c:\msysgit\bin\git.exe"))
					GitExe = @"c:\msysgit\bin\git.exe";
				else
					GitExe = "git.exe";
			} else {
				GitExe = "git";
			}
		}

		public static string GitExe { get; private set; }

		public static Platform Platform { get; private set; }
	}
}
