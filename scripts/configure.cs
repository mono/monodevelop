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

namespace MonoDevelop.Configuration
{
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
        static IdeConfigurationTool config;

		static int Main (string[] args)
		{
			try {
                config = new IdeConfigurationTool (new FileInfo(Assembly.GetEntryAssembly().Location).Directory.Parent.FullName);

                if (args.Length == 0)
                {
                    PrintHelp();
                    return 0;
                }

                var cmd = args[0];
                args = args.Skip(1).ToArray();

                switch (cmd)
                {
                    case "get-version":
                        GetVersion(args);
                        break;
                    case "get-releaseid":
                        GetReleaseId(args);
                        break;
                    case "gen-updateinfo":
                        GenerateUpdateInfo(args);
                        break;
                    case "gen-buildinfo":
                        GenerateBuildInfo(args);
                        break;
                    case "is-preview":
                        GetIsPreview(args);
                        break;
                    case "is-major-preview":
                        GetIsMajorPreview(args);
                        break;
                    default:
                        Console.WriteLine("Unknown command: " + cmd);
                        return 1;
                }
                return 0;
            }
			catch (UserException ex) {
				Console.WriteLine (ex.Message);
				return 1;
			}
			catch (Exception ex) {
                Console.WriteLine (ex.ToString());
				return 1;
			}
		}

		static void GetVersion (string[] args)
		{
			Console.WriteLine (config.ProductVersion);
		}

		static void GetIsPreview (string[] args)
		{
			Console.WriteLine (config.IsPreview);
		}

		static void GetIsMajorPreview (string[] args)
		{
			Console.WriteLine (config.IsMajorPreview);
		}
		static void GetReleaseId (string[] args)
		{
			Console.WriteLine (config.ReleaseId);
		}

        static void GenerateUpdateInfo(string[] args)
        {
            if (args.Length == 0)
                throw new UserException("Platform config file not provided");
            if (args.Length == 1)
                throw new UserException("Target directory not provided");

            config.GenerateUpdateInfo(args[0], args[1]);
        }

        static void GenerateBuildInfo(string[] args)
        {
            if (args.Length == 0)
                throw new UserException("Target directory not provided");

            config.GenerateBuildInfo(args[0]);
        }

