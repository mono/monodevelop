// created on 6/8/2004 at 5:44 AM
using System;
using System.Diagnostics;
using MonoDevelop.Core;

namespace MonoDevelop.Prj2Make
{
	public sealed class PkgConfigInvoker
	{
		
		public static string GetPkgConfigVersion()
		{
			string pkgout = null;

			pkgout = RunPkgConfig("--version");

			if(pkgout != null)
			{
				return pkgout;
			}

			return null;
		}

		public static string GetPkgVariableValue(string strPkg, string strVarName)
		{
			string pkgout = null;

			pkgout = RunPkgConfig(String.Format("--variable={0} {1}", 
				strVarName, strPkg));

			if(pkgout != null)
			{
				return pkgout;
			}

			return null;
		}

		public static string GetPkgConfigModuleVersion(string strPkg)
		{
			string pkgout = null;

			pkgout = RunPkgConfig(String.Format("--modversion {0}", strPkg));

			if(pkgout != null)
			{
				return pkgout;
			}

			return null;
		}

		public static string RunPkgConfig(string strArgLine)
		{
			string pkgout;

			ProcessStartInfo pi = new ProcessStartInfo ();
			pi.FileName = "pkg-config";
			pi.RedirectStandardOutput = true;
			pi.UseShellExecute = false;
			pi.Arguments = strArgLine;
			Process p = null;
			try 
			{
				p = Process.Start (pi);
			} 
			catch (Exception e) 
			{
				Console.WriteLine("Couldn't run pkg-config: " + e.Message);
				Environment.Exit (1);
			}

			if (p.StandardOutput == null)
			{
				LoggingService.LogDebug ("Specified package did not return any information");
			}
			
			pkgout = p.StandardOutput.ReadToEnd ();		
			p.WaitForExit ();
			if (p.ExitCode != 0) 
			{
				LoggingService.LogDebug ("pkg-config command failed: pkg-config " + strArgLine);
				return null;
			}

			if (pkgout != null)
			{
				p.Close ();
				return pkgout;
			}

			p.Close ();

			return null;
		}
	}
}
