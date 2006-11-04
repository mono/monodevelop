using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;

using VersionControl.Service;
using VersionControl.AddIn.Views;


namespace VersionControl.AddIn
{
	public class VersionControlNodeExtension : NodeBuilderExtension
	{
		Hashtable projectsWatched = new Hashtable();
		Hashtable fileStatus = new Hashtable();
	
		public override bool CanBuildNode (Type dataType)
		{
			//Console.Error.WriteLine(dataType);
			return typeof(ProjectFile).IsAssignableFrom (dataType)
				|| typeof(SystemFile).IsAssignableFrom (dataType)
				|| typeof(Project).IsAssignableFrom (dataType)
				|| typeof(ProjectFolder).IsAssignableFrom (dataType)
				|| typeof(Combine).IsAssignableFrom (dataType);
		}		
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			// Add status overlays
			
			if (dataObject is CombineEntry) {
				CombineEntry ce = (CombineEntry) dataObject;
				Repository rep = VersionControlProjectService.GetRepository (ce, ce.FileName);
				if (rep != null) {
					AddOverlay (ref icon, VersionControlProjectService.overlay_controled);
				}
				return;
			} else if (dataObject is ProjectFolder) {
				ProjectFolder ce = (ProjectFolder) dataObject;
				Repository rep = VersionControlProjectService.GetRepository (ce.Project, ce.Path);
				if (rep != null) {
					AddOverlay (ref icon, VersionControlProjectService.overlay_controled);
					AddOverlay (ref closedIcon, VersionControlProjectService.overlay_controled);
				}
				return;
			}
			
			if (!builder.Options["ShowVersionControlOverlays"])
				return;
		
			Project prj;
			string file;
			
			if (dataObject is ProjectFile) {
				ProjectFile pfile = (ProjectFile) dataObject;
				prj = pfile.Project;
				file = pfile.FilePath;
			} else {
				SystemFile pfile = (SystemFile) dataObject;
				prj = pfile.Project;
				file = pfile.Path;
			}
			
			if (prj == null)
				return;
			
			Repository repo = VersionControlProjectService.GetRepository (prj, file);
			if (repo == null)
				return;
			
			// TODO: Monitor changes not just to project files
			// but also to .svn directories to catch commits
			// and updates.
			
			WatchProject (prj);
			
			// When a file had a status and later has no status,
			// for whatever reason, it needs to be removed from the hashtable.
			fileStatus.Remove(file);
			
			VersionStatus status = GetVersionInfo (repo, file);
			
			fileStatus[file] = status;
			
			Gdk.Pixbuf overlay = VersionControlProjectService.LoadOverlayIconForStatus (status);
			if (overlay != null)
				AddOverlay (ref icon, overlay);
		}
		
		void AddOverlay (ref Gdk.Pixbuf icon, Gdk.Pixbuf overlay)
		{
			double scale = 1;//(double)(2*icon.Width/3) / (double)overlay.Width;
			int w = (int)(overlay.Width*scale);
			int h = (int)(overlay.Height*scale);
			icon = icon.Copy();
			overlay.Composite(icon,
				icon.Width-w,  icon.Height-h,
				w, h,
				icon.Width-w, icon.Height-h,
				scale,scale, Gdk.InterpType.Bilinear, 255); 
		}
		
		VersionStatus GetVersionInfo (Repository vc, string filepath)
		{
			if (vc.IsVersioned (filepath)) {
				VersionInfo node = vc.GetVersionInfo(filepath, false);
				return node.Status;
			}
			return VersionStatus.Unknown;
		}
		
		void WatchProject (Project project) 
		{
			if (projectsWatched.ContainsKey(project)) return;
			projectsWatched[project] = projectsWatched;
			project.FileChangedInProject += new ProjectFileEventHandler(Monitor);
		}
		
		void Monitor (object sender, ProjectFileEventArgs args)
		{
			// If the status of the file actually changed, then
			// update the project pad so the overlays are updated.
			
			Repository repo = VersionControlProjectService.GetRepository (args.ProjectFile.Project, args.ProjectFile.FilePath);
			if (repo == null)
				return;
			
			string file = args.ProjectFile.FilePath;
			
			VersionStatus newstatus = GetVersionInfo (repo, file);
			if (newstatus == VersionStatus.Unknown && !fileStatus.ContainsKey(file))
				return; // had no status before, has no status now

			if (!fileStatus.ContainsKey(file)
				|| (fileStatus.ContainsKey(file) 
					&& (VersionStatus)fileStatus[file] != newstatus)) {
				// No status before and has status now, or
				// status changed.  Refresh the project pad.
				ITreeBuilder builder = Context.GetTreeBuilder(args.ProjectFile);
				if (builder != null)
					builder.UpdateAll();
			}
		}
		
