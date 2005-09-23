
using System;

namespace MonoDevelop.Services
{
	public delegate void LogAppendedHandler(object sender, LogAppendedArgs args);

	public interface ILoggingService
	{
		/* Test if a level is enabled for logging */
		bool IsDebugEnabled { get; }
		bool IsInfoEnabled { get; }
		bool IsWarnEnabled { get; }
		bool IsErrorEnabled { get; }
		bool IsFatalEnabled { get; }

		/* Log a message object */
		void Debug(object message);
		void Info(object message);
		void Warn(object message);
		void Error(object message);
		void Fatal(object message);
		
		void Debug(string logger, object message);
		void Info(string logger, object message);
		void Warn(string logger, object message);
		void Error(string logger, object message);
		void Fatal(string logger, object message);

		/* Log a message object and exception */
		void Debug(object message, Exception t);
		void Info(object message, Exception t);
		void Warn(object message, Exception t);
		void Error(object message, Exception t);
		void Fatal(object message, Exception t);

		void Debug(string logger, object message, Exception t);
		void Info(string logger, object message, Exception t);
		void Warn(string logger, object message, Exception t);
		void Error(string logger, object message, Exception t);
		void Fatal(string logger, object message, Exception t);

		/* Log a message string using the System.String.Format syntax */
		void DebugFormat(string format, params object[] args);
		void InfoFormat(string format, params object[] args);
		void WarnFormat(string format, params object[] args);
		void ErrorFormat(string format, params object[] args);
		void FatalFormat(string format, params object[] args);

		void DebugFormat(string logger, string format, params object[] args);
		void InfoFormat(string logger, string format, params object[] args);
		void WarnFormat(string logger, string format, params object[] args);
		void ErrorFormat(string logger, string format, params object[] args);
		void FatalFormat(string logger, string format, params object[] args);

		/* Log a message string using the System.String.Format syntax */
		void DebugFormat(IFormatProvider provider, string format, params object[] args);
		void InfoFormat(IFormatProvider provider, string format, params object[] args);
		void WarnFormat(IFormatProvider provider, string format, params object[] args);
		void ErrorFormat(IFormatProvider provider, string format, params object[] args);
		void FatalFormat(IFormatProvider provider, string format, params object[] args);

		void DebugFormat(string logger, IFormatProvider provider, string format, params object[] args);
		void InfoFormat(string logger, IFormatProvider provider, string format, params object[] args);
		void WarnFormat(string logger, IFormatProvider provider, string format, params object[] args);
		void ErrorFormat(string logger, IFormatProvider provider, string format, params object[] args);
		void FatalFormat(string logger, IFormatProvider provider, string format, params object[] args);

		event LogAppendedHandler LogAppended;
	}

	public class LogAppendedArgs
	{
		public string Message, Level;
		public DateTime Timestamp;
	}
}
