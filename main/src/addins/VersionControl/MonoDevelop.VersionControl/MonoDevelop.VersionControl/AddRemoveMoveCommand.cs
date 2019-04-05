using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl 
{
	internal class AddCommand
	{
		public static async Task<bool> AddAsync (VersionControlItemList items, bool test, CancellationToken cancellationToken)
		{
			foreach (var item in items) {
				var info = await item.GetVersionInfoAsync (cancellationToken);
				if (!info.CanAdd)
					return false;
			}
			if (test)
				return true;
			await new AddWorker (items).StartAsync (cancellationToken).ConfigureAwait (false);
			return true;
		}
		
		private class AddWorker : VersionControlTask {
			VersionControlItemList items;
						
			public AddWorker (VersionControlItemList items) 
			{
				this.items = items;
			}
			
			protected override string GetDescription()
			{
				return GettextCatalog.GetString ("Adding...");
			}
			
			protected override async Task RunAsync ()
			{
				ProgressMonitor monitor = Monitor;

				foreach (VersionControlItemList list in items.SplitByRepository ())
					await list[0].Repository.AddAsync (list.Paths, true, monitor);
				
				Gtk.Application.Invoke ((o, args) => {
					VersionControlService.NotifyFileStatusChanged (items);
				});
				monitor.ReportSuccess (GettextCatalog.GetString ("Add operation completed."));
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
		public static async Task<bool> RemoveAsync (VersionControlItemList items, bool test, CancellationToken cancellationToken)
		{
			foreach (var item in items) {
				var info = await item.GetVersionInfoAsync (cancellationToken);
				if (!info.CanRemove)
					return false;
			}
			if (test)
				return true;
			
			string msg = GettextCatalog.GetString ("Are you sure you want to remove the selected items from the version control system?");
			string msg2 = GettextCatalog.GetString ("The files will be kept on disk.");
			if (MessageService.Confirm (msg, msg2, AlertButton.Remove))
				await new RemoveWorker (items).StartAsync(cancellationToken);
			return true;
		}
		
		private class RemoveWorker : VersionControlTask {
			VersionControlItemList items;
						
			public RemoveWorker (VersionControlItemList items) {
				this.items = items;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Removing...");
			}
			
			protected override async Task RunAsync ()
			{
				foreach (VersionControlItemList list in items.SplitByRepository ()) {
					VersionControlItemList files = list.GetFiles ();
					if (files.Count > 0)
						await files[0].Repository.DeleteFilesAsync (files.Paths, true, Monitor, true).ConfigureAwait (false);
					VersionControlItemList dirs = list.GetDirectories ();
					if (dirs.Count > 0)
						await dirs[0].Repository.DeleteDirectoriesAsync (dirs.Paths, true, Monitor, true).ConfigureAwait (false);
				}
				
				Gtk.Application.Invoke ((o, args) => {
					VersionControlService.NotifyFileStatusChanged (items);
				});
				Monitor.ReportSuccess (GettextCatalog.GetString ("Remove operation completed."));
			}
		}
		
	}
}