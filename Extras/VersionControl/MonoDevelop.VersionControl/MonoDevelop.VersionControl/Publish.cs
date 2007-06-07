using System;
using System.Collections;

using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Item;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui;

using MonoDevelop.VersionControl.Dialogs;

namespace MonoDevelop.VersionControl 
{
	public class PublishCommand 
	{
		public static bool Publish (IProject entry, string localPath, bool test)
		{
			if (test)
				return true;

			ArrayList files = new ArrayList ();

			// Build the list of files to be checked in			
			string moduleName = entry.Name;
			if (localPath == entry.BasePath) {
				GetFiles (files, entry);
			} else  {
				foreach (ProjectItem item in entry.Items) {
					ProjectFile file = item as ProjectFile;
					if (file == null)
						continue;
					if (file.FullPath.StartsWith (localPath) && file.FileType != FileType.Folder) 
						files.Add (file.FullPath);
				}
			}
			
			using (SelectRepositoryDialog dlg = new SelectRepositoryDialog (SelectRepositoryMode.Publish)) {
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
			}
			return true;
		}
		
		static void GetFiles (ArrayList files, IProject entry)
		{
			files.Add (entry.FileName);
			foreach (ProjectItem item in entry.Items) {
				ProjectFile file = item as ProjectFile;
				if (file == null)
					continue;
				if (file.FileType != FileType.Folder)
					files.Add (file.FullPath);
			}
		}
		static void GetFiles (ArrayList files, Solution entry)
		{
			files.Add (ProjectService.SolutionFileName); 
			foreach (IProject e in entry.AllProjects)
				GetFiles (files, e);
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
		Solution co;
					
		public PublishWorker (Repository vc, string moduleName, string localPath, string[] files, string message) 
		{
			this.co = ProjectService.Solution;
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
