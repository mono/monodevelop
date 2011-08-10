using System;

namespace MonoDevelop.Monitoring {

	interface ICrashMonitor {
		event EventHandler ApplicationExited;
		event EventHandler<CrashEventArgs> CrashDetected;
		
		void Start ();
		void Stop ();
	}
}

