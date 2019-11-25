//
// MSBuildLoggerAdapter.cs
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
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Utilities;

namespace MonoDevelop.Projects.MSBuild
{
	class MSBuildLoggerAdapter: LoggerAdapter
	{
		ILogger [] loggers;
		IEventSource eventSource;
		TargetLogger eventsLogger;
		readonly List<MSBuildTargetResult> results = new List<MSBuildTargetResult> ();

		class LocalLogger : Logger
		{
			internal MSBuildLoggerAdapter Adapter;

			public override void Initialize (IEventSource eventSource)
			{
				Adapter.Initialize (eventSource);
			}

			public override void Shutdown ()
			{
				Adapter.Shutdown ();
			}
		}

		/// <summary>
		/// MSBuild loggers bound to the MD loggers
		/// </summary>
		/// <value>The loggers.</value>
		public ILogger [] Loggers {
			get {
				return loggers;
			}
		}

		public MSBuildLoggerAdapter (IEngineLogWriter logWriter, MSBuildVerbosity verbosity) : base (logWriter)
		{
			// LocalLogger will be used to collect build result information.
			// ConsoleLogger is used for the regular build output
			// TargetLogger collects and fordwards build events requested by the client

			var logger = new LocalLogger () { Adapter = this };
			if (logWriter != null) {
				var consoleLogger = new ConsoleLogger (ProjectBuilder.GetVerbosity (verbosity), LogWrite, null, null);
				eventsLogger = new TargetLogger (logWriter.RequiredEvents, LogEvent);
				loggers = new ILogger [] { logger, consoleLogger, eventsLogger };
			} else {
				loggers = new ILogger [] { logger };
			}
		}

		public override IEngineLogWriter EngineLogWriter {
			get {
				return base.EngineLogWriter;
			}
			set {
				base.EngineLogWriter = value;
				eventsLogger.EventFilter = value.RequiredEvents;
			}
		}

		public List<MSBuildTargetResult> BuildResult {
			get { return results; }
		}

		public void AddLogger (ILogger logger)
		{
			var newLoggers = new ILogger [loggers.Length + 1];
			Array.Copy (loggers, newLoggers, loggers.Length);
			newLoggers [loggers.Length] = logger;
			loggers = newLoggers;
		}

		void Initialize (IEventSource eventSource)
		{
			this.eventSource = eventSource;
			eventSource.WarningRaised += EventSourceWarningRaised;
			eventSource.ErrorRaised += EventSourceErrorRaised;
		}

		void Shutdown ()
		{
			eventSource.ErrorRaised -= EventSourceErrorRaised;
			eventSource.WarningRaised -= EventSourceWarningRaised;
		}

		void EventSourceWarningRaised (object sender, BuildWarningEventArgs e)
		{
			results.Add (new MSBuildTargetResult (
				e.ProjectFile, true, e.Subcategory, e.Code, e.File,
				e.LineNumber, e.ColumnNumber, e.ColumnNumber, e.EndLineNumber,
				e.Message, e.HelpKeyword)
			);
		}

		void EventSourceErrorRaised (object sender, BuildErrorEventArgs e)
		{
			results.Add (new MSBuildTargetResult (
				e.ProjectFile, false, e.Subcategory, e.Code, e.File,
				e.LineNumber, e.ColumnNumber, e.ColumnNumber, e.EndLineNumber,
				e.Message, e.HelpKeyword)
			);
		}
	}
}
