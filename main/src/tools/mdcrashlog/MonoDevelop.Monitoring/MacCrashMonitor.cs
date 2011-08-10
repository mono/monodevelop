using System;
using System.IO;

namespace MonoDevelop.Monitoring {

	class MacCrashMonitor : CrashMonitor {
		
		static readonly string CrashLogDirectory = 
			Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Library/Logs/CrashReporter");
		
		public MacCrashMonitor (int pid)
			: base (pid, CrashLogDirectory, "mono*")
		{
			
		}
		
		protected override void OnCrashDetected (CrashEventArgs e)
		{
			if (IsFromMonitoredPid (e.CrashLogPath))
				base.OnCrashDetected (e);
		}
		
		bool IsFromMonitoredPid (string logPath)
		{
			int parsedPid;
			
			using (var reader = new StreamReader (File.OpenRead (logPath))) {
				// First line of a macos crash dump should look like:
				// Process:         mono [1132]
				var line = reader.ReadLine ();
				if (string.IsNullOrEmpty (line) || !line.StartsWith ("Process"))
					return false;
				
				var startIndex = line.LastIndexOf ('[');
				var endIndex = line.LastIndexOf  (']');
				if (startIndex < 0 || endIndex < 0)
					return false;

				startIndex ++; // We don't want to include the '['
				var number = line.Substring (startIndex, endIndex  - startIndex);
				if (!int.TryParse (number, out parsedPid))
					return false;

				Console.WriteLine ("Parsed Pid was: {0}", parsedPid);
				return parsedPid ==  Pid;
			}
		}
	}
}
