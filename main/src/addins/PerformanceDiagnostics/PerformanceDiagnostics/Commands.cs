using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Dialogs;

namespace PerformanceDiagnosticsAddIn
{
	class StartStopListeningUIThreadMonitorHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (UIThreadMonitor.Instance.IsSampling)
				UIThreadMonitor.Instance.Start (sample: false);
			else
				UIThreadMonitor.Instance.Start (sample: true);
		}

		protected override void Update (CommandInfo info)
		{
			info.Text = UIThreadMonitor.Instance.IsSampling ? GettextCatalog.GetString ("Stop monitoring UIThread hangs") : GettextCatalog.GetString ("Start monitoring UIThread hangs");
			base.Update (info);
		}
	}

	class ProfileFor5SecondsHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.DisableOnShellLock = false;
			base.Update (info);
		}

		protected override void Run ()
		{
			UIThreadMonitor.Instance.Profile (spinDump: false, 5);
		}
	}

	class SpinDumpFor5SecondsHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.DisableOnShellLock = false;
			base.Update (info);
		}

		protected override void Run ()
		{
			UIThreadMonitor.Instance.Profile (spinDump: true, 5);
		}
	}

	class EnhanceSampleFile : CommandHandler
	{
		protected override void Run ()
		{
			var dialog = new OpenFileDialog (GettextCatalog.GetString ("Sample file output"), MonoDevelop.Components.FileChooserAction.Open);
			if (dialog.Run ())
				MonoDevelop.Utilities.SampleProfiler.ConvertJITAddressesToMethodNames (Options.OutputPath, dialog.SelectedFile, "External");
		}
	}

	class ToggleProfileHandler : CommandHandler
	{
		protected override void Update(CommandInfo info)
		{
			info.Checked = UIThreadMonitor.Instance.ToggleProfilingChecked;
			info.DisableOnShellLock = false;
			base.Update(info);
		}

		protected override void Run ()
		{
			UIThreadMonitor.Instance.ToggleProfiling (spinDump: false);
		}
	}

	class InitializeGtkHelperHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (!Options.HasMemoryLeakFeature)
				return;

			var type = typeof (GLib.Object).Assembly.GetType ("GLib.PointerWrapper");
			if (type == null) {
				return;
			}

			LoggingService.LogInfo ("Gtk/Mac leak tracking enabled");

			if (Options.HasMemoryLeakFeaturePad)
				LoggingService.LogInfo ("Gtk/Mac leak tracking pad enabled. May cause performance issues.");

			var field = type.GetField ("ObjectCreated", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			field.SetValue (null, new Action<IntPtr> (arg => {
				lock (LeakHelpers.GObjectDict)
					LeakHelpers.GObjectDict.Add (arg);
			}));

			field = type.GetField ("ObjectDestroyed", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			field.SetValue (null, new Action<IntPtr> (arg => {
				lock (LeakHelpers.GObjectDict)
					LeakHelpers.GObjectDict.Remove (arg);
			}));
		}
	}

	class DumpLiveWidgetsHandler : CommandHandler
	{
		static readonly System.IO.TextWriter log = LoggingService.CreateLogFile ("leak-dump");
		protected override void Update (CommandInfo info)
		{
			info.Visible = Options.HasMemoryLeakFeature;
			base.Update (info);
		}

		protected override async void Run ()
		{
			await LeakSummaryService.Dump (log);
			log.WriteLine ("=========================================================================================");
		}
	}

#if DEBUG
	class InduceManagedCrashHandler : CommandHandler
	{
		protected override void Run ()
		{
			Task.Run (() => {
				new NSObject ().BeginInvokeOnMainThread (() => throw new Exception ("Diagnostics: Managed crash"));
			});
		}
	}

	class InduceNativeCrashHandler : CommandHandler
	{
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		static extern void void_objc_msgSend (IntPtr receiver, IntPtr selector);

		protected override void Run ()
		{
			Task.Run (() => {
				using var x = new NSException ("Native crash", "Diagnostics", null);
				var selector = ObjCRuntime.Selector.GetHandle ("raise");

				void_objc_msgSend (x.Handle, selector);
			});
		}
	}

	class InduceHangHandler : CommandHandler
	{
		protected override void Run ()
		{
			Thread.Sleep (TimeSpan.FromMinutes(2));
		}
	}
#endif
}
