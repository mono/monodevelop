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
				IProgressMonitor monitor = GetProgressMonitor ();
				
				foreach (VersionControlItemList list in items.SplitByRepository ())
					list[0].Repository.Add (list.Paths, true, monitor);
				
				Gtk.Application.Invoke (delegate {
					foreach (VersionControlItem item in items)
						VersionControlService.NotifyFileStatusChanged (item.Repository, item.Path, item.IsDirectory);
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
						files[0].Repository.DeleteFiles (files.Paths, true, GetProgressMonitor ());
					VersionControlItemList dirs = list.GetDirectories ();
					if (dirs.Count > 0)
						dirs[0].Repository.DeleteDirectories (dirs.Paths, true, GetProgressMonitor ());
				}
				
				Gtk.Application.Invoke (delegate {
					foreach (VersionControlItem item in items)
						VersionControlService.NotifyFileStatusChanged (item.Repository, item.Path, item.IsDirectory);
				});
			}
		}
		
	}
}