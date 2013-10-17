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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.IO.Compression;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.Messages;

using MonoDevelop.Core.Logging;

namespace MonoDevelop.Core
{
	public static class LoggingService
	{
		const string ServiceVersion = "1";
		const string ReportCrashesKey = "MonoDevelop.LogAgent.ReportCrashes";
		const string ReportUsageKey = "MonoDevelop.LogAgent.ReportUsage";

		public static readonly FilePath CrashLogDirectory = UserProfile.Current.LogDir.Combine ("LogAgent");

		static RaygunClient raygunClient = null;
		static List<ILogger> loggers = new List<ILogger> ();
		static RemoteLogger remoteLogger;
		static DateTime timestamp;
		static TextWriter defaultError;
		static TextWriter defaultOut;
		static bool reporting;
		static int CrashId;
		static int Processing;

		// Return value is the new value for 'ReportCrashes'
		// First parameter is the current value of 'ReportCrashes
		// Second parameter is the exception
		// Thirdparameter shows if the exception is fatal or not
		public static Func<bool?, Exception, bool, bool?> UnhandledErrorOccured;

		static LoggingService ()
		{
			ConsoleLogger consoleLogger = new ConsoleLogger ();
			loggers.Add (consoleLogger);
			loggers.Add (new InstrumentationLogger ());
			
			string consoleLogLevelEnv = Environment.GetEnvironmentVariable ("MONODEVELOP_CONSOLE_LOG_LEVEL");
			if (!string.IsNullOrEmpty (consoleLogLevelEnv)) {
				try {
					consoleLogger.EnabledLevel = (EnabledLoggingLevel) Enum.Parse (typeof (EnabledLoggingLevel), consoleLogLevelEnv, true);
				} catch {}
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
					FileLogger fileLogger = new FileLogger (logFileEnv);
					loggers.Add (fileLogger);
					string logFileLevelEnv = Environment.GetEnvironmentVariable ("MONODEVELOP_FILE_LOG_LEVEL");
					fileLogger.EnabledLevel = (EnabledLoggingLevel) Enum.Parse (typeof (EnabledLoggingLevel), logFileLevelEnv, true);
				} catch (Exception e) {
					LogError (e.ToString ());
				}
			}

			timestamp = DateTime.Now;

#if ENABLE_RAYGUN
			string raygunKey = BrandingService.GetString ("RaygunApiKey");
			if (raygunKey != null) {
				raygunClient = new RaygunClient (raygunKey);
			}
#endif

			//remove the default trace listener on .NET, it throws up horrible dialog boxes for asserts
			System.Diagnostics.Debug.Listeners.Clear ();

			//add a new listener that just logs failed asserts
			System.Diagnostics.Debug.Listeners.Add (new AssertLoggingTraceListener ());
		}

		public static bool? ReportCrashes {
			get { return PropertyService.Get<bool?> (ReportCrashesKey); }
			set { PropertyService.Set (ReportCrashesKey, value); }
		}

		public static bool? ReportUsage {
			get { return PropertyService.Get<bool?> (ReportUsageKey); }
			set { PropertyService.Set (ReportUsageKey, value); }
		}

		static string GenericLogFile {
			get { return "Ide.log"; }
		}
		
		public static DateTime LogTimestamp {
			get { return timestamp; }
		}

		static string UniqueLogFile {
			get {
				return string.Format ("Ide.{0}.log", timestamp.ToString ("yyyy-MM-dd__HH-mm-ss"));
			}
		}
		
		public static void Initialize (bool redirectOutput)
		{
			PurgeOldLogs ();

			// Always redirect on windows otherwise we cannot get output at all
			if (Platform.IsWindows || redirectOutput)
				RedirectOutputToLogFile ();
		}
		
		public static void Shutdown ()
		{
			RestoreOutputRedirection ();
		}

		internal static void ReportUnhandledException (Exception ex, bool willShutDown)
		{
			ReportUnhandledException (ex, willShutDown, false, null);
		}

		internal static void ReportUnhandledException (Exception ex, bool willShutDown, bool silently)
		{
			ReportUnhandledException (ex, willShutDown, silently);
		}

