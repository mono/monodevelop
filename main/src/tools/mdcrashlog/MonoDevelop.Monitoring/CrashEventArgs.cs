using System;

namespace MonoDevelop.Monitoring {

	class CrashEventArgs : EventArgs {
		
		public string CrashLogPath {
			get; private set;
		}
		
		public CrashEventArgs (string crashLogPath)
		{
			CrashLogPath = crashLogPath;
		}
	}
}

