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
using System.Runtime.InteropServices;
using System.Collections;

namespace MonoDevelop.Ide.Gui
{
	static class GLibLogging
	{
		#region Begin Workaround

		public delegate void LogFunc (IntPtr log_domain, LogLevelFlags log_level, IntPtr message);

	[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
		internal delegate void LogFunc2 (IntPtr log_domain, LogLevelFlags log_level, IntPtr message, LogFunc user_data);

	[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
	public delegate void PrintFunc (string message);

	[Flags]
	public enum LogLevelFlags : int
	{
		/* log flags */
		FlagRecursion          = 1 << 0,
		FlagFatal              = 1 << 1,

		/* GLib log levels */
		Error                  = 1 << 2,       /* always fatal */
		Critical               = 1 << 3,
		Warning                = 1 << 4,
		Message                = 1 << 5,
		Info                   = 1 << 6,
		Debug                  = 1 << 7,

		/* Convenience values */
		AllButFatal            = 253,
		AllButRecursion        = 254,
		All                    = 255,

		FlagMask               = 3,
		LevelMask              = unchecked ((int) 0xFFFFFFFC)
	}

	public class Log
	{
		const string LIBGLIB = "libglib-2.0-0.dll";
		static Hashtable handlers;

		static void EnsureHash ()
		{
			if (handlers == null)
				handlers = new Hashtable ();
		}

		[DllImport (LIBGLIB, CallingConvention = CallingConvention.Cdecl)]
		static extern void g_logv (IntPtr log_domain, LogLevelFlags flags, IntPtr message);

		public void WriteLog (string logDomain, LogLevelFlags flags, string format, params object[] args)
		{
			IntPtr ndom = GLib.Marshaller.StringToPtrGStrdup (logDomain);
			IntPtr nmessage = GLib.Marshaller.StringToPtrGStrdup (String.Format (format, args));
			g_logv (ndom, flags, nmessage);
			GLib.Marshaller.Free (ndom);
			GLib.Marshaller.Free (nmessage);
		}

		[DllImport (LIBGLIB, CallingConvention = CallingConvention.Cdecl)]
		static extern uint g_log_set_handler (IntPtr log_domain, LogLevelFlags flags, LogFunc2 log_func, LogFunc user_data);

			static readonly LogFunc2 LogFuncTrampoline = (IntPtr domain, LogLevelFlags level, IntPtr message, LogFunc user_data) => {
			user_data (domain, level, message);
		};

			public static uint SetLogHandler (IntPtr logDomain, LogLevelFlags flags, LogFunc logFunc)
		{
				uint result = g_log_set_handler (logDomain, flags, LogFuncTrampoline, logFunc);
			EnsureHash ();
			handlers[result] = logFunc;

			return result;
		}

		[DllImport (LIBGLIB, CallingConvention = CallingConvention.Cdecl)]
		static extern uint g_log_remove_handler (IntPtr log_domain, uint handler_id);

		public static void RemoveLogHandler (string logDomain, uint handlerID)
		{
			if (handlers != null && handlers.ContainsKey (handlerID))
				handlers.Remove (handlerID);

			IntPtr ndom = GLib.Marshaller.StringToPtrGStrdup (logDomain);
			g_log_remove_handler (ndom, handlerID);
			GLib.Marshaller.Free (ndom);
		}


		[DllImport (LIBGLIB, CallingConvention = CallingConvention.Cdecl)]
		static extern PrintFunc g_set_print_handler (PrintFunc handler);

		public static PrintFunc SetPrintHandler (PrintFunc handler)
		{
			EnsureHash ();
			handlers["PrintHandler"] = handler;

			return g_set_print_handler (handler);
		}

		[DllImport (LIBGLIB, CallingConvention = CallingConvention.Cdecl)]
		static extern PrintFunc g_set_printerr_handler (PrintFunc handler);

		public static PrintFunc SetPrintErrorHandler (PrintFunc handler)
		{
			EnsureHash ();
			handlers["PrintErrorHandler"] = handler;

			return g_set_printerr_handler (handler);
		}

		[DllImport (LIBGLIB, CallingConvention = CallingConvention.Cdecl)]
		static extern void g_log_default_handler (IntPtr log_domain, LogLevelFlags log_level, IntPtr message, IntPtr unused_data);

		public static void DefaultHandler (string logDomain, LogLevelFlags logLevel, string message)
		{
			IntPtr ndom = GLib.Marshaller.StringToPtrGStrdup (logDomain);
			IntPtr nmess = GLib.Marshaller.StringToPtrGStrdup (message);
			g_log_default_handler (ndom, logLevel, nmess, IntPtr.Zero);
			GLib.Marshaller.Free (ndom);
			GLib.Marshaller.Free (nmess);
		}

		[DllImport (LIBGLIB, CallingConvention = CallingConvention.Cdecl)]
		extern static LogLevelFlags g_log_set_always_fatal (LogLevelFlags fatal_mask);

		public static LogLevelFlags SetAlwaysFatal (LogLevelFlags fatalMask)
		{
			return g_log_set_always_fatal (fatalMask);
		}

		[DllImport (LIBGLIB, CallingConvention = CallingConvention.Cdecl)]
		extern static LogLevelFlags g_log_set_fatal_mask (IntPtr log_domain, LogLevelFlags fatal_mask);

		public static LogLevelFlags SetAlwaysFatal (string logDomain, LogLevelFlags fatalMask)
		{
			IntPtr ndom = GLib.Marshaller.StringToPtrGStrdup (logDomain);
			LogLevelFlags result = g_log_set_fatal_mask (ndom, fatalMask);
			GLib.Marshaller.Free (ndom);
			return result;
		}

		/*
		 * Some common logging methods.
		 *
		 * Sample usage:
		 *
		 *	// Print the messages for the NULL domain
		 *	LogFunc logFunc = new LogFunc (Log.PrintLogFunction);
		 *	Log.SetLogHandler (null, LogLevelFlags.All, logFunc);
		 *
		 *	// Print messages and stack trace for Gtk critical messages
		 *	logFunc = new LogFunc (Log.PrintTraceLogFunction);
		 *	Log.SetLogHandler ("Gtk", LogLevelFlags.Critical, logFunc);
		 *
		 */

		public static void PrintLogFunction (string domain, LogLevelFlags level, string message)
		{
			Console.WriteLine ("Domain: '{0}' Level: {1}", domain, level);
			Console.WriteLine ("Message: {0}", message);
		}

		public static void PrintTraceLogFunction (string domain, LogLevelFlags level, string message)
		{
			PrintLogFunction (domain, level, message);
			Console.WriteLine ("Trace follows:\n{0}", new System.Diagnostics.StackTrace ());
		}
	}
		#endregion End woraround

		// If we get more than 1MB of debug info, we don't care. 99.999% of the time we're just doing
		// the same thing over and over and we really don't want log files which are 100gb in size
		static int RemainingBytes = 1 * 1024 * 1024;
		static readonly string[] domains = new string[] {"Gtk", "Gdk", "GLib", "GLib-GObject", "Pango", "GdkPixbuf" };
		static uint[] handles;
		
		public static bool Enabled
		{
			get { return handles != null; }
			set {
				if ((handles != null) == value)
					return;
				
				if (value) {
					handles = new uint[domains.Length];
					for (int i = 0; i < domains.Length; i++) {
						IntPtr domain = GLib.Marshaller.StringToPtrGStrdup (domains [i]);
						handles [i] = GLibLogging.Log.SetLogHandler (domain, GLibLogging.LogLevelFlags.All, LoggerMethod);
						GLib.Marshaller.Free (domain);
					}
				} else {
					for (int i = 0; i < domains.Length; i++)
						GLib.Log.RemoveLogHandler (domains[i], handles[i]);
					handles = null;
				}
			}
		}
		
		static void LoggerMethod (IntPtr logDomainPtr, LogLevelFlags logLevel, IntPtr messagePtr)
		{
			if (RemainingBytes < 0)
				return;

			string logDomain = GLib.Marshaller.Utf8PtrToString (logDomainPtr);
			string message, extra = string.Empty;
			try {
				// Marshal message manually, because the text can contain invalid UTF-8.
				// Specifically, with zh_CN, pango fails to render some characters and
				// pango's error message contains the broken UTF-8, thus on marshalling
				// we need to catch the exception, otherwise we end up in a recursive
				// glib exception handling.
				message = GLib.Marshaller.Utf8PtrToString (messagePtr);
			} catch (Exception e) {
				message = "Failed to convert message";
				extra = "\n" + e.ToString ();
			}
			System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace (2, true);
			string msg = string.Format ("{0}-{1}: {2}\nStack trace: \n{3}{4}", 
			    logDomain, logLevel, message, trace.ToString (), extra);

			switch (logLevel) {
			case LogLevelFlags.Debug:
				LoggingService.LogDebug (msg);
				break;
			case LogLevelFlags.Info:
				LoggingService.LogInfo (msg);
				break;
			case LogLevelFlags.Warning:
				LoggingService.LogWarning (msg);
				break;
			case LogLevelFlags.Error:
			case LogLevelFlags.Critical:
			default:
				LoggingService.LogError (msg);
				break;
			}
			
			RemainingBytes -= msg.Length;
			if (RemainingBytes < 0)
				LoggingService.LogError ("Disabling glib logging for the rest of the session");
		}
	}
}
