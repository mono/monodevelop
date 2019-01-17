﻿//
// ProjectBuilder.Shared.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2009-2011 Novell, Inc (http://www.novell.com)
// Copyright (c) 2011-2015 Xamarin Inc. (http://www.xamarin.com)
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

using Microsoft.Build.Framework;
using System.Xml;
using System.IO;
using System;
using System.Text;
using System.Threading;

namespace MonoDevelop.Projects.MSBuild
{
	partial class ProjectBuilder
	{
		ILogWriter currentLogWriter;
		StringBuilder log = new StringBuilder ();
		bool flushingLog;
		Timer flushTimer;
		object flushLogLock = new object ();
		const int LogFlushTimeout = 100;

		public void Dispose ()
		{
			buildEngine.UnloadProject (file);
		}

		public void Refresh ()
		{
			buildEngine.UnloadProject (file);
		}

		public void RefreshWithContent (string projectContent)
		{
			buildEngine.UnloadProject (file);
			buildEngine.SetUnsavedProjectContent (file, projectContent);
		}

		/// <summary>
		/// Prepares the logging infrastructure
		/// </summary>
		void InitLogger (ILogWriter logWriter)
		{
			currentLogWriter = logWriter;
			if (currentLogWriter != null) {
				log.Clear ();
				flushingLog = false;
				flushTimer = new Timer (o => FlushLog ());
			}
		}

		/// <summary>
		/// Flushes the log that has not yet been sent and disposes the logging infrastructure
		/// </summary>
		void DisposeLogger ()
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

		void LogWriteLine (string txt)
		{
			LogWrite (txt + Environment.NewLine);
		}

		void LogWrite (string txt)
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

		void FlushLog ()
		{
			// We need a lock for the whole method here because it is called from the timer
			// and from DisposeLogger, and we want to make sure a flush is complete before
			// trying another one

			lock (flushLogLock) {
				string txt;
				lock (log) {
					// Don't flush the log inside the lock since that would prevent LogWriteLine from writing
					// more log while the current log is being flushed (that would slow down the whole build)
					txt = log.ToString ();
					log.Clear ();
					flushingLog = false;
				}
				if (txt.Length > 0 && currentLogWriter != null)
					currentLogWriter.Write (txt);
			}
		}

		LoggerVerbosity GetVerbosity (MSBuildVerbosity verbosity)
		{
			switch (verbosity) {
			case MSBuildVerbosity.Quiet:
				return LoggerVerbosity.Quiet;
			case MSBuildVerbosity.Minimal:
				return LoggerVerbosity.Minimal;
			default:
				return LoggerVerbosity.Normal;
			case MSBuildVerbosity.Detailed:
				return LoggerVerbosity.Detailed;
			case MSBuildVerbosity.Diagnostic:
				return LoggerVerbosity.Diagnostic;
			}
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}

		//from MSBuildProjectService
		static string UnescapeString (string str)
		{
			int i = str.IndexOf ('%');
			while (i != -1 && i < str.Length - 2) {
				int c;
				if (int.TryParse (str.Substring (i+1, 2), System.Globalization.NumberStyles.HexNumber, null, out c))
					str = str.Substring (0, i) + (char) c + str.Substring (i + 3);
				i = str.IndexOf ('%', i + 1);
			}
			return str;
		}

		string GenerateSolutionConfigurationContents (ProjectConfigurationInfo[] configurations)
		{
			// can't use XDocument because of the 2.0 builder
			// and don't just build a string because things may need escaping

			var doc = new XmlDocument ();
			var root = doc.CreateElement ("SolutionConfiguration");
			doc.AppendChild (root);
			foreach (var config in configurations) {
				var el = doc.CreateElement ("ProjectConfiguration");
				root.AppendChild (el);
				el.SetAttribute ("Project", config.ProjectGuid);
				el.SetAttribute ("AbsolutePath", config.ProjectFile);
				el.SetAttribute ("BuildProjectInSolution", config.Enabled ? "True" : "False");
				el.InnerText = string.Format (config.Configuration + "|" + config.Platform);
			}

			//match MSBuild formatting
			var options = new XmlWriterSettings {
				Indent = true,
				IndentChars = "",
				OmitXmlDeclaration = true,
			};
			using (var sw = new StringWriter ())
			using (var xw = XmlWriter.Create (sw, options)) {
				doc.WriteTo (xw);
				xw.Flush ();
				return sw.ToString ();
			}
		}
	}
}

