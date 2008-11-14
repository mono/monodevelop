using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl
{
	internal class RevertCommand
	{

		public static bool Revert (VersionControlItemList items, bool test)
		{
			if (RevertInternal (items, test)) {
				foreach (var itemPath in items.Paths)
					VersionControlService.SetCommitComment (itemPath, string.Empty, true);
				return true;
			}
			return false;
		}
		
		private static bool RevertInternal (VersionControlItemList items, bool test)
		{
			try {
				if (test) {
					foreach (VersionControlItem item in items)
						if (!item.Repository.CanRevert (item.Path))
							return false;
					return true;
				}

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
					MessageService.ShowException (ex, GettextCatalog.GetString ("Version control command failed."));
				return false;
			}
		}

		private class RevertWorker : Task {
			VersionControlItemList items;
						
			public RevertWorker (VersionControlItemList items) {
				this.items = items;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Reverting ...");
			}
			
			protected override void Run ()
			{
				foreach (VersionControlItemList list in items.SplitByRepository ())
					list[0].Repository.Revert (list.Paths, true, GetProgressMonitor ());
				
				Gtk.Application.Invoke (delegate {
					foreach (VersionControlItem item in items) {
						if (!item.IsDirectory) {
							// Reload reverted files
							Document doc = IdeApp.Workbench.GetDocument (item.Path);
							if (doc != null)
								doc.Reload ();
							FileService.NotifyFileChanged (item.Path);
						}
						VersionControlService.NotifyFileStatusChanged (item.Repository, item.Path, item.IsDirectory);
					}
				});
			}
		}
		
	}
}
