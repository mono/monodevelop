using System;
using System.IO;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Monitoring;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Gtk;
using MonoDevelop.Ide;
using MonoDevelop.CrashReporting;

namespace MonoDevelop {

	class MainClass {
		
		public static void Main (string[] args)
		{
			int pid;
			string logDirectory;
			
			ParseOptions (args, out pid, out logDirectory);
			
			Console.WriteLine ("Monitoring PID {0} with log path: {1}", pid, logDirectory);
			if (pid == -1) {
				Console.WriteLine ("The pid of the MonoDevelop process being monitored must be supplied");
			} else if (string.IsNullOrEmpty (logDirectory)) {
				Console.WriteLine ("The path to write log files to must be supplied");
			} else {
				new MainClass (pid, logDirectory);
			}
		}
		
		static void ParseOptions (string[] args, out int pid, out string logPath)
		{
			pid = -1;
			logPath = null;
			
			for (int i = 0; i < args.Length - 1; i += 2) {
				if (args [i] == "-p") {
					if (!int.TryParse (args [i + 1], out pid)) {
						pid = -1;
					}
				}
				
				if (args [i] == "-l") {
					logPath = args [i + 1];
				}
			}
		}
		
		ICrashMonitor Monitor {
			get; set;
		}
		
		bool ProcessingCrash {
			get; set;
		}
		
		CrashReporter Reporter {
			get; set;
		}
		
		bool MonitoredProcessExited {
			get; set;
		}
		
		public MainClass (int pid, string logDirectory)
		{
			Monitor = CrashMonitor.Create (pid);
			Reporter = new CrashReporter (logDirectory);
			
			Monitor.ApplicationExited += (sender, e) => {
				// Wait a few seconds to see if there has been a crash. If not,
				// lets just gracefully exit.
				Thread.Sleep (5000);
				Application.Invoke (delegate {
					MonitoredProcessExited = true;
					
					// If monodevelop crashed, do not shut down until we've finished
					// processing the crash log.
					if (!ProcessingCrash)
						Environment.Exit (0);
				});
			};
			
			Monitor.CrashDetected += (sender, e) => {
				ShowCrashUI (e.CrashLogPath);
			};
			
			Monitor.Start ();
			
			Gtk.Application.Init ();
			Gtk.Application.Run ();
		}

		void ShowCrashUI (string crashLogPath)
		{
			Gtk.Application.Invoke (delegate {
				try {
					ProcessingCrash = true;
					var dialog = new Gtk.MessageDialog (null, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "MonoDevelop has experienced an unexpected error. Details of this error will be uploaded to Xamarin for diagnostic purposes.") {
						WindowPosition = WindowPosition.Center
					};
					
					var result = dialog.Run ();
					dialog.Destroy ();
					if (result == (int) ResponseType.Ok) {
						Reporter.UploadOrCache (crashLogPath);
					} else {
						Console.WriteLine ("Not uploading");
					}
				} finally {
					ProcessingCrash = false;
				}
				
				// If MD has gone away and we've processed the crash, we may as well exit.
				if (MonitoredProcessExited)
					Environment.Exit (0);
			});
		}
	}
}
