// 
// LoggingService.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Mono.Addins;
using MonoDevelop.Core.LogReporting;
using MonoDevelop.Core.Logging;
using Mono.Unix.Native;
using System.Text;
using System.Collections.Immutable;

namespace MonoDevelop.Core
{
	public static class LoggingService
	{
		const string ServiceVersion = "1";
		const string ReportCrashesKey = "MonoDevelop.LogAgent.ReportCrashes";
		const string ReportUsageKey = "MonoDevelop.LogAgent.ReportUsage";

		static object serviceLock = new object ();
		static ImmutableList<ILogger> loggers = ImmutableList<ILogger>.Empty;
		static RemoteLogger remoteLogger;
		static DateTime timestamp;
		static int logFileSuffix;
		static TextWriter defaultError;
		static TextWriter defaultOut;
		static bool reporting;

		// Return value is the new value for 'ReportCrashes'
		// First parameter is the current value of 'ReportCrashes
		// Second parameter is the exception
		// Thirdparameter shows if the exception is fatal or not
		public static Func<bool?, Exception, bool, bool?> UnhandledErrorOccured;

		static List<CrashReporter> customCrashReporters = new List<CrashReporter> ();

		static LoggingService ()
		{
			var consoleLogger = new ConsoleLogger ();
			loggers = loggers.Add (consoleLogger);
			loggers = loggers.Add (new InstrumentationLogger ());
			
			string consoleLogLevelEnv = Environment.GetEnvironmentVariable ("MONODEVELOP_CONSOLE_LOG_LEVEL");
			if (!string.IsNullOrEmpty (consoleLogLevelEnv)) {
				try {
					consoleLogger.EnabledLevel = (EnabledLoggingLevel) Enum.Parse (typeof (EnabledLoggingLevel), consoleLogLevelEnv, true);
				} catch (Exception e) {
					LogError ("Error setting log level", e);
				}
			}
			
			string consoleLogUseColourEnv = Environment.GetEnvironmentVariable ("MONODEVELOP_CONSOLE_LOG_USE_COLOUR");
			if (!string.IsNullOrEmpty (consoleLogUseColourEnv) && consoleLogUseColourEnv.ToLower () == "false") {
				consoleLogger.UseColour = false;
			} else {
				consoleLogger.UseColour = true;
			}
			
			string logFileEnv = Environment.GetEnvironmentVariable ("MONODEVELOP_LOG_FILE");
			if (!string.IsNullOrEmpty (logFileEnv)) {
				try {
					var fileLogger = new FileLogger (logFileEnv);
					loggers = loggers.Add (fileLogger);
					string logFileLevelEnv = Environment.GetEnvironmentVariable ("MONODEVELOP_FILE_LOG_LEVEL");
					fileLogger.EnabledLevel = (EnabledLoggingLevel) Enum.Parse (typeof (EnabledLoggingLevel), logFileLevelEnv, true);
				} catch (Exception e) {
					LogError ("Error setting custom log file", e);
				}
			}
			try {
				PurgeOldLogs ();
			} catch {
				LogError ("Could not purge old log files");
			}

			timestamp = DateTime.Now;

			//remove the default trace listener on .NET, it throws up horrible dialog boxes for asserts
			Debug.Listeners.Clear ();

			//add a new listener that just logs failed asserts
			Debug.Listeners.Add (new AssertLoggingTraceListener ());
		}

		public static bool? ReportCrashes {
			get { return PropertyService.Get<bool?> (ReportCrashesKey); }
			set { PropertyService.Set (ReportCrashesKey, value); }
		}

		public static event EventHandler<PropertyChangedEventArgs> ReportCrashesChanged {
			add { PropertyService.AddPropertyHandler (ReportCrashesKey, value); }
			remove { PropertyService.RemovePropertyHandler (ReportCrashesKey, value); }
		}

		public static bool? ReportUsage {
			get { return PropertyService.Get<bool?> (ReportUsageKey); }
			set { PropertyService.Set (ReportUsageKey, value); }
		}

		public static event EventHandler<PropertyChangedEventArgs> ReportUsageChanged {
			add { PropertyService.AddPropertyHandler (ReportUsageKey, value); }
			remove { PropertyService.RemovePropertyHandler (ReportUsageKey, value); }
		}

		/// <summary>
		/// Creates a session log file with the given identifier.
		/// </summary>
		/// <returns>A TextWriter, null if the file cannot be created.</returns>
		public static TextWriter CreateLogFile (string identifier)
		{
			string filename;
			return CreateLogFile (identifier, out filename);
		}

