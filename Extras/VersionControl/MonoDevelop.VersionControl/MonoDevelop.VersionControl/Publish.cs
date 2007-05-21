using System;
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui;

using MonoDevelop.VersionControl.Dialogs;

namespace MonoDevelop.VersionControl 
{
	public class PublishCommand 
	{
		public static bool Publish (CombineEntry entry, string localPath, bool test)
		{
			if (test)
				return true;

			ArrayList files = new ArrayList ();

			// Build the list of files to be checked in			
			string moduleName = entry.Name;
			if (localPath == entry.BaseDirectory) {
				GetFiles (files, entry);
			} else if (entry is Project) {
				foreach (ProjectFile file in ((Project)entry).ProjectFiles.GetFilesInPath (localPath))
					if (file.Subtype != Subtype.Directory)
						files.Add (file.FilePath);
			} else
				return false;
	
			SelectRepositoryDialog dlg = new SelectRepositoryDialog (SelectRepositoryMode.Publish);
			try {
				dlg.ModuleName = moduleName;
				dlg.Message = GettextCatalog.GetString ("Initial check-in of module {0}", moduleName);
				do {
					if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
						if (IdeApp.Services.MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to publish the project to the repository '{0}'?", dlg.Repository.Name))) {
							PublishWorker w = new PublishWorker (dlg.Repository, dlg.ModuleName, localPath, (string[]) files.ToArray (typeof(string)), dlg.Message);
							w.Start ();
							break;
						}
					} else
						break;
				} while (true);
			} finally {
				dlg.Destroy ();
			}
			return true;
		}
		
		static void GetFiles (ArrayList files, CombineEntry entry)
		{
			files.Add (entry.FileName);
			if (entry is Project) {
				foreach (ProjectFile file in ((Project)entry).ProjectFiles)
					if (file.Subtype != Subtype.Directory)
						files.Add (file.FilePath);
			} else if (entry is Combine) {
				foreach (CombineEntry e in ((Combine)entry).Entries)
					GetFiles (files, e);
			}
		}
		
		public static bool CanPublish (Repository vc, string path, bool isDir) {
			if (!vc.IsVersioned (path) && isDir) 
				return true;
			return false;
		}
	}
	
	internal class PublishWorker : Task {
		Repository vc;
		string path;
		string moduleName;
		string[] files;
		string message;
		Combine co;
					
		public PublishWorker (Repository vc, string moduleName, string localPath, string[] files, string message) 
		{
			this.co = IdeApp.ProjectOperations.CurrentOpenCombine;
			this.vc = vc;
			this.path = localPath;
			this.moduleName = moduleName;
			this.files = files;
			this.message = message;
		}

		protected override string GetDescription ()
		{
			return "Publishing \"" + co.Name + "\" Project...";
		}
		
		protected override void Run ()
		{
			vc.Publish (moduleName, path, files, message, GetProgressMonitor ());
		}
	}
}