        static void PrintHelp()
        {
            Console.WriteLine("MonoDevelop Configuration Script");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("\tget-version: Prints the version of this release");
            Console.WriteLine("\tget-releaseid: Prints the release id");
            Console.WriteLine("\tis-preview: Prints `True` if this is a preview or `False` otherwise");
            Console.WriteLine("\tgen-updateinfo <config-file> <path>: Generates the updateinfo file");
            Console.WriteLine("\t\tin the provided path");
            Console.WriteLine("\tgen-buildinfo <path>: Generates the buildinfo file in the provided path");
            Console.WriteLine();
        }
    }

    public class IdeConfigurationTool
    {
        public readonly string MonoDevelopPath;
        public readonly string Version;
        public readonly string ProductVersion;
        public readonly string ProductVersionText;
        public readonly string CompatVersion;
		public readonly string SourceUrl;
        public readonly string AssemblyVersion = "4.0.0.0";
        public readonly string ReleaseId;
        public readonly PlatformInfo PlatformInfo;
        public readonly bool IsPreview;
        public readonly bool IsMajorPreview;

        public IdeConfigurationTool(string monoDevelopPath)
        {
            MonoDevelopPath = monoDevelopPath;

            string versionTxt = Path.Combine(MonoDevelopPath, "version.config");
            Version = SystemUtil.Grep(versionTxt, @"Version=(.*)");
            ProductVersionText = SystemUtil.Grep(versionTxt, "Label=(.*)");
            CompatVersion = SystemUtil.Grep(versionTxt, "CompatVersion=(.*)");
			SourceUrl = SystemUtil.Grep(versionTxt, "SourceUrl=(.*)", true);
            IsPreview = SystemUtil.Grep(versionTxt, "IsPreview=(.*)") == "true";
            IsMajorPreview = SystemUtil.Grep(versionTxt, "IsMajorPreview=(.*)") == "true";

			var customSource = Environment.GetEnvironmentVariable ("MONODEVELOP_UPDATEINFO_SOURCE_URL");
			if (!string.IsNullOrEmpty (customSource))
				SourceUrl = customSource;

			var customLabel = Environment.GetEnvironmentVariable ("MONODEVELOP_UPDATEINFO_LABEL");
			if (!string.IsNullOrEmpty (customLabel))
				ProductVersionText = customLabel;

            Version ver = new Version(Version);
            int vbuild = ver.Build != -1 ? ver.Build : 0;
            var cd = GetVersionCommitDistance(MonoDevelopPath);
            int vbrev = ver.Revision != -1 ? ver.Revision : cd;
            ReleaseId = "" + ver.Major + ver.Minor.ToString("00") + vbuild.ToString("00") + vbrev.ToString("0000");
            ProductVersion = ver.Major + "." + ver.Minor + "." + vbuild + "." + vbrev;
        }

		public void GenerateUpdateInfo (string platformConfigFile, string targetDir)
		{
			PlatformInfo pinfo;
            using (var sr = new StreamReader(platformConfigFile))
            {
				XmlSerializer ser = new XmlSerializer (typeof(PlatformInfo));
				pinfo = (PlatformInfo)ser.Deserialize (sr);
			}

			if (pinfo.AppId != null) {
				var content = pinfo.AppId + " " + ReleaseId;
				if (!string.IsNullOrEmpty (SourceUrl))
					content += "\nsource-url:" + SourceUrl;
				File.WriteAllText (Path.Combine (targetDir, "updateinfo"), content);
			}

			if (pinfo.UpdaterId != null)
				File.WriteAllText (Path.Combine (targetDir, "updateinfo.updater"), pinfo.UpdaterId + " " + pinfo.UpdaterVersion);
		}

        public void GenerateBuildInfo(string targetDir)
		{
            string head = SystemUtil.RunProcess(SystemUtil.GitExe, "rev-parse HEAD", MonoDevelopPath).Trim();

			var txt = "Release ID: " + ReleaseId + "\n";
			txt += "Git revision: " + head + "\n";
			txt += "Build date: " + DateTime.Now.ToString ("yyyy-MM-dd HH:mm:sszz") + "\n";

			var buildBranch = Environment.GetEnvironmentVariable ("BUILD_SOURCEBRANCHNAME");
			if (!string.IsNullOrWhiteSpace (buildBranch))
				txt += "Build branch: " + buildBranch;

            File.WriteAllText(Path.Combine(targetDir, "buildinfo"), txt);
		}

		static int GetVersionCommitDistance (string path)
		{
            var blame = new StringReader(SystemUtil.RunProcess(SystemUtil.GitExe, "blame version.config", path));
			string line;
			while ((line = blame.ReadLine ()) != null && line.IndexOf ("Version=") == -1)
				;
			if (line != null) {
				string hash = line.Substring (0, line.IndexOf (' ')).TrimStart ('^');
                string dist = SystemUtil.RunProcess(SystemUtil.GitExe, "rev-list --count " + hash + "..HEAD", path);
				return int.Parse (dist.Trim ());
			}
			return 0;
		}
	}

	class UserException: Exception
	{
		public UserException (string msg)
			: base (msg)
		{
		}
	}

    public static class Logging
    {
        static string logFile;
        static object localLock = new object ();

        public static void Initialize(string logFileName)
        {
            logFile = logFileName;
        }

        public static void ReportError(string msg)
        {
            if (logFile != null) {
                lock (localLock)
                    File.AppendAllText(logFile, "[ERROR] " + msg + Environment.NewLine);
            }
            Console.WriteLine("[ERROR] " + msg);
        }

        public static void ReportInfo(string msg)
        {
            if (logFile != null) {
                lock (localLock)
                    File.AppendAllText(logFile, "[INFO] " + msg + Environment.NewLine);
            }
            Console.WriteLine(msg);
        }

        public static void ReportDebug(string msg)
        {
            if (logFile != null)
            {
                lock (localLock)
                    File.AppendAllText(logFile, "[INFO] " + msg + Environment.NewLine);
            }
        }
    }

	public static class SystemUtil
	{
		static SystemUtil ()
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
				if (File.Exists ("/usr/bin/git"))
					GitExe = "/usr/bin/git";
				else
					GitExe = "git";
			}
		}

		public static string GitExe { get; private set; }

		public static Platform Platform { get; private set; }

        public static string Grep(string file, string regex, bool optional = false)
        {
            string txt = File.ReadAllText(file);
            var m = Regex.Match(txt, regex);
			if (m == null || !m.Success) {
				if (!optional)
					throw new UserException ("Match not found for regex: " + regex);
				return null;
			}
            if (m.Groups.Count != 2)
                throw new UserException("Invalid regex: expression must have a single capture group: " + regex);
            Group cap = m.Groups[1];
            return cap.Value;
        }

       public static string RunProcess(string file, string args, string workingDir)
       {
           Logging.ReportDebug(file + " " + args);

            Process p = new Process();
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.WorkingDirectory = workingDir;
            p.StartInfo.FileName = file;
            p.StartInfo.Arguments = args;
            StringBuilder sb = new StringBuilder();
            p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                Logging.ReportDebug(e.Data);
                sb.AppendLine(e.Data);
            };
            p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                Logging.ReportDebug(e.Data);
            };
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new UserException(file + " failed");
            return sb.ToString();
        }
    }
}
