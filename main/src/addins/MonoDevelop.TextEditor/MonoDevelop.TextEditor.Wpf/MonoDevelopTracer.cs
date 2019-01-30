//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.Diagnostics;
using MonoDevelop.Core;

namespace MonoDevelop.TextEditor
{
	class MonoDevelopTracer : Microsoft.VisualStudio.Utilities.ITracer
	{
		public MonoDevelopTracer (string name, SourceLevels level = SourceLevels.Warning)
		{
			Source = new TraceSource (name);
			Level = level;
		}

		public TraceSource Source { get; }

		public SourceLevels Level { get; set; }

		public int IndentLevel => 0;

		public IDisposable Indent (int count = 1)
		{
			return null;
		}

		public bool ShouldTrace (TraceEventType eventType)
		{
			switch (eventType) {
			case TraceEventType.Critical:
				return Level.HasFlag (SourceLevels.Critical);
			case TraceEventType.Error:
				return Level.HasFlag (SourceLevels.Error);
			case TraceEventType.Warning:
				return Level.HasFlag (SourceLevels.Warning);
			case TraceEventType.Information:
				return Level.HasFlag (SourceLevels.Information);
			case TraceEventType.Verbose:
				return Level.HasFlag (SourceLevels.Verbose);
			}
			return false;
		}

		public void Trace (TraceEventType eventType, string message)
		{
			if (ShouldTrace (eventType)) {
				switch (eventType) {
				case TraceEventType.Critical:
					LoggingService.LogFatalError (message);
					break;
				case TraceEventType.Error:
					LoggingService.LogError (message);
					break;
				case TraceEventType.Warning:
					LoggingService.LogWarning (message);
					break;
				case TraceEventType.Information:
					LoggingService.LogInfo (message);
					break;
				case TraceEventType.Verbose:
					LoggingService.LogDebug (message);
					break;
				}
			}
		}

		public void Trace (TraceEventType eventType, string message, object arg0)
		{
			if (ShouldTrace (eventType)) {
				switch (eventType) {
				case TraceEventType.Critical:
					LoggingService.LogFatalError (message, arg0);
					break;
				case TraceEventType.Error:
					LoggingService.LogError (message, arg0);
					break;
				case TraceEventType.Warning:
					LoggingService.LogWarning (message, arg0);
					break;
				case TraceEventType.Information:
					LoggingService.LogInfo (message, arg0);
					break;
				case TraceEventType.Verbose:
					LoggingService.LogDebug (message, arg0);
					break;
				}
			}
		}

		public void Trace (TraceEventType eventType, string message, object arg0, object arg1)
		{
			if (ShouldTrace (eventType)) {
				switch (eventType) {
				case TraceEventType.Critical:
					LoggingService.LogFatalError (message, arg0, arg1);
					break;
				case TraceEventType.Error:
					LoggingService.LogError (message, arg0, arg1);
					break;
				case TraceEventType.Warning:
					LoggingService.LogWarning (message, arg0, arg1);
					break;
				case TraceEventType.Information:
					LoggingService.LogInfo (message, arg0, arg1);
					break;
				case TraceEventType.Verbose:
					LoggingService.LogDebug (message, arg0, arg1);
					break;
				}
			}
		}

		public void Trace (TraceEventType eventType, string message, params object[] args)
		{
			if (ShouldTrace (eventType)) {
				switch (eventType) {
				case TraceEventType.Critical:
					LoggingService.LogFatalError (message, args);
					break;
				case TraceEventType.Error:
					LoggingService.LogError (message, args);
					break;
				case TraceEventType.Warning:
					LoggingService.LogWarning (message, args);
					break;
				case TraceEventType.Information:
					LoggingService.LogInfo (message, args);
					break;
				case TraceEventType.Verbose:
					LoggingService.LogDebug (message, args);
					break;
				}
			}
		}

		public void TraceError (string message)
		{
			LoggingService.LogError (message);
		}

		public void TraceError (string message, object arg0)
		{
			LoggingService.LogError (message, arg0);
		}

		public void TraceError (string message, object arg0, object arg1)
		{
			LoggingService.LogError (message, arg0, arg1);
		}

		public void TraceError (string message, params object[] args)
		{
			LoggingService.LogError (message, args);
		}

		public void TraceException (Exception ex, TraceEventType eventType = TraceEventType.Error)
		{
			LoggingService.LogError (Source.Name, ex);
		}

		public void TraceInformation (string message)
		{
			LoggingService.LogInfo (message);
		}

		public void TraceInformation (string message, object arg0)
		{
			LoggingService.LogInfo (message, arg0);
		}

		public void TraceInformation (string message, object arg0, object arg1)
		{
			LoggingService.LogInfo (message, arg0, arg1);
		}

		public void TraceInformation (string message, params object[] args)
		{
			LoggingService.LogInfo (message, args);
		}

		public void TraceVerbose (string message)
		{
			LoggingService.LogDebug (message);
		}

		public void TraceVerbose (string message, object arg0)
		{
			LoggingService.LogDebug (message, arg0);
		}

		public void TraceVerbose (string message, object arg0, object arg1)
		{
			LoggingService.LogDebug (message, arg1);
		}

		public void TraceVerbose (string message, params object[] args)
		{
			LoggingService.LogDebug (message, args);
		}

		public void TraceWarning (string message)
		{
			LoggingService.LogWarning (message);
		}

		public void TraceWarning (string message, object arg0)
		{
			LoggingService.LogWarning (message, arg0);
		}

		public void TraceWarning (string message, object arg0, object arg1)
		{
			LoggingService.LogWarning (message, arg0, arg1);
		}

		public void TraceWarning (string message, params object[] args)
		{
			LoggingService.LogWarning (message, args);
		}
	}
}
