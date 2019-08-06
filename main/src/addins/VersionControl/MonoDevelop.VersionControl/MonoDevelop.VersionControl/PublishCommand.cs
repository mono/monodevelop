using System.Collections.Generic;
using System.Linq;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.Dialogs;
using MonoDevelop.Ide;
using MonoDevelop.Components.Commands;
using Xwt;

namespace MonoDevelop.VersionControl 
{
	internal class PublishCommand : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = info.Visible = false;
			if (!VersionControlService.IsGloballyDisabled) {
				var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
				if (solution == null)
					return;
				Repository repository = VersionControlService.GetRepository (solution);
				VersionInfo versionInfo = repository.GetVersionInfo (repository.RootPath);
				info.Enabled = info.Visible = Publish (versionInfo, solution, solution.BaseDirectory, true);
			}
		}

		protected override void Run ()
		{
			var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution == null)
				return;
			Repository repository = VersionControlService.GetRepository (solution);
			VersionInfo versionInfo = repository.GetVersionInfo (repository.RootPath);
			Publish (versionInfo, solution, solution.BaseDirectory, false);
		}

		public static bool Publish (VersionInfo versionInfo, WorkspaceObject entry, FilePath localPath, bool test)
		{
			if (test) {
				var canPublish = !versionInfo.CanUpdate;

				if (canPublish)
					return true;

				return VersionControlService.CheckVersionControlInstalled () && VersionControlService.GetRepository (entry) == null;
			}

			string moduleName = entry.Name;
			Repository repository = VersionControlService.GetRepository (entry);
			bool isInitialized = repository != null;
			List<FilePath> files = new List<FilePath> ();

			if (!isInitialized) {
				// Build the list of files to be checked in         
				if (localPath == entry.BaseDirectory) {
					GetFiles (files, entry);
				} else if (entry is Project) {
					foreach (ProjectFile file in ((Project)entry).Files.GetFilesInPath (localPath)) {
						if (file.Subtype != Subtype.Directory)
							files.Add (file.FilePath);
					}
				} else
					return false;

				if (files.Count == 0)
					return false;
			}

			bool hasChanges = false;
			if (repository != null) {
				var directoryVersionInfo = repository.GetDirectoryVersionInfo (localPath, true, false);
				if (directoryVersionInfo != null)
					hasChanges = directoryVersionInfo.Any (vi => vi.HasLocalChanges);

				if (hasChanges) {
					foreach (var vi in directoryVersionInfo) {
						if (vi.HasRemoteChanges)
							files.Add (vi.LocalPath);
					}
				}
			}

			if (!isInitialized) {
				SelectRepository (moduleName, localPath, files);
			} else {
				ValidateRepository (repository);
				ConfigureRepository (repository, moduleName, localPath, files, hasChanges);
			}
			return true;
		}

		static void SelectRepository (string moduleName, FilePath localPath, List<FilePath> files)
		{
			SelectRepositoryDialog dlg = new SelectRepositoryDialog (SelectRepositoryMode.Publish);

			try {
				dlg.ModuleName = moduleName;
				dlg.Message = GettextCatalog.GetString ("Initial check-in of module {0}", moduleName);

				do {
					if (MessageService.RunCustomDialog (dlg) == (int)Gtk.ResponseType.Ok && dlg.Repository != null) {
						AlertButton publishButton = new AlertButton (GettextCatalog.GetString ("_Publish"));
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
				dlg.Dispose ();
			}
		}

		static void ValidateRepository (Repository repository)
		{
			if (repository != null && repository is UrlBasedRepository urlBasedRepository) {
				if (string.IsNullOrEmpty (urlBasedRepository.Url)) {
					using var tempRepository = repository.VersionControlSystem.CreateRepositoryInstance ();
					if (tempRepository is UrlBasedRepository urlTempBasedRepository)
						urlBasedRepository.Url = urlTempBasedRepository.Url;
				}
			}
		}

		static void ConfigureRepository (Repository repository, string moduleName, FilePath localPath, List<FilePath> files, bool hasChanges)
		{
			using (ConfigureRepositoryDialog dlg = new ConfigureRepositoryDialog (repository, hasChanges)) {
				if (hasChanges) {
					dlg.Message = GettextCatalog.GetString ("Check-in of module {0}", moduleName);
				}
				dlg.ModuleName = moduleName;
				if (dlg.Run (MessageDialog.RootWindow) == Xwt.Command.Ok && dlg.Repository != null) {
					PublishWorker w = new PublishWorker (dlg.Repository, dlg.ModuleName, localPath, files.ToArray (), dlg.Message);
					w.Start ();
				}
			}
		}

		static void GetFiles (List<FilePath> files, WorkspaceObject entry)
		{
			// Ensure that we strip out all linked files from outside of the solution/projects path.
			if (entry is IWorkspaceFileObject)
				files.AddRange (((IWorkspaceFileObject)entry).GetItemFiles (true).Where (file => file.CanonicalPath.IsChildPathOf (entry.BaseDirectory)));
		}
		
		public static bool CanPublish (Repository vc, string path, bool isDir) {
			if (!VersionControlService.CheckVersionControlInstalled ())
				return false;

			if (!vc.GetVersionInfo (path).IsVersioned && isDir) 
				return true;
			return false;
		}

		class PublishWorker : VersionControlTask
		{
			Repository vc;
			FilePath path;
			string moduleName;
			FilePath [] files;
			string message;

			public PublishWorker (Repository vc, string moduleName, FilePath localPath, FilePath [] files, string message)
			{
				this.vc = vc;
				this.path = localPath;
				this.moduleName = moduleName;
				this.files = files;
				this.message = message;
				OperationType = VersionControlOperationType.Push;
			}

			protected override string GetDescription ()
			{
				return GettextCatalog.GetString ("Publishing \"{0}\" Project...", moduleName);
			}

			protected override void Run ()
			{
				try {
					vc.Publish (moduleName, path, files, message, Monitor);
				} catch (VersionControlException e) {
					LoggingService.LogError ("Publish operation failed", e);
					Monitor.ReportError (e.Message, null);
					return;
				}

				Gtk.Application.Invoke ((o, args) => {
					VersionControlService.NotifyFileStatusChanged (new FileUpdateEventArgs (vc, path, true));
				});
				Monitor.ReportSuccess (GettextCatalog.GetString ("Publish operation completed."));
			}
		}
	}
}
