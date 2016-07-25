using System;
using MonoDevelop.Components.Commands;

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
			info.Text = UIThreadMonitor.Instance.IsListening ? "Stop monitoring UIThread hangs" : "Start monitoring UIThread hangs";
			base.Update (info);
		}
	}
}

