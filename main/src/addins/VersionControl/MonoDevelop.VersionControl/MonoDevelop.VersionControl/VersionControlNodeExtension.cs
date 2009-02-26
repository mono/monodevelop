using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

using MonoDevelop.VersionControl.Views;


namespace MonoDevelop.VersionControl
{
	class VersionControlNodeExtension : NodeBuilderExtension
	{
		Hashtable filePaths = new Hashtable();
	
		public override bool CanBuildNode (Type dataType)
		{
			//Console.Error.WriteLine(dataType);
			return typeof(ProjectFile).IsAssignableFrom (dataType)
				|| typeof(SystemFile).IsAssignableFrom (dataType)
				|| typeof(ProjectFolder).IsAssignableFrom (dataType)
				|| typeof(IWorkspaceObject).IsAssignableFrom (dataType);
		}
		
		public VersionControlNodeExtension ()
		{
			VersionControlService.FileStatusChanged += Monitor;
		}
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			if (!builder.Options["ShowVersionControlOverlays"])
				return;
		
			// Add status overlays
			
			if (dataObject is IWorkspaceObject) {
				IWorkspaceObject ce = (IWorkspaceObject) dataObject;
				Repository rep = VersionControlService.GetRepository (ce);
				if (rep != null)
					AddFolderOverlay (rep, ce.BaseDirectory, ref icon, ref closedIcon);
				return;
			} else if (dataObject is ProjectFolder) {
				ProjectFolder ce = (ProjectFolder) dataObject;
				if (ce.Project != null) {
					Repository rep = VersionControlService.GetRepository (ce.Project);
					if (rep != null)
						AddFolderOverlay (rep, ce.Path, ref icon, ref closedIcon);
				}
				return;
			}
			
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
			
			Repository repo = VersionControlService.GetRepository (prj);
			if (repo == null)
				return;
			
			VersionStatus status = GetVersionInfo (repo, file);
			Gdk.Pixbuf overlay = VersionControlService.LoadOverlayIconForStatus (status);
			if (overlay != null)
				AddOverlay (ref icon, overlay);
		}
		
		void AddFolderOverlay (Repository rep, string folder, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Gdk.Pixbuf overlay = null;
			VersionInfo vinfo = rep.GetVersionInfo (folder, false);
			if (vinfo == null || !vinfo.IsVersioned) {
				overlay = VersionControlService.LoadOverlayIconForStatus (VersionStatus.Unversioned);
			} else if (vinfo.IsVersioned && !vinfo.HasLocalChanges) {
				overlay = VersionControlService.overlay_controled;
			} else {
				overlay = VersionControlService.LoadOverlayIconForStatus (vinfo.Status);
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
			int dy = 2;
			
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
			if (dataObject is ProjectFile) {
				return ((ProjectFile) dataObject).FilePath;
			} else if (dataObject is SystemFile) {
				return ((SystemFile) dataObject).Path;
			} else if (dataObject is IWorkspaceObject) {
				return ((IWorkspaceObject)dataObject).BaseDirectory;
			} else if (dataObject is ProjectFolder) {
				return ((ProjectFolder)dataObject).Path;
			}
			return null;
		}
		
		public override Type CommandHandlerType {
			get { return typeof(AddinCommandHandler); }
		}
	}

	

	
	class AddinCommandHandler : VersionControlCommandHandler 
	{
		[AllowMultiSelection]
		[CommandHandler (Commands.Update)]
		protected void OnUpdate() {
			RunCommand(Commands.Update, false);
		}
		
		[CommandUpdateHandler (Commands.Update)]
		protected void UpdateUpdate(CommandInfo item) {
			TestCommand(Commands.Update, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Diff)]
		protected void OnDiff() {
			RunCommand(Commands.Diff, false);
		}
		
		[CommandUpdateHandler (Commands.Diff)]
		protected void UpdateDiff(CommandInfo item) {
			TestCommand(Commands.Diff, item);
		}
		
		[AllowMultiSelection]
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
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Add)]
		protected void OnAdd() {
			RunCommand(Commands.Add, false);
		}
		
		[CommandUpdateHandler (Commands.Add)]
		protected void UpdateAdd(CommandInfo item) {
			TestCommand(Commands.Add, item);
		}
		
		[AllowMultiSelection]
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
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Revert)]
		protected void OnRevert() {
			RunCommand(Commands.Revert, false);
		}
		
		[CommandUpdateHandler (Commands.Revert)]
		protected void UpdateRevert(CommandInfo item) {
			TestCommand(Commands.Revert, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Lock)]
		protected void OnLock() {
			RunCommand(Commands.Lock, false);
		}
		
		[CommandUpdateHandler (Commands.Lock)]
		protected void UpdateLock(CommandInfo item) {
			TestCommand(Commands.Lock, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Unlock)]
		protected void OnUnlock() {
			RunCommand(Commands.Unlock, false);
		}
		
		[CommandUpdateHandler (Commands.Unlock)]
		protected void UpdateUnlock(CommandInfo item) {
			TestCommand(Commands.Unlock, item);
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
		
		private TestResult RunCommand (Commands cmd, bool test)
		{
			VersionControlItemList items = GetItems ();

			foreach (VersionControlItem it in items) {
				if (it.Repository == null) {
					if (cmd != Commands.Publish)
						return TestResult.NoVersionControl;
				} else if (!it.Repository.VersionControlSystem.IsInstalled) {
					return TestResult.Disable;
				}
			}

			bool res = false;
			
			try {
				switch (cmd) {
					case Commands.Update:
						res = UpdateCommand.Update (items, test);
						break;
					case Commands.Diff:
						res = DiffView.Show (items, test);
						break;
					case Commands.Log:
						res = LogView.Show (items, null, test);
						break;
					case Commands.Status:
						res = StatusView.Show (items, test);
						break;
					case Commands.Commit:
						res = CommitCommand.Commit (items, test);
						break;
					case Commands.Add:
						res = AddCommand.Add (items, test);
						break;
					case Commands.Remove:
						res = RemoveCommand.Remove (items, test);
						break;
					case Commands.Revert:
						res = RevertCommand.Revert (items, test);
						break;
					case Commands.Lock:
						res = LockCommand.Lock (items, test);
						break;
					case Commands.Unlock:
						res = UnlockCommand.Unlock (items, test);
						break;
					case Commands.Publish:
						VersionControlItem it = items [0];
						if (items.Count == 1 && it.IsDirectory && it.WorkspaceObject != null)
							res = PublishCommand.Publish (it.WorkspaceObject, it.Path, test);
						break;
				}
			}
			catch (Exception ex) {
				if (test)
					LoggingService.LogError (ex.ToString ());
				else
					MessageService.ShowException (ex, GettextCatalog.GetString ("Version control command failed."));
				return TestResult.Disable;
			}
			
			return res ? TestResult.Enable : TestResult.Disable;
		}
	}

	class OpenCommandHandler : VersionControlCommandHandler 
	{
		[AllowMultiSelection]
		[CommandHandler (ViewCommands.Open)]
		protected void OnOpen ()
		{
			foreach (VersionControlItem it in GetItems ()) {
				if (!it.IsDirectory)
					IdeApp.Workbench.OpenDocument (it.Path);
			}
		}
	}		
	
	
	enum TestResult
	{
		Enable,
		Disable,
		NoVersionControl
	}
}
