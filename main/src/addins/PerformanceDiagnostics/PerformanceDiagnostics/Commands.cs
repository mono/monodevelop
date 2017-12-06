using System;
using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;

namespace PerformanceDiagnosticsAddIn
{
	public class StartStopListeningUIThreadMonitorHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (UIThreadMonitor.Instance.IsListening)
				UIThreadMonitor.Instance.Stop ();
			else
				UIThreadMonitor.Instance.Start ();
		}

		protected override void Update (CommandInfo info)
		{
			info.Text = UIThreadMonitor.Instance.IsListening ? GettextCatalog.GetString ("Stop monitoring UIThread hangs") : GettextCatalog.GetString ("Start monitoring UIThread hangs");
			base.Update (info);
		}
	}

	public class ProfileFor5SecondsHandler : CommandHandler
	{
		protected override void Run ()
		{
			UIThreadMonitor.Instance.Profile (5);
		}
	}

	public class ToggleProfileHandler : CommandHandler
	{
		protected override void Update(CommandInfo info)
		{
			info.Checked = UIThreadMonitor.Instance.ToggleProfilingChecked;
			base.Update(info);
		}

		protected override void Run ()
		{
			UIThreadMonitor.Instance.ToggleProfiling ();
		}
	}

	public class InitializeGtkHelperHandler : CommandHandler
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

	public class DumpLiveWidgetsHandler : CommandHandler
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
}

