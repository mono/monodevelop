using System;
using System.IO;
using System.Linq;

namespace Application
{
	public class GenVersion
	{
		public static void Main (string[] args)
		{
			var dir = args [0];
			var pathVersionConfig = Path.Combine (dir, "..", "..", "..", "..", "version.config");
			if (!File.Exists(pathVersionConfig))
			{
				// in a tarball, we have less depth in the directory hierarchy
				pathVersionConfig = Path.Combine (dir, "..", "..", "..", "version.config");
			}

			var lines = File.ReadAllLines (pathVersionConfig);

			var label = GetValue (lines, "Label");
			var customLabel = Environment.GetEnvironmentVariable ("MONODEVELOP_UPDATEINFO_LABEL");
			if (!string.IsNullOrEmpty (customLabel))
				label = customLabel;

			var txt = File.ReadAllText (Path.Combine (dir, "BuildVariables.cs.in"));
			var buildInfoVersion = GetValue (lines, "Version");
			txt = txt.Replace ("@PACKAGE_VERSION@", buildInfoVersion);
			txt = txt.Replace ("@FULL_VERSION@", GetFullVersion(buildInfoVersion));
			txt = txt.Replace ("@PACKAGE_VERSION_LABEL@", label);
			txt = txt.Replace ("@COMPAT_ADDIN_VERSION@", GetValue (lines, "CompatVersion"));
			txt = txt.Replace ("@BUILD_LANE@", Environment.GetEnvironmentVariable ("BUILD_LANE"));
			File.WriteAllText (Path.Combine (dir, "BuildVariables.cs"), txt);
		}

		static string GetValue (string[] lines, string key)
		{
			var val = lines.First (li => li.StartsWith (key + "="));
			return val.Substring (key.Length + 1);
		}

		static string GetFullVersion(string buildInfoVersion)
		{
			var version = new Version (buildInfoVersion);
			var relId = GetReleaseId ();
			if (relId != null && relId.Length >= 9) {
				int rev;
				int.TryParse (relId.Substring (relId.Length - 4), out rev);
				version = new Version (Math.Max (version.Major, 0), Math.Max (version.Minor, 0), Math.Max (version.Build, 0), Math.Max (rev, 0));
			}
			return version.ToString();
		}	

		static string GetReleaseId ()
		{
			var biFile = System.Reflection.Assembly.GetEntryAssembly ().Location;
			biFile = Path.GetDirectoryName (biFile);
			biFile = Path.Combine (biFile, "fullbuildinfo", "buildinfo");

			if (File.Exists (biFile)) {
				var line = File.ReadAllLines (biFile).Select (l => l.Split (':')).FirstOrDefault (a => a.Length > 1 && a [0].Trim () == "Release ID");
				if (line != null)
					return line [1].Trim ();
			}
			return null;
		}	
	}
}

