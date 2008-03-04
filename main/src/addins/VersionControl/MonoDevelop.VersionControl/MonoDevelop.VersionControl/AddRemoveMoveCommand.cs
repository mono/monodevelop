using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;

namespace MonoDevelop.VersionControl 
{
	internal class AddCommand
	{
		public static bool Add (Repository vc, string path, bool test) {
			if (vc.CanAdd(path)) {
				if (test) return true;
				new AddWorker(vc, path).Start();
				return true;
			}
			return false;
		}
		
		public static bool CanAdd (Repository vc, string path) {
			if (vc.CanAdd(path)) 
				return true;
			else
				return false;
		}

		private class AddWorker : Task {
			Repository vc;
			string path;
						
			public AddWorker (Repository vc, string path) 
			{
				this.vc = vc;
				this.path = path;
			}
			
			protected override string GetDescription()
			{
				return GettextCatalog.GetString ("Adding {0}...", path);
			}
			
			protected override void Run ()
			{
				vc.Add (path, true, GetProgressMonitor ());
				VersionControlService.NotifyFileStatusChanged (vc, path, Directory.Exists (path));
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
		public static bool Remove (Repository vc, string path, bool isDir, bool test)
		{
			if (vc.CanRemove(path)) {
				if (test) return true;
				new RemoveWorker(vc, path, isDir).Start();
				return true;
			}
			return false;
		}
		
		public static bool CanRemove (Repository vc, string path) {
			if (vc.CanRemove(path)) 
				return true;
			else
				return false;
		}

		private class RemoveWorker : Task {
			Repository vc;
			string path;
						
			public RemoveWorker(Repository vc, string path, bool isDir) {
				this.vc = vc;
				this.path = path;
			}
			
			protected override string GetDescription() {
				return "Removing " + path + "...";
			}
			
			protected override void Run() {
				bool isDir = Directory.Exists (path);
				if (isDir)
					vc.DeleteDirectory (path, true, GetProgressMonitor ());
				else
					vc.DeleteFile (path, true, GetProgressMonitor ());
				VersionControlService.NotifyFileStatusChanged (vc, path, isDir);
			}
		}
		
	}
}