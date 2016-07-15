using System;
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
}

