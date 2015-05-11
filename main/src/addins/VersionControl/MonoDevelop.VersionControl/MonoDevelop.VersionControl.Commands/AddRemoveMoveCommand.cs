using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Commands 
{
	internal class AddCommand
	{
		public static bool Add (List<VersionControlItem> items, bool test)
		{
			if (!items.All (i => i.VersionInfo.CanAdd))
				return false;
			if (test)
				return true;
			
			new AddWorker (items).Start();
			return true;
		}
		
		private class AddWorker : Task {
			List<VersionControlItem> items;
						
			public AddWorker (List<VersionControlItem> items)
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
				
				foreach (var list in items.SplitByRepository ())
					list.Key.Add (list.Value.GetPaths (), true, Monitor);
				
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
		public static bool Remove (List<VersionControlItem> items, bool test)
		{
			if (!items.All (i => i.VersionInfo.CanRemove))
				return false;
			if (test)
				return true;
			
			string msg = GettextCatalog.GetString ("Are you sure you want to remove the selected items from the version control system?");
			string msg2 = GettextCatalog.GetString ("The files will be kept on disk.");
			if (MessageService.Confirm (msg, msg2, AlertButton.Remove))
				new RemoveWorker (items).Start();
			return true;
		}
		
		private class RemoveWorker : Task {
			List<VersionControlItem> items;
						
			public RemoveWorker (List<VersionControlItem> items) {
				this.items = items;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Removing...");
			}
			
			protected override void Run()
			{
				foreach (var list in items.SplitByRepository ()) {
					List<VersionControlItem> files = list.Value.GetFiles ();
					if (files.Count > 0)
						list.Key.DeleteFiles (files.GetPaths (), true, Monitor);
					
					List<VersionControlItem> dirs = list.Value.GetDirectories ();
					if (dirs.Count > 0)
						list.Key.DeleteDirectories (dirs.GetPaths (), true, Monitor);
				}
				
				Gtk.Application.Invoke (delegate {
					VersionControlService.NotifyFileStatusChanged (items);
				});
			}
		}
		
	}
}