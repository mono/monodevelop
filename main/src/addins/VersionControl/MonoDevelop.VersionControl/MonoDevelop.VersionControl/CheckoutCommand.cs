using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.Dialogs;
using MonoDevelop.Ide;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.VersionControl
{
	internal class CheckoutCommand : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = VersionControlService.CheckVersionControlInstalled ();
			info.Visible = !VersionControlService.IsGloballyDisabled;
		}

		protected override void Run()
		{
			SelectRepositoryDialog del = new SelectRepositoryDialog (SelectRepositoryMode.Checkout);
			try {
				if (MessageService.RunCustomDialog (del) == (int) Gtk.ResponseType.Ok && del.Repository != null) {
					CheckoutWorker w = new CheckoutWorker (del.Repository, del.TargetPath);
					w.StartAsync ();
				}
			} finally {
				del.Destroy ();
				del.Dispose ();
			}
		}

		class CheckoutWorker : VersionControlTask
		{
			Repository vc;
			string path;

			public CheckoutWorker (Repository vc, string path)
			{
				this.vc = vc;
				this.path = path;
				OperationType = VersionControlOperationType.Pull;
			}

			protected override string GetDescription ()
			{
				return GettextCatalog.GetString ("Checking out {0}...", path);
			}

			protected override ProgressMonitor CreateProgressMonitor ()
			{
				return new MonoDevelop.Core.ProgressMonitoring.AggregatedProgressMonitor (
					base.CreateProgressMonitor (),
					new MonoDevelop.Ide.ProgressMonitoring.MessageDialogProgressMonitor (true, true, true, true)
				);
			}

			AlertButton AskForCheckoutPath ()
			{
				return MessageService.AskQuestion (
					GettextCatalog.GetString ("Checkout path is not empty. Do you want to delete its contents?"),
					path,
					AlertButton.Cancel,
					AlertButton.Ok);
			}

			protected override async Task RunAsync ()
			{
				if (System.IO.Directory.Exists (path) && System.IO.Directory.EnumerateFileSystemEntries (path).Any ()) {
					var result = await Runtime.RunInMainThread (() => AskForCheckoutPath ());
					if (result == AlertButton.Cancel)
						return;

					FileService.DeleteDirectory (path);
					FileService.CreateDirectory (path);
				}

				await vc.CheckoutAsync (path, null, true, Monitor);

				if (Monitor.CancellationToken.IsCancellationRequested) {
					Monitor.ReportSuccess (GettextCatalog.GetString ("Checkout operation cancelled"));
					return;
				}

				if (!System.IO.Directory.Exists (path)) {
					Monitor.ReportError (GettextCatalog.GetString ("Checkout folder does not exist"), null);
					return;
				}

				foreach (string str in System.IO.Directory.EnumerateFiles (path, "*", System.IO.SearchOption.AllDirectories)) {
					if (MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile (str)) {
						await Runtime.RunInMainThread (delegate {
							IdeApp.Workspace.OpenWorkspaceItem (str);
						});
						break;
					}
				}

				Monitor.ReportSuccess (GettextCatalog.GetString ("Solution checked out"));
			}
		}
	}
}
