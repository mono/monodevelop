using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
 
using MonoDevelop.Components;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl 
{
	internal class AddCommand
	{
		public static bool Add (VersionControlItemList items, bool test)
		{
			foreach (VersionControlItem it in items)
				if (!it.Repository.CanAdd (it.Path))
					return false;
			if (test)
				return true;
			
			new AddWorker (items).Start();
			return true;
		}
		
		public static bool CanAdd (Repository vc, string path) {
			if (vc.CanAdd(path)) 
				return true;
			else
				return false;
		}

		private class AddWorker : Task {
			VersionControlItemList items;
						
			public AddWorker (VersionControlItemList items) 
			{
				this.items = items;
			}
			
			protected override string GetDescription()
			{
				return GettextCatalog.GetString ("Adding...");
			}
			
			protected override void Run ()
			{
				IProgressMonitor monitor = Monitor;
				
				foreach (VersionControlItemList list in items.SplitByRepository ())
					list[0].Repository.Add (list.Paths, true, monitor);
				
				Gtk.Application.Invoke (delegate {
					VersionControlService.NotifyFileStatusChanged (items);
				});
			}
		}
		
	}
	
//	public class MoveCommand {
//		public static bool Move(string path, bool test) {
//			foreach (VersionControlSystem vc in VersionControlService.Providers) {
//				if (vc.CanMove(path)) {
//					if (test) return true;
//					new MoveWorker(vc, path).Start();
//					return true;
//				}
//			}
//			return false;
//		}
//		
//		public static bool CanMove(string path) {
//			foreach (VersionControlSystem vc in VersionControlService.Providers) {
//				if (vc.CanMove(path)) 
//					return true;
//			}
//			return false;
//		}
//
//		private class MoveWorker : Task {
//			VersionControlSystem vc;
//			string path;
//						
//			public MoveWorker(VersionControlSystem vc, string path) {
//				this.vc = vc;
//				this.path = path;
//			}
//			
//			protected override string GetDescription() {
//				return "Moving " + path + "...";
//			}
//			
//			protected override void Run() {
//				vc.Delete(path, true, new UpdateCallback(Callback));
//			}
//		
//			protected override void Finished() {
//			}
//			
//			private void Callback(string path, string action) {
//				Log(action + "\t" + path);
//			}
//		}
//		
//	}
	
	internal class RemoveCommand
	{
		public static bool Remove (VersionControlItemList items, bool test)
		{
			foreach (VersionControlItem it in items)
				if (!it.Repository.CanRemove (it.Path))
					return false;
			if (test)
				return true;
			
			string msg = GettextCatalog.GetString ("Are you sure you want to remove the selected items from the version control system?");
			string msg2 = GettextCatalog.GetString ("The files will be removed from disk");
			if (MessageService.Confirm (msg, msg2, AlertButton.Delete))
				new RemoveWorker (items).Start();
			return true;
		}
		
		public static bool CanRemove (Repository vc, string path) {
			if (vc.CanRemove(path)) 
				return true;
			else
				return false;
		}

		private class RemoveWorker : Task {
			VersionControlItemList items;
						
			public RemoveWorker (VersionControlItemList items) {
				this.items = items;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Removing...");
			}
			
			protected override void Run()
			{
				foreach (VersionControlItemList list in items.SplitByRepository ()) {
					VersionControlItemList files = list.GetFiles ();
					if (files.Count > 0)
						files[0].Repository.DeleteFiles (files.Paths, true, Monitor);
					VersionControlItemList dirs = list.GetDirectories ();
					if (dirs.Count > 0)
						dirs[0].Repository.DeleteDirectories (dirs.Paths, true, Monitor);
				}
				
				Gtk.Application.Invoke (delegate {
					VersionControlService.NotifyFileStatusChanged (items);
				});
			}
		}
		
	}
}