using System;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Commands
{
	internal class RevertCommand
	{

		public static bool Revert (List<VersionControlItem> items, bool test)
		{
			if (RevertInternal (items, test)) {
				foreach (var itemPath in items.GetPaths ())
					VersionControlService.SetCommitComment (itemPath, string.Empty, true);
				return true;
			}
			return false;
		}
		
		private static bool RevertInternal (List<VersionControlItem> items, bool test)
		{
			try {
				if (test)
					return items.All (i => i.VersionInfo.CanRevert);

				if (MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to revert the changes done in the selected files?"), 
				                                GettextCatalog.GetString ("All changes made to the selected files will be permanently lost."),
				                                AlertButton.Cancel, AlertButton.Revert) != AlertButton.Revert)
					return false;

				new RevertWorker (items).Start();
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

		private class RevertWorker : Task {
			List<VersionControlItem> items;
						
			public RevertWorker (List<VersionControlItem> items) {
				this.items = items;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Reverting ...");
			}
			
			protected override void Run ()
			{
				foreach (var list in items.SplitByRepository ())
					list.Key.Revert (list.Value.GetPaths (), true, Monitor);
				
				Monitor.ReportSuccess (GettextCatalog.GetString ("Revert operation completed."));
				Gtk.Application.Invoke (delegate {
					foreach (VersionControlItem item in items) {
						if (!item.IsDirectory) {
							FileService.NotifyFileChanged (item.Path);
							// Reload reverted files
							Document doc = IdeApp.Workbench.GetDocument (item.Path);
							if (doc != null && System.IO.File.Exists (item.Path))
								doc.Reload ();
						}
					}
					VersionControlService.NotifyFileStatusChanged (items);
				});
			}
		}
		
	}
}
