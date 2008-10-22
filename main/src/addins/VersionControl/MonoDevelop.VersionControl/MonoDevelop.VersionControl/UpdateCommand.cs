using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;

namespace MonoDevelop.VersionControl
{
	internal class UpdateCommand
	{
		public static bool Update (VersionControlItemList items, bool test)
		{
			foreach (VersionControlItem it in items)
				if (!it.Repository.CanUpdate (it.Path))
					return false;
			if (test)
				return true;
			
			new UpdateWorker (items).Start();
			return true;
		}

		private class UpdateWorker : Task {
			VersionControlItemList items;
						
			public UpdateWorker (VersionControlItemList items) {
				this.items = items;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Updating...");
			}
			
			protected override void Run ()
			{
				foreach (VersionControlItemList list in items.SplitByRepository ()) {
					list[0].Repository.Update (list.Paths, true, GetProgressMonitor ());
				}
				Gtk.Application.Invoke (delegate {
					foreach (VersionControlItem item in items)
						VersionControlService.NotifyFileStatusChanged (item.Repository, item.Path, item.IsDirectory);
				});
			}
		}
		
	}

}
