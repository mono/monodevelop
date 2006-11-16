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
		Hashtable filePaths = new Hashtable();
	
		public override bool CanBuildNode (Type dataType)
		{
			//Console.Error.WriteLine(dataType);
			return typeof(ProjectFile).IsAssignableFrom (dataType)
				|| typeof(SystemFile).IsAssignableFrom (dataType)
				|| typeof(Project).IsAssignableFrom (dataType)
				|| typeof(ProjectFolder).IsAssignableFrom (dataType)
				|| typeof(Combine).IsAssignableFrom (dataType);
		}
		
		public VersionControlNodeExtension ()
		{
			VersionControlProjectService.FileStatusChanged += Monitor;
		}
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			// Add status overlays
			
			if (dataObject is CombineEntry) {
				CombineEntry ce = (CombineEntry) dataObject;
				Repository rep = VersionControlProjectService.GetRepository (ce, ce.FileName);
				if (rep != null)
					AddFolderOverlay (rep, ce.BaseDirectory, ref icon, ref closedIcon);
				return;
			} else if (dataObject is ProjectFolder) {
				ProjectFolder ce = (ProjectFolder) dataObject;
				Repository rep = VersionControlProjectService.GetRepository (ce.Project, ce.Path);
				if (rep != null)
					AddFolderOverlay (rep, ce.Path, ref icon, ref closedIcon);
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
			
			VersionStatus status = GetVersionInfo (repo, file);
			Gdk.Pixbuf overlay = VersionControlProjectService.LoadOverlayIconForStatus (status);
			if (overlay != null)
				AddOverlay (ref icon, overlay);
		}
		
		void AddFolderOverlay (Repository rep, string folder, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Gdk.Pixbuf overlay = null;
			VersionInfo vinfo = rep.GetVersionInfo (folder, false);
			if (vinfo == null) {
				overlay = VersionControlProjectService.LoadOverlayIconForStatus (VersionStatus.Unversioned);
			} else if (vinfo.Status == VersionStatus.Unchanged) {
				overlay = VersionControlProjectService.overlay_controled;
			} else {
				overlay = VersionControlProjectService.LoadOverlayIconForStatus (vinfo.Status);
			}
			if (overlay != null) {
				AddOverlay (ref icon, overlay);
				if (closedIcon != null)
					AddOverlay (ref closedIcon, overlay);
			}
		}
		
		void AddOverlay (ref Gdk.Pixbuf icon, Gdk.Pixbuf overlay)
		{
			int dx = 2;
			int dy = 3;
			
			Gdk.Pixbuf res = new Gdk.Pixbuf (icon.Colorspace, icon.HasAlpha, icon.BitsPerSample, icon.Width + dx, icon.Height + dy);
			res.Fill (0);
			icon.CopyArea (0, 0, icon.Width, icon.Height, res, 0, 0);
			
			overlay.Composite (res,
				res.Width - overlay.Width,  res.Height - overlay.Height,
				overlay.Width, overlay.Height,
				res.Width - overlay.Width,  res.Height - overlay.Height,
				1, 1, Gdk.InterpType.Bilinear, 255); 
			
			icon = res;
		}
		
		VersionStatus GetVersionInfo (Repository vc, string filepath)
		{
			VersionInfo node = vc.GetVersionInfo (filepath, false);
			if (node != null)
				return node.Status;
			return VersionStatus.Unversioned;
		}
		
		void Monitor (object sender, FileUpdateEventArgs args)
		{
			object obj = filePaths [args.FilePath];
			if (obj != null) {
				ITreeBuilder builder = Context.GetTreeBuilder (obj);
				if (builder != null)
					builder.UpdateAll();
			}
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			string path = GetPath (dataObject);
			if (path != null)
				filePaths [path] = dataObject;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			string path = GetPath (dataObject);
			if (path != null)
				filePaths.Remove (path);
		}
		
		internal static string GetPath (object dataObject)
		{
			if (dataObject is ProjectFile) {
				return ((ProjectFile) dataObject).FilePath;
			} else if (dataObject is SystemFile) {
				return ((SystemFile) dataObject).Path;
			} else if (dataObject is CombineEntry) {
				return ((CombineEntry)dataObject).BaseDirectory;
			} else if (dataObject is ProjectFolder) {
				return ((ProjectFolder)dataObject).Path;
			}
			return null;
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
		
		[CommandHandler (Commands.Revert)]
		protected void OnRevert() {
			RunCommand(Commands.Revert, false);
		}
		
		[CommandUpdateHandler (Commands.Revert)]
		protected void UpdateRevert(CommandInfo item) {
			TestCommand(Commands.Revert, item);
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
					return CommitCommand.Commit (repo, path, test);
				case Commands.Add:
					return AddCommand.Add (repo, path, test);
				case Commands.Remove:
					return RemoveCommand.Remove (repo, path, test);
				case Commands.Revert:
					return RevertCommand.Revert (repo, path, test);
				case Commands.Publish:
					if (!isDir) return false;
					return PublishCommand.Publish (pentry, path, test);
			}
			return false;
		}
	}
}