		public static TextWriter CreateLogFile (string identifier, out string filename)
		{
			FilePath logDir = UserProfile.Current.LogDir;
			Directory.CreateDirectory (logDir);

			int oldIdx = logFileSuffix;

			while (true) {
				filename = logDir.Combine (GetSessionLogFileName (identifier));
				try {
					var stream = File.Open (filename, FileMode.Create, FileAccess.Write, FileShare.Read);
					return new StreamWriter (stream) { AutoFlush = true };
				} catch (Exception ex) {
					// if the file already exists, retry with a suffix, up to 10 times
					if (logFileSuffix < oldIdx + 10) {
						const int ERROR_FILE_EXISTS = 80;
						const int ERROR_SHARING_VIOLATION = 32;
						var err = System.Runtime.InteropServices.Marshal.GetHRForException (ex) & 0xFFFF;
						if (err == ERROR_FILE_EXISTS || err == ERROR_SHARING_VIOLATION) {
							logFileSuffix++;
							continue;
						}
					}
					LogInternalError ("Failed to create log file.", ex);
					return null;
				}
			}
		}

		static string GetSessionLogFileName (string logName)
		{
			if (logFileSuffix == 0)
				return string.Format ("{0}.{1}.log", logName, timestamp.ToString ("yyyy-MM-dd__HH-mm-ss"));
			return string.Format ("{0}.{1}-{2}.log", logName, timestamp.ToString ("yyyy-MM-dd__HH-mm-ss"), logFileSuffix);
		}
		
		public static void Initialize (bool redirectOutput)
		{
			// Always redirect on windows otherwise we cannot get output at all
			if (Platform.IsWindows || redirectOutput)
				RedirectOutputToLogFile ();
		}
		
		public static void Shutdown ()
		{
			RestoreOutputRedirection ();
		}

		public static void RegisterCrashReporter (CrashReporter reporter)
		{
			lock (customCrashReporters)
				customCrashReporters.Add (reporter);
		}

		public static void UnregisterCrashReporter (CrashReporter reporter)
		{
			lock (customCrashReporters)
				customCrashReporters.Remove (reporter);
		}

		internal static void ReportUnhandledException (Exception ex, bool willShutDown)
		{
			ReportUnhandledException (ex, willShutDown, false, null);
		}

		internal static void ReportUnhandledException (Exception ex, bool willShutDown, bool silently)
		{
			ReportUnhandledException (ex, willShutDown, silently, null);
		}

		internal static void ReportUnhandledException (Exception ex, bool willShutDown, bool silently, string tag)
		{
			try {
				var tags = new List<string> { tag };

				if (reporting)
					return;

				reporting = true;

				var oldReportCrashes = ReportCrashes;

				if (UnhandledErrorOccured != null && !silently)
					ReportCrashes = UnhandledErrorOccured (ReportCrashes, ex, willShutDown);

				// If crash reporting has been explicitly disabled, disregard this crash
				if (ReportCrashes.HasValue && !ReportCrashes.Value)
					return;

				lock (customCrashReporters) {
					foreach (var cr in customCrashReporters.Concat (AddinManager.GetExtensionObjects<CrashReporter> (true)))
						cr.ReportCrash (ex, willShutDown, tags);
				}

				//ensure we don't lose the setting
				if (ReportCrashes != oldReportCrashes) {
					PropertyService.SaveProperties ();
				}

			} catch {
				// I don't know if we can/should try to log this. I'm going to guess that we don't want to
				// and can't safely call any LoggingService.Log methods anyway in case we'd recurse, though
				// the 'reporting' boolean should take care of that.
			} finally {
				reporting = false;
			}
		}

		static void PurgeOldLogs ()
		{
			// Delete all logs older than a week
			if (!Directory.Exists (UserProfile.Current.LogDir))
				return;
			
			var files = Directory.EnumerateFiles (UserProfile.Current.LogDir)
				.Select (f => new FileInfo (f))
				.Where (f => f.CreationTimeUtc < DateTime.UtcNow.Subtract (TimeSpan.FromDays (7)));

			foreach (var v in files) {
				try {
					v.Delete ();
				} catch (Exception ex) {
					Console.Error.WriteLine (ex);
				}
			}
		}

		static void RedirectOutputToLogFile ()
		{
			try {
				if (Platform.IsWindows) {
					//TODO: redirect the file descriptors on Windows, just plugging in a textwriter won't get everything
					RedirectOutputToFileWindows ();
				} else {
					RedirectOutputToFileUnix ();
				}
			} catch (Exception ex) {
				LogInternalError ("Failed to redirect output to log file", ex);
			}
		}

