//
// InternalLog.cs
//
// Author:
//   Lluis Sanchez Gual
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Core.Logging;

namespace MonoDevelop.Ide.Gui
{
	static class InternalLog
	{
		static StatusBarIcon errorIcon;
		static bool errorNotificationEnabled;
		
		static InternalLogger logger;
		static EnabledLoggingLevel enabledLevel = EnabledLoggingLevel.UpToInfo;
		
		static InternalLog ()
		{
			string logLevelEnv = System.Environment.GetEnvironmentVariable ("MONODEVELOP_LOGGING_PAD_LEVEL");
			if (!string.IsNullOrEmpty (logLevelEnv)) {
				try {
					enabledLevel = (EnabledLoggingLevel) Enum.Parse (typeof (EnabledLoggingLevel), logLevelEnv, true);
				} catch {}
			}
		}
		
		public static void Initialize ()
		{
			if (!Initialized) {
				logger = new InternalLogger ();
				LoggingService.AddLogger (logger);
			}
		}
		
		public static bool Initialized {
			get { return logger != null; }
		}
		
		public static void Dispose ()
		{
			if (Initialized) {
				LoggingService.RemoveLogger (logger.Name);
				logger = null;
			}
		}
		
		public static EnabledLoggingLevel EnabledLoggingLevel {
			get { return enabledLevel; }
		}
		
		static void NotifyError (LogMessage message)
		{
			if (!errorNotificationEnabled)
				return;
			ClearErrorIcon ();
			var pix = ImageService.GetIcon (Gtk.Stock.DialogError, Gtk.IconSize.Menu);
			errorIcon = IdeApp.Workbench.StatusBar.ShowStatusIcon (pix);
			errorIcon.Clicked += delegate {
				ClearErrorIcon ();
				MessageService.ShowError (message.Message);
			};
			errorIcon.SetAlertMode (5);
			errorIcon.ToolTip = message.Message;
		}
		
		public static void ClearErrorIcon ()
		{
			if (errorIcon != null) {
				errorIcon.Dispose ();
				errorIcon = null;
			}
		}
		
		public static void EnableErrorNotification ()
		{
			errorNotificationEnabled = true;
			if (logger.LastError != null)
				NotifyError (logger.LastError);
		}
		
		private class InternalLogger : ILogger
		{
			public LogMessage LastError = null;
			
			public void Log (LogLevel level, string message)
			{
				LogMessage logMessage = new LogMessage (level, message);

				if (level  == LogLevel.Fatal) {
					if (errorNotificationEnabled) {
						Gtk.Application.Invoke (delegate {
							InternalLog.NotifyError (logMessage);
						});
					}
					else
						LastError = logMessage;
				}
			}
			
			public EnabledLoggingLevel EnabledLevel {
				get { return InternalLog.EnabledLoggingLevel; }
			}

			public string Name {
				get { return "MonoDevelop Internal Log"; }
			}
		}
	}
	
	class LogMessage
	{
		LogLevel level;
		string message;
		DateTime timestamp;
		
		public LogLevel Level {
			get { return level; }
		}

		public string Message {
			get { return message; }
		}
		
		public DateTime TimeStamp {
			get { return timestamp; }
		}
		
		public LogMessage (LogLevel level, string message)
		{
			this.level = level;
			this.message = message;
			this.timestamp = DateTime.Now;
		}
		
		public override bool Equals (object o)
		{
			LogMessage m = o as LogMessage;
			if (m != null)
				return (m.level == this.level) && (m.timestamp == this.timestamp) && (m.message == this.message); 
			return false;
		}
		
		public override int GetHashCode ()
		{
			return (((int)level).ToString () + message + timestamp.Ticks.ToString ()).GetHashCode ();
		}


	}
}
