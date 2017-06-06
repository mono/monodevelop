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

	public class InitializeGtkHelperHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (!Options.HasMemoryLeakFeature)
				return;

			var field = typeof (GLib.SafeObjectHandle).GetField ("InternalCreateHandle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			field.SetValue (null, new Func<IntPtr, GLib.SafeObjectHandle> (arg => new LeakHelpers.LeakCheckSafeHandle (arg)));
		}
	}

	public class DumpLiveWidgetsHandler : CommandHandler
	{
		static System.IO.TextWriter log = LoggingService.CreateLogFile ("leak-dump");
		protected override void Update (CommandInfo info)
		{
			info.Visible = Options.HasMemoryLeakFeature;
			base.Update (info);
		}

		protected override void Run ()
		{
			GC.Collect ();
			GC.Collect ();
			GC.Collect ();
			GC.WaitForPendingFinalizers ();

			var (summary, delta) = LeakHelpers.GetSummary ();
			log.WriteLine ("Summary:");
			log.WriteLine (summary);
			log.WriteLine ();
			log.WriteLine ("Delta:");
			log.WriteLine (delta);
			log.WriteLine ("=========================================================================================");
		}
	}
}

