using System;

namespace MonoDevelop.SessionLogging {

	public interface ISessionLogger {
		void LogException (Exception ex);
	}
}

