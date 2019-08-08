using System;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl
{
	internal class RevertCommand
	{

		public static async Task<bool> RevertAsync (VersionControlItemList items, bool test, CancellationToken cancellationToken)
		{
			if (await RevertInternalAsync (items, test, cancellationToken)) {
				foreach (var itemPath in items.Paths)
					VersionControlService.SetCommitComment (itemPath, string.Empty, true);
				return true;
			}
			return false;
		}

		private static async Task<bool> RevertInternalAsync (VersionControlItemList items, bool test, CancellationToken cancellationToken)
		{
			try {
				if (test) {
					foreach (var item in items) {
						var info = await item.GetVersionInfoAsync (cancellationToken);
						if (!info.CanRevert)
							return false;
					}
					return true;
				}

				if (MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to revert the changes made in the selected files?"), 
				                                GettextCatalog.GetString ("All changes made to the selected files will be permanently lost."),
				                                AlertButton.Cancel, AlertButton.Revert) != AlertButton.Revert)
					return false;

				await new RevertWorker (items).StartAsync();
				return true;
			}
			catch (Exception ex) {
				if (test)
					LoggingService.LogError (ex.ToString ());
				else
					MessageService.ShowError (GettextCatalog.GetString ("Version control command failed."), ex);
				return false;
			}
		}

		private class RevertWorker : VersionControlTask {
			VersionControlItemList items;

			public RevertWorker (VersionControlItemList items) {
				this.items = items;
			}

			protected override string GetDescription() {
				return GettextCatalog.GetString ("Reverting ...");
			}

			protected override async Task RunAsync ()
			{
				foreach (VersionControlItemList list in items.SplitByRepository ()) {
					try {
						await list[0].Repository.RevertAsync (list.Paths, true, Monitor);
					} catch (Exception ex) {
						LoggingService.LogError ("Revert operation failed", ex);
						Monitor.ReportError (ex.Message, null);
						return;
					}
				}

				Gtk.Application.Invoke ((o, args) => {
					foreach (VersionControlItem item in items) {
						if (!item.IsDirectory) {
							// Reload reverted files
							Document doc = IdeApp.Workbench.GetDocument (item.Path);
							if (doc != null && System.IO.File.Exists (item.Path))
								doc.Reload ();
						}
					}
					VersionControlService.NotifyFileStatusChanged (items);
				});
				Monitor.ReportSuccess (GettextCatalog.GetString ("Revert operation completed."));
			}
		}

	}
}
