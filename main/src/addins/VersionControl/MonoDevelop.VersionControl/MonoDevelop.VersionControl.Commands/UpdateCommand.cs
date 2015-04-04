using System.Linq;
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Commands
{
	internal class UpdateCommand
	{
		public static bool Update (List<VersionControlItem> items, bool test)
		{
			if (!items.All (i => i.VersionInfo.CanUpdate))
				return false;
			if (test)
				return true;
			
			new UpdateWorker (items).Start();
			return true;
		}

		private class UpdateWorker : Task {
			List<VersionControlItem> items;
						
			public UpdateWorker (List<VersionControlItem> items) {
				this.items = items;
				OperationType = VersionControlOperationType.Pull;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Updating...");
			}
			
			protected override void Run ()
			{
				foreach (List<VersionControlItem> list in items.SplitByRepository ()) {
					list[0].Repository.Update (list.GetPaths (), true, Monitor);
				}
				Monitor.ReportSuccess (GettextCatalog.GetString ("Update operation completed."));
				Gtk.Application.Invoke (delegate {
					VersionControlService.NotifyFileStatusChanged (items);
				});
			}
		}
		
	}

}
