using System;
using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.VersionControl.Views;
using MonoDevelop.Ide;


namespace MonoDevelop.VersionControl
{
	class VersionControlNodeExtension : NodeBuilderExtension
	{
		Dictionary<FilePath,object> pathToObject = new Dictionary<FilePath, object> ();
		
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

		protected override void Initialize ()
		{
			base.Initialize ();
		}

		public override void Dispose ()
		{
			base.Dispose ();
		}

		public override void BuildNode (ITreeBuilder builder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			if (!builder.Options["ShowVersionControlOverlays"])
				return;
		
			// Add status overlays
			
			if (dataObject is IWorkspaceObject) {
				IWorkspaceObject ce = (IWorkspaceObject) dataObject;
				Repository rep = VersionControlService.GetRepository (ce);
				if (rep != null) {
					AddFolderOverlay (rep, ce.BaseDirectory, ref icon, ref closedIcon, false);
					rep.GetDirectoryVersionInfo (ce.BaseDirectory, false, false);
				}
				return;
			} else if (dataObject is ProjectFolder) {
				ProjectFolder ce = (ProjectFolder) dataObject;
				if (ce.ParentWorkspaceObject != null) {
					Repository rep = VersionControlService.GetRepository (ce.ParentWorkspaceObject);
					if (rep != null) {
						AddFolderOverlay (rep, ce.Path, ref icon, ref closedIcon, true);
						rep.GetDirectoryVersionInfo (ce.Path, false, false);
					}
				}
				return;
			}
			
			IWorkspaceObject prj;
			FilePath file;
			
			if (dataObject is ProjectFile) {
				ProjectFile pfile = (ProjectFile) dataObject;
				prj = pfile.Project;
				file = pfile.FilePath;
			} else {
				SystemFile pfile = (SystemFile) dataObject;
				prj = pfile.ParentWorkspaceObject;
				file = pfile.Path;
			}
			
			if (prj == null)
				return;
			
			Repository repo = VersionControlService.GetRepository (prj);
			if (repo == null)
				return;
			
			VersionInfo vi = repo.GetVersionInfo (file);

			Gdk.Pixbuf overlay = VersionControlService.LoadOverlayIconForStatus (vi.Status);
			if (overlay != null)
				AddOverlay (ref icon, overlay);
		}

/*		public override void PrepareChildNodes (object dataObject)
		{
			if (dataObject is IWorkspaceObject) {
				IWorkspaceObject ce = (IWorkspaceObject) dataObject;
				Repository rep = VersionControlService.GetRepository (ce);
				if (rep != null)
					rep.GetDirectoryVersionInfo (ce.BaseDirectory, false, false);
			} else if (dataObject is ProjectFolder) {
				ProjectFolder ce = (ProjectFolder) dataObject;
				if (ce.ParentWorkspaceObject != null) {
					Repository rep = VersionControlService.GetRepository (ce.ParentWorkspaceObject);
					if (rep != null)
						rep.GetDirectoryVersionInfo (ce.Path, false, false);
				}
			}
			base.PrepareChildNodes (dataObject);
		}
*/		
		void AddFolderOverlay (Repository rep, string folder, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon, bool skipVersionedOverlay)
		{
			Gdk.Pixbuf overlay = null;
			VersionInfo vinfo = rep.GetVersionInfo (folder);
			if (vinfo == null || !vinfo.IsVersioned) {
				overlay = VersionControlService.LoadOverlayIconForStatus (VersionStatus.Unversioned);
			} else if (vinfo.IsVersioned && !vinfo.HasLocalChanges) {
				if (!skipVersionedOverlay)
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
		
		void Monitor (object sender, FileUpdateEventArgs args)
		{
			foreach (FileUpdateEventInfo uinfo in args) {
				foreach (var ob in GetObjectsForPath (uinfo.FilePath)) {
					ITreeBuilder builder = Context.GetTreeBuilder (ob);
					if (builder != null)
						builder.Update();
				}
			}
		}

		void RegisterObjectPath (FilePath path, object ob)
		{
			path = path.CanonicalPath;
			object currentObj;
			if (pathToObject.TryGetValue (path, out currentObj)) {
				if (currentObj is List<object>) {
					var list = (List<object>) currentObj;
					list.Add (ob);
				} else {
					var list = new List<object> (2);
					list.Add (currentObj);
					list.Add (ob);
					pathToObject [path] = list;
				}
			} else
				pathToObject [path] = ob;
		}

		void UnregisterObjectPath (FilePath path, object ob)
		{
			path = path.CanonicalPath;
			object currentObj;
			if (pathToObject.TryGetValue (path, out currentObj)) {
				if (currentObj is List<object>) {
					var list = (List<object>) currentObj;
					if (list.Remove (ob)) {
						if (list.Count == 1)
							pathToObject [path] = list[0];
					}
				} else if (currentObj == ob)
					pathToObject.Remove (path);
			}
		}

		IEnumerable<object> GetObjectsForPath (FilePath path)
		{
			path = path.CanonicalPath;
			object currentObj;
			if (pathToObject.TryGetValue (path, out currentObj)) {
				if (currentObj is List<object>) {
					foreach (var ob in (List<object>) currentObj)
						yield return ob;
				} else
					yield return currentObj;
			}
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			FilePath path = GetPath (dataObject);
			if (path != FilePath.Null)
				RegisterObjectPath (path, dataObject);
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			FilePath path = GetPath (dataObject);
			if (path != FilePath.Null)
				UnregisterObjectPath (path, dataObject);
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
			return FilePath.Null;
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
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Annotate)]
		protected void OnAnnotate() {
			RunCommand(Commands.Annotate, false);
		}
		
		[CommandUpdateHandler (Commands.Annotate)]
		protected void UpdateAnnotate(CommandInfo item) {
			TestCommand(Commands.Annotate, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.CreatePatch)]
		protected void OnCreatePatch() {
			RunCommand(Commands.CreatePatch, false);
		}
		
		[CommandUpdateHandler (Commands.CreatePatch)]
		protected void UpdateCreatePatch(CommandInfo item) {
			TestCommand(Commands.CreatePatch, item);
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
				} else if (it.Repository.VersionControlSystem != null && !it.Repository.VersionControlSystem.IsInstalled) {
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
					res = DiffCommand.Show (items, test);
					break;
				case Commands.Log:
					res = LogCommand.Show (items, test);
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
				case Commands.Annotate:
					res = BlameCommand.Show (items, test);
					break;
				case Commands.CreatePatch:
					res = CreatePatchCommand.CreatePatch (items, test);
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

		public override void RefreshItem ()
		{
			foreach (VersionControlItem it in GetItems ()) {
				if (it.Repository != null)
					it.Repository.ClearCachedVersionInfo (it.Path);
			}
			base.RefreshItem ();
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