		internal static void ReportUnhandledException (Exception ex, bool willShutDown, bool silently, string tag)
		{
			var tags = new List<string> { tag };

			if (reporting)
				return;

			reporting = true;
			try {
				var oldReportCrashes = ReportCrashes;

				if (UnhandledErrorOccured != null && !silently)
					ReportCrashes = UnhandledErrorOccured (ReportCrashes, ex, willShutDown);

				// If crash reporting has been explicitly disabled, disregard this crash
				if (ReportCrashes.HasValue && !ReportCrashes.Value)
					return;

				byte[] data;
				using (var stream = new MemoryStream ()) {
					using (var writer = System.Xml.XmlWriter.Create (stream)) {
						writer.WriteStartElement ("CrashLog");
						writer.WriteAttributeString ("version", ServiceVersion);

						writer.WriteElementString ("SystemInformation", SystemInformation.GetTextDescription ());
						writer.WriteElementString ("Exception", ex.ToString ());

						writer.WriteEndElement ();
					}
					data = stream.ToArray ();
				}

				if (raygunClient != null) {
					ThreadPool.QueueUserWorkItem (delegate {
						raygunClient.Send (ex, tags);
					});
				}

				// Log to disk only if uploading fails.
				var filename = string.Format ("{0}.{1}.{2}.crashlog", DateTime.UtcNow.ToString ("yyyy-MM-dd__HH-mm-ss"), SystemInformation.SessionUuid, Interlocked.Increment (ref CrashId));
				ThreadPool.QueueUserWorkItem (delegate {
					if (!TryUploadReport (filename, data)) {
						if (!Directory.Exists (CrashLogDirectory))
							Directory.CreateDirectory (CrashLogDirectory);

						File.WriteAllBytes (CrashLogDirectory.Combine (filename), data);
					}
				});

				//ensure we don't lose the setting
				if (ReportCrashes != oldReportCrashes) {
					PropertyService.SaveProperties ();
				}

			} finally {
				reporting = false;
			}
		}

