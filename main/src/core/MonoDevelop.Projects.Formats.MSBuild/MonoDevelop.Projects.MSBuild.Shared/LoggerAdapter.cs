//
// LoggerAdapter.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MonoDevelop.Projects.MSBuild
{
	/// <summary>
	/// This class is used to bridge msbuild loggers to MD loggers
	/// </summary>
	class LoggerAdapter: IDisposable
	{
		IEngineLogWriter currentLogWriter;
		StringBuilder log = new StringBuilder ();
		List<LogEvent> logMessages = new List<LogEvent> ();

		bool flushingLog;
		Timer flushTimer;
		object flushLogLock = new object ();
		const int LogFlushTimeout = 100;

		/// <summary>
		/// Prepares the logging infrastructure
		/// </summary>
		public LoggerAdapter (IEngineLogWriter logWriter)
		{
			currentLogWriter = logWriter;
			if (currentLogWriter != null) {
				log.Clear ();
				logMessages.Clear ();
				flushingLog = false;
				flushTimer = new Timer (o => FlushLog ());
			}
		}

		public virtual IEngineLogWriter EngineLogWriter {
			get { return currentLogWriter; }
			set {
				lock (flushLogLock) {
					FlushLog ();
					currentLogWriter = value;
				}
			}
		}

		/// <summary>
		/// Flushes the log that has not yet been sent and disposes the logging infrastructure
		/// </summary>
		public void Dispose ()
		{
			lock (flushLogLock)
				lock (log) {
					if (currentLogWriter != null) {
						try {
							flushTimer.Dispose ();
							FlushLog ();
						} catch {
							// Ignoree
						} finally {
							// This needs to be done inside the finally, to make sure it is called even in
							// the case the thread is being aborted.
							flushTimer = null;
							currentLogWriter = null;
						}
					}
				}
		}

		public void LogWriteLine (string txt)
		{
			LogWrite (txt + Environment.NewLine);
		}

		public void LogWrite (string txt)
		{
			lock (log) {
				if (currentLogWriter != null) {
					// Append the line to the log, and schedule the flush of the log, unless it has already been done
					log.Append (txt);
					if (!flushingLog) {
						// Flush the log after 100ms
						flushingLog = true;
						flushTimer.Change (LogFlushTimeout, Timeout.Infinite);
					}
				}
			}
		}

		public void LogEvent (LogEvent msg)
		{
			lock (log) {
				if (currentLogWriter != null) {
					// Append the line to the log, and schedule the flush of the log, unless it has already been done
					logMessages.Add (msg);
					if (!flushingLog) {
						// Flush the log after 100ms
						flushingLog = true;
						flushTimer.Change (LogFlushTimeout, Timeout.Infinite);
					}
				}
			}
		}

		void FlushLog ()
		{
			// We need a lock for the whole method here because it is called from the timer
			// and from DisposeLogger, and we want to make sure a flush is complete before
			// trying another one

			lock (flushLogLock) {
				string txt;
				List<LogEvent> messages;
				lock (log) {
					// Don't flush the log inside the lock since that would prevent LogWriteLine from writing
					// more log while the current log is being flushed (that would slow down the whole build)
					txt = log.ToString ();
					log.Clear ();
					messages = new List<LogEvent> (logMessages);
					logMessages.Clear ();
					flushingLog = false;
				}
				if (currentLogWriter != null && (txt.Length > 0 || messages.Count > 0))
					currentLogWriter.Write (txt.Length > 0 ? txt : null, messages.Count > 0 ? messages.ToArray () : null);
			}
		}

	}
}