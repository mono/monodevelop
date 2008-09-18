using System;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

using MonoDevelop.VersionControl.Dialogs;

namespace MonoDevelop.VersionControl 
{
	internal class PublishCommand 
	{
		public static bool Publish (IWorkspaceObject entry, string localPath, bool test)
		{
			if (test)
				return true;

			if (!VersionControlService.CheckVersionControlInstalled ())
				return false;
			
			List<string> files = new List<string> ();

			// Build the list of files to be checked in			
			string moduleName = entry.Name;
			if (localPath == entry.BaseDirectory) {
				GetFiles (files, entry);
			} else if (entry is Project) {
				foreach (ProjectFile file in ((Project)entry).Files.GetFilesInPath (localPath))
					if (file.Subtype != Subtype.Directory)
						files.Add (file.FilePath);
			} else
				return false;

			if (files.Count == 0)
				return false;
	
			SelectRepositoryDialog dlg = new SelectRepositoryDialog (SelectRepositoryMode.Publish);
			try {
				dlg.ModuleName = moduleName;
				dlg.Message = GettextCatalog.GetString ("Initial check-in of module {0}", moduleName);
				do {
					if (dlg.Run () == (int) Gtk.ResponseType.Ok && dlg.Repository != null) {
						AlertButton publishButton = new AlertButton ("_Publish");					
						if (MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to publish the project?"), GettextCatalog.GetString ("The project will be published to the repository '{0}', module '{1}'.", dlg.Repository.Name, dlg.ModuleName), AlertButton.Cancel, publishButton) == publishButton) {
							PublishWorker w = new PublishWorker (dlg.Repository, dlg.ModuleName, localPath, files.ToArray (), dlg.Message);
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
		
		static void GetFiles (List<string> files, IWorkspaceObject entry)
		{
			if (entry is IWorkspaceFileObject)
				files.AddRange (((IWorkspaceFileObject)entry).GetItemFiles (true));
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
					
		public PublishWorker (Repository vc, string moduleName, string localPath, string[] files, string message) 
		{
			this.vc = vc;
			this.path = localPath;
			this.moduleName = moduleName;
			this.files = files;
			this.message = message;
		}

		protected override string GetDescription ()
		{
			return GettextCatalog.GetString ("Publishing \"{0}\" Project...", moduleName);
		}
		
		protected override void Run ()
		{
			vc.Publish (moduleName, path, files, message, GetProgressMonitor ());
			
			Gtk.Application.Invoke (delegate {
				VersionControlService.NotifyFileStatusChanged (vc, path, true);
			});
		}
	}
}