		static MonoDevelop.Core.ProgressMonitoring.LogTextWriter stderr;
		static MonoDevelop.Core.ProgressMonitoring.LogTextWriter stdout;
		static TextWriter writer;
		static void RedirectOutputToFileWindows ()
		{
			writer = CreateLogFile ("Ide");
			if (writer == Console.Out)
				return;

			stderr = new MonoDevelop.Core.ProgressMonitoring.LogTextWriter ();
			stderr.ChainWriter (Console.Error);
			stderr.ChainWriter (writer);
			defaultError = Console.Error;
			Console.SetError (stderr);

			stdout = new MonoDevelop.Core.ProgressMonitoring.LogTextWriter ();
			stdout.ChainWriter (Console.Out);
			stdout.ChainWriter (writer);
			defaultOut = Console.Out;
			Console.SetOut (stdout);
		}
		
		static void RedirectOutputToFileUnix ()
		{
			const int STDOUT_FILENO = 1;
			const int STDERR_FILENO = 2;
			
			const OpenFlags flags = OpenFlags.O_WRONLY | OpenFlags.O_CREAT | OpenFlags.O_TRUNC;

			const FilePermissions mode =
				FilePermissions.S_IFREG | FilePermissions.S_IRUSR | FilePermissions.S_IWUSR |
				FilePermissions.S_IRGRP | FilePermissions.S_IWGRP;

			FilePath logDir = UserProfile.Current.LogDir;
			Directory.CreateDirectory (logDir);

			int fd;
			string logFile;
			int oldIdx = logFileSuffix;

			while (true) {
				logFile = logDir.Combine (GetSessionLogFileName ("Ide"));

				// if the file already exists, retry with a suffix, up to 10 times
				fd = Syscall.open (logFile, flags, mode);
				if (fd >= 0)
					break;

				var err = Stdlib.GetLastError ();
				if (logFileSuffix >= oldIdx + 10 || err != Errno.EEXIST) {
					logFileSuffix++;
					continue;
				}
				throw new IOException ("Unable to open file: " + err);
			}

			try {
				int res = Syscall.dup2 (fd, STDOUT_FILENO);
				if (res < 0)
					throw new IOException ("Unable to redirect stdout: " + Stdlib.GetLastError ());
				
				res = Syscall.dup2 (fd, STDERR_FILENO);
				if (res < 0)
					throw new IOException ("Unable to redirect stderr: " + Stdlib.GetLastError ());

				//try to symlink timestamped file to generic one. NBD if it fails.
				SymlinkWithRetry (logFile, logDir.Combine ("Ide.log"), 10);
			} finally {
				Syscall.close (fd);
			}
		}

		static bool SymlinkWithRetry (string from, string to, int retries)
		{
			for (int i = 0; i < retries; i++) {
				Syscall.unlink (to);
				if (Syscall.symlink (from, to) >= 0)
					return true;
			}
			return false;
		}

		static void RestoreOutputRedirection ()
		{
			if (defaultError != null)
				Console.SetError (defaultError);
			if (defaultOut != null)
				Console.SetOut (defaultOut);
		}
		
		internal static RemoteLogger RemoteLogger {
			get {
				if (remoteLogger == null)
					remoteLogger = new RemoteLogger ();
				return remoteLogger;
			}
		}

#region the core service
		
		public static bool IsLevelEnabled (LogLevel level)
		{
			var l = (EnabledLoggingLevel) level;
			foreach (ILogger logger in loggers)
				if ((logger.EnabledLevel & l) == l)
					return true;
			return false;
		}
		
		public static void Log (LogLevel level, string message)
		{
			var l = (EnabledLoggingLevel) level;
			foreach (ILogger logger in loggers)
				if ((logger.EnabledLevel & l) == l)
					logger.Log (level, message);
		}
		
#endregion
		
#region methods to access/add/remove loggers -- this service is essentially a log message broadcaster
		
		public static ILogger GetLogger (string name)
		{
			foreach (ILogger logger in loggers)
				if (logger.Name == name)
					return logger;
			return null;
		}
		
		public static void AddLogger (ILogger logger)
		{
			lock (serviceLock) {
				if (GetLogger (logger.Name) != null)
					throw new Exception ("There is already a logger with the name '" + logger.Name + "'");
				loggers = loggers.Add (logger);
			}
		}
		
