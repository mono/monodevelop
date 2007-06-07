using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.SolutionViewPad;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Components.Commands;

using MonoDevelop.VersionControl.Views;


namespace MonoDevelop.VersionControl
{
	class VersionControlNodeExtension : NodeBuilderExtension
	{
		Hashtable filePaths = new Hashtable();
	
		public override bool CanBuildNode (Type dataType)
		{
			//Console.Error.WriteLine(dataType);
			return typeof(FileNode).IsAssignableFrom (dataType)
				|| typeof(SystemFileNode).IsAssignableFrom (dataType)
				|| typeof(SolutionProject).IsAssignableFrom (dataType)
				|| typeof(FolderNode).IsAssignableFrom (dataType)
				|| typeof(Solution).IsAssignableFrom (dataType);
		}
		
		public VersionControlNodeExtension ()
		{
			VersionControlProjectService.FileStatusChanged += Monitor;
		}
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			if (!builder.Options["ShowVersionControlOverlays"])
				return;
		
			// Add status overlays
			
			if (dataObject is Solution) {
				Solution ce = (Solution) dataObject;
				Repository rep = VersionControlProjectService.GetRepository (ce);
				if (rep != null)
					AddFolderOverlay (rep, Path.GetDirectoryName (ProjectService.SolutionFileName) /* ce.BaseDirectory */, ref icon, ref closedIcon);
				return;
			} else if (dataObject is SolutionProject) {
				SolutionProject ce = (SolutionProject) dataObject;
				Repository rep = VersionControlProjectService.GetRepository (ce.Project);
				if (rep != null)
					AddFolderOverlay (rep, Path.Combine (Path.GetDirectoryName (ProjectService.SolutionFileName), ce.Location), ref icon, ref closedIcon);
				return;
			} else if (dataObject is FolderNode) {
				FolderNode ce = (FolderNode) dataObject;
				Repository rep = VersionControlProjectService.GetRepository (ce.Project.Project);
				if (rep != null)
					AddFolderOverlay (rep, ce.Path, ref icon, ref closedIcon);
				return;
			}
			
			SolutionProject prj;
			string file;
			
			if (dataObject is FileNode) {
				FileNode pfile = (FileNode) dataObject;
				prj = pfile.Project;
				file = pfile.FileName;
			} else {
				SystemFileNode pfile = (SystemFileNode) dataObject;
				prj = pfile.Project;
				file = pfile.FileName;
			}
			
			if (prj == null)
				return;
			
			Repository repo = VersionControlProjectService.GetRepository (prj.Project);
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
			Gdk.Pixbuf cached = Context.GetComposedIcon (icon, overlay);
			if (cached != null) {
				icon = cached;
				return;
			}
			
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
			
			Context.CacheComposedIcon (icon, overlay, res);
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
			if (dataObject is FileNode) {
				return Path.GetDirectoryName (((FileNode) dataObject).FileName);
			} else if (dataObject is SystemFileNode) {
				return Path.GetDirectoryName (((SystemFileNode) dataObject).FileName);
			} else if (dataObject is Solution) {
				return Path.GetDirectoryName (ProjectService.SolutionFileName);//((Solution)dataObject).BaseDirectory;
			} else if (dataObject is FolderNode) {
				return ((FolderNode)dataObject).Path;
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
			TestResult res = RunCommand(cmd, true);
			if (res == TestResult.NoVersionControl && cmd == Commands.Log) {
				// Use the update command to show the "not available" message
				item.Icon = null;
				item.Enabled = false;
				item.Text = GettextCatalog.GetString ("This project or folder is not under version control");
			} else
				item.Visible = res == TestResult.Enable;
		}
		
		private TestResult RunCommand(Commands cmd, bool test)
		{
			string path;
			bool isDir;
			SolutionProject pentry;
			
			if (CurrentNode.DataItem is FileNode) {
				FileNode file = (FileNode)CurrentNode.DataItem;
				path = Path.GetDirectoryName (file.FileName);
				isDir = false;
				pentry = file.Project;
			} else if (CurrentNode.DataItem is SystemFileNode) {
				SystemFileNode file = (SystemFileNode)CurrentNode.DataItem;
				path = Path.GetDirectoryName (file.FileName);
				isDir = false;
				pentry = file.Project;
			} else if (CurrentNode.DataItem is SolutionProject) {
				SolutionProject project = (SolutionProject)CurrentNode.DataItem;
				path = Path.Combine (Path.GetDirectoryName (ProjectService.SolutionFileName),
				                     project.Location);
				isDir = true;
				pentry = project;
			} else if (CurrentNode.DataItem is FolderNode) {
				FolderNode f = ((FolderNode)CurrentNode.DataItem);
				path = f.Path;
				isDir = true;
				pentry = f.Project;
			} else if (CurrentNode.DataItem is Solution) {
				Solution c = (Solution)CurrentNode.DataItem;
				path = Path.GetDirectoryName (ProjectService.SolutionFileName);// c.BaseDirectory;
				isDir = true;				
				pentry = null;
			} else {
				Console.Error.WriteLine(CurrentNode.DataItem);
				return TestResult.NoVersionControl;
			}
			
			Repository repo = VersionControlProjectService.GetRepository (pentry.Project);
			if (repo == null) {
				if (cmd != Commands.Publish)
					return TestResult.NoVersionControl;
			} else if (!repo.VersionControlSystem.IsInstalled) {
				return TestResult.Disable;
			}
			
			bool res = false;
			
			try {
				switch (cmd) {
					case Commands.Update:
						res = UpdateCommand.Update (repo, path, test);
						break;
					case Commands.Diff:
						res = DiffView.Show (repo, path, test);
						break;
					case Commands.Log:
						res = LogView.Show (repo, path, isDir, null, test);
						break;
					case Commands.Status:
						res = StatusView.Show (repo, path, test);
						break;
					case Commands.Commit:
						res = CommitCommand.Commit (repo, path, test);
						break;
					case Commands.Add:
						res = AddCommand.Add (repo, path, test);
						break;
					case Commands.Remove:
						res = RemoveCommand.Remove (repo, path, isDir, test);
						break;
					case Commands.Revert:
						res = RevertCommand.Revert (repo, path, test);
						break;
					case Commands.Publish:
						if (isDir)
							res = PublishCommand.Publish (pentry == null ? null : pentry.Project, path, test);
						break;
				}
			}
			catch (Exception ex) {
				if (test)
					Runtime.LoggingService.Error (ex);
				else
					IdeApp.Services.MessageService.ShowError (ex, GettextCatalog.GetString ("Version control command failed."));
				return TestResult.Disable;
			}
			
			return res ? TestResult.Enable : TestResult.Disable;
		}
	}
	
	enum TestResult
	{
		Enable,
		Disable,
		NoVersionControl
	}
}
