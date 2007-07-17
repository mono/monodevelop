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

namespace MonoDevelop.Ide.Gui
{
	class InternalLog
	{
		static List<LogAppendedArgs> messages = new List<LogAppendedArgs> ();
		static IStatusIcon errorIcon;
		static LogAppendedArgs lastError;
		static bool errorNotificationEnabled;
		
		public static int ErrorCount;
		public static int WarningCount;
		public static int InfoCount;
		public static int DebugCount;
		
		public const string Error = "Error";
		public const string Fatal = "Fatal";
		public const string Warning = "Warn";
		public const string Info = "Info";
		public const string Debug = "Debug";
		
		public static void Initialize ()
		{
			Runtime.LoggingService.LogAppended += OnLogAppended;
		}
		
		public static List<MonoDevelop.Core.LogAppendedArgs> Messages {
			get {
				return messages;
			}
		}
		
		public static void Reset ()
		{
			lock (messages) {
				messages.Clear ();
				ErrorCount = WarningCount = InfoCount = DebugCount = 0;
			}
		}
		
		static void OnLogAppended (object sender, LogAppendedArgs args)
		{
			lock (messages) {
				messages.Add (args);
				switch (args.Level) {
					case InternalLog.Fatal: ErrorCount++; break;
					case InternalLog.Error: ErrorCount++; break;
					case InternalLog.Warning: WarningCount++; break;
					case InternalLog.Info: InfoCount++; break;
					case InternalLog.Debug: DebugCount++; break;
				}
			}
			if (args.Level == InternalLog.Error || args.Level == InternalLog.Fatal) {
				if (errorNotificationEnabled) {
					Gtk.Application.Invoke (delegate {
						NotifyError (args);
					});
				}
				else
					lastError = args;
			}
		}

		static void NotifyError (LogAppendedArgs args)
		{
			ClearErrorIcon ();
			Gdk.Pixbuf pix = IdeApp.Services.Resources.GetIcon (Gtk.Stock.DialogError, Gtk.IconSize.Menu);
			errorIcon = IdeApp.Workbench.StatusBar.ShowStatusIcon (pix);
			errorIcon.EventBox.ButtonPressEvent += new Gtk.ButtonPressEventHandler (OnShowLogPad);
			errorIcon.SetAlertMode (5);
			errorIcon.ToolTip = args.Message;
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
			if (lastError != null)
				NotifyError (lastError);
		}
		
		static void OnShowLogPad (object s, EventArgs a)
		{
			Pad pad = IdeApp.Workbench.GetPad<InternalLogPad> ();
			pad.BringToFront ();
			ClearErrorIcon ();
		}
	}
}