		public static void ProcessCache ()
		{
			int origValue = -1;
			try {
				// Ensure only 1 thread at a time attempts to upload cached reports
				origValue = Interlocked.CompareExchange (ref Processing, 1, 0);
				if (origValue != 0)
					return;

				// Uploading is not enabled, so bail out
				if (!ReportCrashes.GetValueOrDefault ())
					return;

				// Definitely no crash reports if this doesn't exist
				if (!Directory.Exists (CrashLogDirectory))
					return;

				foreach (var file in Directory.GetFiles (CrashLogDirectory)) {
					if (TryUploadReport (file, File.ReadAllBytes (file)))
						File.Delete (file);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Exception processing cached crashes", ex);
			} finally {
				if (origValue == 0)
					Interlocked.CompareExchange (ref Processing, 0, 1);
			}
		}

		static bool TryUploadReport (string filename, byte[] data)
		{
			try {
				// Empty files won't be accepted by the server as it thinks 'ContentLength' has not been set as it's
				// zero. We don't need empty files anyway.
				if (data.Length == 0)
					return true;

				var server = Environment.GetEnvironmentVariable ("MONODEVELOP_CRASHREPORT_SERVER");
				if (string.IsNullOrEmpty (server))
					server = "monodevlog.xamarin.com:35162";

				var request = (HttpWebRequest) WebRequest.Create (string.Format ("http://{0}/logagentreport/", server));
				request.Headers.Add ("LogAgentVersion", ServiceVersion);
				request.Headers.Add ("LogAgent_Filename", Path.GetFileName (filename));
				request.Headers.Add ("Content-Encoding", "gzip");
				request.Method = "POST";

				// Compress the data and then use the compressed length in ContentLength
				var compressed = new MemoryStream ();
				using (var zipper = new GZipStream (compressed, CompressionMode.Compress))
					zipper.Write (data, 0, data.Length);
				data = compressed.ToArray ();

				request.ContentLength = data.Length;
				using (var requestStream = request.GetRequestStream ())
					requestStream.Write (data, 0, data.Length);

				LoggingService.LogDebug ("CrashReport sent to server, awaiting response...");

				// Ensure the server has correctly processed everything.
				using (var response = (HttpWebResponse) request.GetResponse ()) {
					if (response.StatusCode != HttpStatusCode.OK) {
						LoggingService.LogError ("Server responded with status code {1} and error: {0}", response.StatusDescription, response.StatusCode);
						return false;
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Failed to upload report to the server", ex);
				return false;
			}

			LoggingService.LogDebug ("Successfully uploaded crash report");
			return true;
		}

		static void PurgeOldLogs ()
		{
			// Delete all logs older than a week
			if (!Directory.Exists (UserProfile.Current.LogDir))
				return;

			// HACK: we were using EnumerateFiles but it's broken in some Mono releases
			// https://bugzilla.xamarin.com/show_bug.cgi?id=2975
			var files = Directory.GetFiles (UserProfile.Current.LogDir)
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
			FilePath logDir = UserProfile.Current.LogDir;
			if (!Directory.Exists (logDir))
				Directory.CreateDirectory (logDir);
			
			try {
				if (Platform.IsWindows) {
					//TODO: redirect the file descriptors on Windows, just plugging in a textwriter won't get everything
					RedirectOutputToFileWindows (logDir, UniqueLogFile);
				} else {
					RedirectOutputToFileUnix (logDir, UniqueLogFile);
				}
			} catch (Exception ex) {
				Console.Error.WriteLine (ex);
			}
		}

		static void RedirectOutputToFileWindows (FilePath logDirectory, string logName)
		{
			var stream = File.Open (logDirectory.Combine (logName), FileMode.Create, FileAccess.Write, FileShare.Read);
			var writer = new StreamWriter (stream) { AutoFlush = true };
			
			var stderr = new MonoDevelop.Core.ProgressMonitoring.LogTextWriter ();
			stderr.ChainWriter (Console.Error);
			stderr.ChainWriter (writer);
			defaultError = Console.Error;
			Console.SetError (stderr);

			var stdout = new MonoDevelop.Core.ProgressMonitoring.LogTextWriter ();
			stdout.ChainWriter (Console.Out);
			stdout.ChainWriter (writer);
			defaultOut = Console.Out;
			Console.SetOut (stdout);
		}
		
		static void RedirectOutputToFileUnix (FilePath logDirectory, string logName)
		{
			const int STDOUT_FILENO = 1;
			const int STDERR_FILENO = 2;
			
			Mono.Unix.Native.OpenFlags flags = Mono.Unix.Native.OpenFlags.O_WRONLY
				| Mono.Unix.Native.OpenFlags.O_CREAT | Mono.Unix.Native.OpenFlags.O_TRUNC;
			var mode = Mono.Unix.Native.FilePermissions.S_IFREG
				| Mono.Unix.Native.FilePermissions.S_IRUSR | Mono.Unix.Native.FilePermissions.S_IWUSR
				| Mono.Unix.Native.FilePermissions.S_IRGRP | Mono.Unix.Native.FilePermissions.S_IWGRP;
			
			var file = logDirectory.Combine (logName);
			int fd = Mono.Unix.Native.Syscall.open (file, flags, mode);
			if (fd < 0)
				//error
				return;
			try {
				int res = Mono.Unix.Native.Syscall.dup2 (fd, STDOUT_FILENO);
				if (res < 0)
					//error
					return;
				
				res = Mono.Unix.Native.Syscall.dup2 (fd, STDERR_FILENO);
				if (res < 0)
					//error
					return;

				var genericLog = logDirectory.Combine (GenericLogFile);
				File.Delete (genericLog);
				Mono.Unix.Native.Syscall.symlink (file, genericLog);
			} finally {
				Mono.Unix.Native.Syscall.close (fd);
			}
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
			EnabledLoggingLevel l = (EnabledLoggingLevel) level;
			foreach (ILogger logger in loggers)
				if ((logger.EnabledLevel & l) == l)
					return true;
			return false;
		}
		
		public static void Log (LogLevel level, string message)
		{
			EnabledLoggingLevel l = (EnabledLoggingLevel) level;
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
			if (GetLogger (logger.Name) != null)
				throw new Exception ("There is already a logger with the name '" + logger.Name + "'");
			loggers.Add (logger);
		}
		
		public static void RemoveLogger (string name)
		{
			ILogger logger = GetLogger (name);
			if (logger == null)
				throw new Exception ("There is no logger registered with the name '" + name + "'");
			loggers.Remove (logger);
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
		
		public static void LogDebug (string message, Exception ex)
		{
			Log (LogLevel.Debug, message + (ex != null? System.Environment.NewLine + ex.ToString () : string.Empty));
		}
		
		public static void LogInfo (string message, Exception ex)
		{
			Log (LogLevel.Info, message + (ex != null? System.Environment.NewLine + ex.ToString () : string.Empty));
		}
		
		public static void LogWarning (string message, Exception ex)
		{
			Log (LogLevel.Warn, message + (ex != null? System.Environment.NewLine + ex.ToString () : string.Empty));
		}
		
		public static void LogError (string message, Exception ex)
		{
			LogUserError (message, ex);
		}

		public static void LogUserError (string message, Exception ex)
		{
			Log (LogLevel.Error, message + (ex != null? System.Environment.NewLine + ex.ToString () : string.Empty));
		}

		public static void LogInternalError (Exception ex)
		{
			if (ex != null) {
				Log (LogLevel.Error, System.Environment.NewLine + ex.ToString ());
			}

			ReportUnhandledException (ex, false, true, "internal");
		}

		public static void LogInternalError (string message, Exception ex)
		{
			Log (LogLevel.Error, message + (ex != null? System.Environment.NewLine + ex.ToString () : string.Empty));

			ReportUnhandledException (ex, false, true, "internal");
		}

		public static void LogCriticalError (string message, Exception ex)
		{
			Log (LogLevel.Error, message + (ex != null? System.Environment.NewLine + ex.ToString () : string.Empty));

			ReportUnhandledException (ex, false, false, "critical");
		}

		public static void LogFatalError (string message, Exception ex)
		{
			Log (LogLevel.Error, message + (ex != null? System.Environment.NewLine + ex.ToString () : string.Empty));

			ReportUnhandledException (ex, true, false, "fatal");
		}

#endregion
	}

}
