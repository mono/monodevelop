// 
// GLibLogging.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Core;

namespace MonoDevelop.Core.Gui
{
	
	
	public static class GLibLogging
	{
		static readonly string[] domains = new string[] {"Gtk", "Gdk", "GLib", "GObject", "Pango"};
		static uint[] handles;
		
		static Delegate exceptionManagerHook;
		
		public static bool Enabled
		{
			get { return handles == null; }
			set {
				if ((handles == null) == value)
					return;
				
				if (value) {
					handles = new uint[domains.Length];
					HookExceptionManager ();
					for (int i = 0; i < domains.Length; i++)
						handles[i] = GLib.Log.SetLogHandler (domains[i],  GLib.LogLevelFlags.All, LogFunc);
				} else {
					UnhookExceptionManager ();
					for (int i = 0; i < domains.Length; i++)
						GLib.Log.RemoveLogHandler (domains[i], handles[i]);
				}
			}
		}
		
		static void LogFunc (string logDomain, GLib.LogLevelFlags logLevel, string message)
		{
			System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace (2, true);
			string msg = string.Format ("{0}-{1}: {2}\nStack trace: \n{3}", 
			    logDomain, logLevel, message, trace.ToString ());
			
			switch (logLevel) {
			case GLib.LogLevelFlags.Debug:
				LoggingService.LogDebug (msg);
				break;
			case GLib.LogLevelFlags.Info:
				LoggingService.LogInfo (msg);
				break;
			case GLib.LogLevelFlags.Warning:
				LoggingService.LogWarning (msg);
				break;
			case GLib.LogLevelFlags.Error:
			case GLib.LogLevelFlags.Critical:
			default:
				LoggingService.LogError (msg);
				break;
			}	
		}
		
		static void HookExceptionManager ()
		{
			if (exceptionManagerHook != null)
				return;
			
			Type t = typeof(GLib.Object).Assembly.GetType ("GLib.ExceptionManager");
			if (t == null)
				return;
			
			System.Reflection.EventInfo ev = t.GetEvent ("UnhandledException");
			Type delType = typeof(GLib.Object).Assembly.GetType ("GLib.UnhandledExceptionHandler");
			System.Reflection.MethodInfo met = typeof (GLibLogging).GetMethod ("OnUnhandledException", 
			    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			exceptionManagerHook = Delegate.CreateDelegate (delType, met);
			ev.AddEventHandler (null, exceptionManagerHook);
		}
		
		static void UnhookExceptionManager ()
		{
			if (exceptionManagerHook == null)
				return;
			
			Type t = typeof(GLib.Object).Assembly.GetType ("GLib.ExceptionManager");
			System.Reflection.EventInfo ev = t.GetEvent ("UnhandledException");
			ev.RemoveEventHandler (null, exceptionManagerHook);
			exceptionManagerHook = null;
		}
		
		static void OnUnhandledException (UnhandledExceptionEventArgs args)
		{
			LoggingService.LogError ("Unhandled exception in GLib event handler.", (Exception) args.ExceptionObject);
		}
	}
}