		public override void Dispose() 
		{
			foreach (Project p in projectsWatched.Keys)
				p.FileChangedInProject -= new ProjectFileEventHandler(Monitor);
			projectsWatched.Clear();
		}
		
		
		public override Type CommandHandlerType {
			get { return typeof(AddinCommandHandler); }
		}
	}
	
	class AddinCommandHandler : NodeCommandHandler 
	{
		[CommandHandler (Commands.Update)]
		protected void OnUpdate() {
			RunCommand(Commands.Update, false);
		}
		
		[CommandUpdateHandler (Commands.Update)]
		protected void UpdateUpdate(CommandInfo item) {
			TestCommand(Commands.Update, item);
		}
		
		[CommandHandler (Commands.Diff)]
		protected void OnDiff() {
			RunCommand(Commands.Diff, false);
		}
		
		[CommandUpdateHandler (Commands.Diff)]
		protected void UpdateDiff(CommandInfo item) {
			TestCommand(Commands.Diff, item);
		}
		
		[CommandHandler (Commands.Log)]
		protected void OnLog() {
			RunCommand(Commands.Log, false);
		}
		
		[CommandUpdateHandler (Commands.Log)]
		protected void UpdateLog(CommandInfo item) {
			TestCommand(Commands.Log, item);
		}
		
		[CommandHandler (Commands.Status)]
		protected void OnStatus() {
			RunCommand(Commands.Status, false);
		}
		
		[CommandUpdateHandler (Commands.Status)]
		protected void UpdateStatus(CommandInfo item) {
			TestCommand(Commands.Status, item);
		}
		
		[CommandHandler (Commands.Commit)]
		protected void OnCommit() {
			RunCommand (Commands.Commit, false);
		}
		
		[CommandUpdateHandler (Commands.Commit)]
		protected void UpdateCommit (CommandInfo item) {
			TestCommand(Commands.Commit, item);
		}
		
		[CommandHandler (Commands.Add)]
		protected void OnAdd() {
			RunCommand(Commands.Add, false);
		}
		
		[CommandUpdateHandler (Commands.Add)]
		protected void UpdateAdd(CommandInfo item) {
			TestCommand(Commands.Add, item);
		}
		
		[CommandHandler (Commands.Remove)]
		protected void OnRemove() {
			RunCommand(Commands.Remove, false);
		}
		
		[CommandUpdateHandler (Commands.Remove)]
		protected void UpdateRemove(CommandInfo item) {
			TestCommand(Commands.Remove, item);
		}
		
		[CommandHandler (Commands.Publish)]
		protected void OnPublish() 
		{
			RunCommand(Commands.Publish, false);
		}
		
		[CommandUpdateHandler (Commands.Publish)]
		protected void UpdatePublish(CommandInfo item) {
			TestCommand(Commands.Publish, item);
		}
			
		private void TestCommand(Commands cmd, CommandInfo item) {
			item.Visible = RunCommand(cmd, true);
		}
		
		private bool RunCommand(Commands cmd, bool test)
		{
			string path;
			bool isDir;
			CombineEntry pentry;
			
			if (CurrentNode.DataItem is ProjectFile) {
				ProjectFile file = (ProjectFile)CurrentNode.DataItem;
				path = file.FilePath;
				isDir = false;
				pentry = file.Project;
			} else if (CurrentNode.DataItem is SystemFile) {
				SystemFile file = (SystemFile)CurrentNode.DataItem;
				path = file.Path;
				isDir = false;
				pentry = file.Project;
			} else if (CurrentNode.DataItem is Project) {
				Project project = (Project)CurrentNode.DataItem;
				path = project.BaseDirectory;
				isDir = true;
				pentry = project;
			} else if (CurrentNode.DataItem is ProjectFolder) {
				ProjectFolder f = ((ProjectFolder)CurrentNode.DataItem);
				path = f.Path;
				isDir = true;
				pentry = f.Project;
			} else if (CurrentNode.DataItem is Combine) {
				Combine c = ((Combine)CurrentNode.DataItem);
				path = c.BaseDirectory;
				isDir = true;				
				pentry = c;
			} else {
				Console.Error.WriteLine(CurrentNode.DataItem);
				return false;
			}
			
			Repository repo = VersionControlProjectService.GetRepository (pentry, path);
			if (repo == null && cmd != Commands.Publish)
				return false;
			
			switch (cmd) {
				case Commands.Update:
					return UpdateCommand.Update (repo, path, test);
				case Commands.Diff:
					return DiffView.Show (repo, path, test);
				case Commands.Log:
					return LogView.Show (repo, path, isDir, null, test);
				case Commands.Status:
					return StatusView.Show (repo, path, test);
				case Commands.Commit:
					return CommitCommand.Commit (repo, path, null, test);
				case Commands.Add:
					return AddCommand.Add (repo, path, test);
				case Commands.Remove:
					return RemoveCommand.Remove (repo, path, test);
				case Commands.Publish:
					if (!isDir) return false;
					return PublishCommand.Publish (pentry, path, test);
			}
			return false;
		}
	}
}
