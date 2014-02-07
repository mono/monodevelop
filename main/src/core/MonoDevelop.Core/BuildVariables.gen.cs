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
			var lines = File.ReadAllLines (Path.Combine (dir, "..", "..", "..", "..", "version.config"));
			var txt = File.ReadAllText (Path.Combine (dir, "BuildVariables.cs.in"));
			txt = txt.Replace ("@PACKAGE_VERSION@", GetValue (lines, "Version"));
			txt = txt.Replace ("@PACKAGE_VERSION_LABEL@", GetValue (lines, "Label"));
			txt = txt.Replace ("@COMPAT_ADDIN_VERSION@", GetValue (lines, "CompatVersion"));
			File.WriteAllText (Path.Combine (dir, "BuildVariables.cs"), txt);
		}

		static string GetValue (string[] lines, string key)
		{
			var val = lines.First (li => li.StartsWith (key + "="));
			return val.Substring (key.Length + 1);
		}
	}
}

