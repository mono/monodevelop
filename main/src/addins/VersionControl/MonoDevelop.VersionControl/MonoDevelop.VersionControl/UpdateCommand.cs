using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	internal class UpdateCommand
	{
		public static async Task<bool> UpdateAsync (VersionControlItemList items, bool test, CancellationToken cancellationToken = default)
		{
			foreach (var item in items) {
				var info = await item.GetVersionInfoAsync (cancellationToken);
				if (!info.CanUpdate)
					return false;
			}
			if (test)
				return true;

			await new UpdateWorker (items).StartAsync (cancellationToken);
			return true;
		}

		private class UpdateWorker : VersionControlTask {
			VersionControlItemList items;

			public UpdateWorker (VersionControlItemList items) {
				this.items = items;
				OperationType = VersionControlOperationType.Pull;
			}

			protected override string GetDescription() {
				return GettextCatalog.GetString ("Updating...");
			}


			protected override async Task RunAsync ()
			{
				foreach (VersionControlItemList list in items.SplitByRepository ()) {
					try {
						await list[0].Repository.UpdateAsync (list.Paths, true, Monitor);
					} catch (Exception ex) {
						LoggingService.LogError ("Update operation failed", ex);
						Monitor.ReportError (ex.Message, null);
						return;
					}
				}
				Gtk.Application.Invoke ((o, args) => {
					VersionControlService.NotifyFileStatusChanged (items);
				});
				Monitor.ReportSuccess (GettextCatalog.GetString ("Update operation completed."));
			}
		}

	}

}
