using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	internal class UpdateCommand
	{
		public static bool Update (VersionControlItemList items, bool test)
		{
			if (!items.All (i => i.VersionInfo.CanUpdate))
				return false;
			if (test)
				return true;
			
			new UpdateWorker (items).Start();
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
			
			protected override void Run ()
			{
				foreach (VersionControlItemList list in items.SplitByRepository ()) {
					list[0].Repository.Update (list.Paths, true, Monitor);
				}
				Gtk.Application.Invoke ((o, args) => {
					VersionControlService.NotifyFileStatusChanged (items);
				});
				Monitor.ReportSuccess (GettextCatalog.GetString ("Update operation completed."));
			}
		}
		
	}

}