		public static void RemoveLogger (string name)
		{
			lock (serviceLock) {
				ILogger logger = GetLogger (name);
				if (logger == null)
					throw new Exception ("There is no logger registered with the name '" + name + "'");
				loggers = loggers.Remove (logger);
			}
		}
		
#endregion
		
#region convenience methods (string message)
		
		public static void LogDebug (string message)
		{
			Log (LogLevel.Debug, message);
		}
		
		public static void LogInfo (string message)
		{
			Log (LogLevel.Info, message);
		}
		
		public static void LogWarning (string message)
		{
			Log (LogLevel.Warn, message);
		}
		
		public static void LogError (string message)
		{
			Log (LogLevel.Error, message);
		}
		
		public static void LogFatalError (string message)
		{
			Log (LogLevel.Fatal, message);
		}

#endregion
		
#region convenience methods (string messageFormat, params object[] args)
		
		public static void LogDebug (string messageFormat, params object[] args)
		{
			Log (LogLevel.Debug, string.Format (messageFormat, args));
		}
		
		public static void LogInfo (string messageFormat, params object[] args)
		{
			Log (LogLevel.Info, string.Format (messageFormat, args));
		}
		
		public static void LogWarning (string messageFormat, params object[] args)
		{
			Log (LogLevel.Warn, string.Format (messageFormat, args));
		}

		public static void LogUserError (string messageFormat, params object[] args)
		{
			Log (LogLevel.Error, string.Format (messageFormat, args));
		}
		
		public static void LogError (string messageFormat, params object[] args)
		{
			LogUserError (messageFormat, args);
		}

		public static void LogFatalError (string messageFormat, params object[] args)
		{
			Log (LogLevel.Fatal, string.Format (messageFormat, args));
		}
		
#endregion
		
#region convenience methods (string message, Exception ex)

		static string FormatExceptionText (string message, Exception ex)
		{
			if (ex == null)
				return message;

			var exceptionText = new StringBuilder ();
			exceptionText.AppendLine (message);
			exceptionText.Append (ex);
			if (ex.Data.Count > 0) {
				exceptionText.AppendLine ();
				exceptionText.Append ("Exception Data:");
				foreach (DictionaryEntry item in ex.Data) {
					exceptionText.AppendLine ();
					exceptionText.AppendFormat ("{0}: {1}", item.Key, item.Value);
				}
			}
			return exceptionText.ToString ();
		}

		public static void LogDebug (string message, Exception ex)
		{
			Log (LogLevel.Debug, FormatExceptionText (message, ex));
		}
		
		public static void LogInfo (string message, Exception ex)
		{
			Log (LogLevel.Info, FormatExceptionText (message, ex));
		}
		
		public static void LogWarning (string message, Exception ex)
		{
			Log (LogLevel.Warn, FormatExceptionText (message, ex));
		}
		
		public static void LogError (string message, Exception ex)
		{
			Log (LogLevel.Error, message + (ex != null? Environment.NewLine + ex : string.Empty));
		}

		[Obsolete ("Use LogError")]
		public static void LogUserError (string message, Exception ex)
		{
			Log (LogLevel.Error, FormatExceptionText (message, ex));
		}

		/// <summary>
		/// Reports that an unexpected error has occurred, but the IDE will continue executing.
		/// Error information is sent to the crash reporting service
		/// </summary>
		/// <param name="ex">Exception</param>
		public static void LogInternalError (Exception ex)
		{
			if (ex != null) {
				Log (LogLevel.Error, Environment.NewLine + ex);
			}

			ReportUnhandledException (ex, false, true, "internal");
		}

		/// <summary>
		/// Reports that an unexpected error has occurred, but the IDE will continue executing.
		/// Error information is sent to the crash reporting service
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="ex">Exception</param>
		public static void LogInternalError (string message, Exception ex)
		{
			Log (LogLevel.Error, FormatExceptionText (message, ex));

			ReportUnhandledException (ex, false, true, "internal");
		}

		[Obsolete ("Use LogInternalError")]
		public static void LogCriticalError (string message, Exception ex)
		{
			LogInternalError (message, ex);
		}

		/// <summary>
		/// Reports that a fatal error has occurred, and that the IDE will shut down.
		/// Error information is sent to the crash reporting service
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="ex">Exception</param>
		public static void LogFatalError (string message, Exception ex)
		{
			Log (LogLevel.Fatal, FormatExceptionText (message, ex));

			ReportUnhandledException (ex, true, false, "fatal");
		}

#endregion
	}
}
