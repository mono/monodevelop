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
using System.Threading.Tasks;
using System.Threading;

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
				|| typeof(WorkspaceObject).IsAssignableFrom (dataType);
		}
		
		protected override void Initialize ()
		{
			base.Initialize ();
			VersionControlService.FileStatusChanged += Monitor;
			IdeApp.Workspace.LastWorkspaceItemClosed += OnWorkspaceRefresh;
		}

		public override void Dispose ()
		{
			VersionControlService.FileStatusChanged -= Monitor;
			IdeApp.Workspace.LastWorkspaceItemClosed -= OnWorkspaceRefresh;
			base.Dispose ();
		}

		void OnWorkspaceRefresh (object sender, EventArgs args)
		{
			pathToObject.Clear ();
		}

		public override void BuildNode (ITreeBuilder builder, object dataObject, NodeInfo nodeInfo)
		{
			if (!builder.Options["ShowVersionControlOverlays"])
				return;
		
			// Add status overlays
			
			if (dataObject is WorkspaceObject) {
				WorkspaceObject ce = (WorkspaceObject) dataObject;
				Repository rep = VersionControlService.GetRepository (ce);
				if (rep != null) {
					rep.GetDirectoryVersionInfo (ce.BaseDirectory, false, false);
					AddFolderOverlay (rep, ce.BaseDirectory, nodeInfo, false);
				}
				return;
			} else if (dataObject is ProjectFolder) {
				ProjectFolder ce = (ProjectFolder) dataObject;
				if (ce.ParentWorkspaceObject != null) {
					Repository rep = VersionControlService.GetRepository (ce.ParentWorkspaceObject);
					if (rep != null) {
						rep.GetDirectoryVersionInfo (ce.Path, false, false);
						AddFolderOverlay (rep, ce.Path, nodeInfo, true);
					}
				}
				return;
			}
			
			WorkspaceObject prj;
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

			var overlay = VersionControlService.LoadOverlayIconForStatus (vi.Status);
			if (overlay != null)
				nodeInfo.OverlayBottomRight = overlay;
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
		static void AddFolderOverlay (Repository rep, string folder, NodeInfo nodeInfo, bool skipVersionedOverlay)
		{
			Xwt.Drawing.Image overlay = null;
			VersionInfo vinfo = rep.GetVersionInfo (folder);
			if (vinfo == null || !vinfo.IsVersioned) {
				overlay = VersionControlService.LoadOverlayIconForStatus (VersionStatus.Unversioned);
			} else if (vinfo.IsVersioned && !vinfo.HasLocalChanges) {
				if (!skipVersionedOverlay)
					overlay = VersionControlService.overlay_controled;
			} else {
				overlay = VersionControlService.LoadOverlayIconForStatus (vinfo.Status);
			}
			if (overlay != null)
				nodeInfo.OverlayBottomRight = overlay;
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
			} else if (dataObject is WorkspaceObject) {
				return ((WorkspaceObject)dataObject).BaseDirectory;
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
		protected async Task OnUpdate() {
			await RunCommand(Commands.Update, false);
		}
		
		[CommandUpdateHandler (Commands.Update)]
		protected async Task UpdateUpdate(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.Update, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Diff)]
		protected async Task OnDiff() {
			await RunCommand(Commands.Diff, false);
		}
		
		[CommandUpdateHandler (Commands.Diff)]
		protected async Task UpdateDiff(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.Diff, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Log)]
		protected async Task OnLog() {
			await RunCommand(Commands.Log, false);
		}
		
		[CommandUpdateHandler (Commands.Log)]
		protected async Task UpdateLog(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.Log, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Status)]
		protected async Task OnStatus() {
			await RunCommand(Commands.Status, false);
		}
		
		[CommandUpdateHandler (Commands.Status)]
		protected async Task UpdateStatus(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.Status, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Add)]
		protected async Task OnAdd() {
			await RunCommand(Commands.Add, false);
		}
		
		[CommandUpdateHandler (Commands.Add)]
		protected async Task UpdateAdd(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.Add, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Remove)]
		protected async Task OnRemove() {
			await RunCommand(Commands.Remove, false);
		}
		
		[CommandUpdateHandler (Commands.Remove)]
		protected async Task UpdateRemove(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.Remove, item);
		}
		
		[CommandHandler (Commands.Publish)]
		protected async Task OnPublish() 
		{
			await RunCommand(Commands.Publish, false);
		}
		
		[CommandUpdateHandler (Commands.Publish)]
		protected async Task UpdatePublish(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.Publish, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Revert)]
		protected async Task OnRevert() {
			await RunCommand(Commands.Revert, false, false);
		}
		
		[CommandUpdateHandler (Commands.Revert)]
		protected async Task UpdateRevert(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.Revert, item, false);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Lock)]
		protected async Task OnLock() {
			await RunCommand(Commands.Lock, false);
		}
		
		[CommandUpdateHandler (Commands.Lock)]
		protected async Task UpdateLock(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.Lock, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Unlock)]
		protected async Task OnUnlock() {
			await RunCommand(Commands.Unlock, false);
		}
		
		[CommandUpdateHandler (Commands.Unlock)]
		protected async Task UpdateUnlock(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.Unlock, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.Annotate)]
		protected async Task OnAnnotate() {
			await RunCommand(Commands.Annotate, false);
		}
		
		[CommandUpdateHandler (Commands.Annotate)]
		protected async Task UpdateAnnotate(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.Annotate, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (Commands.CreatePatch)]
		protected async Task OnCreatePatch() {
			await RunCommand(Commands.CreatePatch, false);
		}
		
		[CommandUpdateHandler (Commands.CreatePatch)]
		protected async Task UpdateCreatePatch(CommandInfo item, CancellationToken token) {
			await TestCommand(Commands.CreatePatch, item);
		}

		[AllowMultiSelection]
		[CommandHandler (Commands.Ignore)]
		protected async Task OnIgnore ()
		{
			await RunCommand(Commands.Ignore, false);
		}

		[CommandUpdateHandler (Commands.Ignore)]
		protected async Task UpdateIgnore (CommandInfo item, CancellationToken token)
		{
			await TestCommand(Commands.Ignore, item);
		}

		[AllowMultiSelection]
		[CommandHandler (Commands.Unignore)]
		protected async Task OnUnignore ()
		{
			await RunCommand(Commands.Unignore, false);
		}

		[CommandUpdateHandler (Commands.Unignore)]
		protected async Task UpdateUnignore (CommandInfo item, CancellationToken token)
		{
			await TestCommand(Commands.Unignore, item);
		}

		[CommandHandler (Commands.ResolveConflicts)]
		protected async Task OnResolveConflicts ()
		{
			await RunCommand (Commands.ResolveConflicts, false, false);
		}

		[CommandUpdateHandler (Commands.ResolveConflicts)]
		protected async Task UpdateResolveConflicts (CommandInfo item, CancellationToken token)
		{
			await TestCommand (Commands.ResolveConflicts, item, false);
		}

		private async Task<TestResult> TestCommand(Commands cmd, CommandInfo item, bool projRecurse = true)
		{
			TestResult res = await RunCommand(cmd, true, projRecurse);
			if (res == TestResult.NoVersionControl && cmd == Commands.Log) {
				// Use the update command to show the "not available" message
				item.Icon = null;
				item.Enabled = false;
				if (VersionControlService.IsGloballyDisabled)
					item.Text = GettextCatalog.GetString ("Version Control support is disabled");
				else
					item.Text = GettextCatalog.GetString ("This project or folder is not under version control");
			} else
				item.Visible = res == TestResult.Enable;

			return res;
		}
		
		private async Task<TestResult> RunCommand (Commands cmd, bool test, bool projRecurse = true)
		{
			VersionControlItemList items = GetItems (projRecurse);

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
					res = await DiffCommand.Show (items, test);
					break;
				case Commands.Log:
					res = await LogCommand.Show (items, test);
					break;
				case Commands.Status:
					res = StatusView.Show (items, test, false);
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
					res = await BlameCommand.Show (items, test);
					break;
				case Commands.CreatePatch:
					res = CreatePatchCommand.CreatePatch (items, test);
					break;
				case Commands.Ignore:
					res = IgnoreCommand.Ignore (items, test);
					break;
				case Commands.Unignore:
					res = UnignoreCommand.Unignore (items, test);
					break;
				case Commands.ResolveConflicts:
					res = await ResolveConflictsCommand.ResolveConflicts (items, test);
					break;
				}
			}
			catch (Exception ex) {
				if (test)
					LoggingService.LogError (ex.ToString ());
				else
					MessageService.ShowError (GettextCatalog.GetString ("Version control command failed."), ex);
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
					IdeApp.Workbench.OpenDocument (it.Path, it.ContainerProject);
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
