using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using CA = MonoDevelop.CodeAnalysis;

namespace MonoDevelop.CodeAnalysis.Smokey {
	
	public class SmokeyRunner : IRunner
	{
		private static readonly Assembly smokey;
		
		static SmokeyRunner ()
		{
			string assemblyDirectory = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);
			smokey = Assembly.LoadFile (Path.Combine (assemblyDirectory, "smokey.exe"));
		}
		
		public SmokeyRunner()
		{
		}

		public IEnumerable<CA.IViolation> Run (string inspectedFile, IEnumerable<CA.IRule> ruleSet)
		{
			// FIXME: add support for ruleSet parameter
			// TODO: use MonoDevelop process APIs instead of System.Diagnostics
			string arguments = "-xml";

			ProcessStartInfo startInfo = new ProcessStartInfo ("mono");
			// runtime needs to be defined explicitly (or somewhy 1.1 can be loaded)
			startInfo.Arguments = string.Format ("--runtime=v2.0.50727 {0} {1} {2}", 
							smokey.CodeBase, arguments, inspectedFile);
			startInfo.RedirectStandardOutput = true;
			startInfo.UseShellExecute = false;
			
			Process smokeyProcess = null;			
			try {
				smokeyProcess = Process.Start (startInfo);
				return SmokeyParser.ParseOutput (smokeyProcess.StandardOutput, ruleSet);
			} catch (Exception ex) {
				throw new InvalidOperationException (AddinCatalog.GetString ("Could not run Smokey or parse its output."), ex);
			} finally {
				if (smokey != null) smokeyProcess.Dispose ();
			}
		}

		public static Assembly Smokey {
			get { return smokey; }
		}

		public string Id {
			get { return "SmokeyRunner"; }
		}

		public string Name {
			get { return "Smokey"; }
		}
	}
}
