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
using MonoDevelop.VersionControl.Commands;


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
		
		protected override void Initialize ()
		{
			base.Initialize ();
			VersionControlService.FileStatusChanged += Monitor;
		}

		public override void Dispose ()
		{
			VersionControlService.FileStatusChanged -= Monitor;
			base.Dispose ();
		}

		public override void BuildNode (ITreeBuilder builder, object dataObject, NodeInfo nodeInfo)
		{
			if (!builder.Options["ShowVersionControlOverlays"])
				return;
		
			// Add status overlays
			
			if (dataObject is IWorkspaceObject) {
				IWorkspaceObject ce = (IWorkspaceObject) dataObject;
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

			nodeInfo.OverlayBottomRight = VersionControlService.LoadOverlayIconForStatus (vi.Status);
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
		[CommandHandler (VersionControlCommands.Update)]
		protected void OnUpdate() {
			RunCommand(VersionControlCommands.Update, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.Update)]
		protected void UpdateUpdate(CommandInfo item) {
			TestCommand(VersionControlCommands.Update, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.Diff)]
		protected void OnDiff() {
			RunCommand(VersionControlCommands.Diff, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.Diff)]
		protected void UpdateDiff(CommandInfo item) {
			TestCommand(VersionControlCommands.Diff, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.Log)]
		protected void OnLog() {
			RunCommand(VersionControlCommands.Log, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.Log)]
		protected void UpdateLog(CommandInfo item) {
			TestCommand(VersionControlCommands.Log, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.Status)]
		protected void OnStatus() {
			RunCommand(VersionControlCommands.Status, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.Status)]
		protected void UpdateStatus(CommandInfo item) {
			TestCommand(VersionControlCommands.Status, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.Add)]
		protected void OnAdd() {
			RunCommand(VersionControlCommands.Add, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.Add)]
		protected void UpdateAdd(CommandInfo item) {
			TestCommand(VersionControlCommands.Add, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.Remove)]
		protected void OnRemove() {
			RunCommand(VersionControlCommands.Remove, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.Remove)]
		protected void UpdateRemove(CommandInfo item) {
			TestCommand(VersionControlCommands.Remove, item);
		}
		
		[CommandHandler (VersionControlCommands.Publish)]
		protected void OnPublish() 
		{
			RunCommand(VersionControlCommands.Publish, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.Publish)]
		protected void UpdatePublish(CommandInfo item) {
			TestCommand(VersionControlCommands.Publish, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.Revert)]
		protected void OnRevert() {
			RunCommand(VersionControlCommands.Revert, false, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.Revert)]
		protected void UpdateRevert(CommandInfo item) {
			TestCommand(VersionControlCommands.Revert, item, false);
		}
		
		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.Lock)]
		protected void OnLock() {
			RunCommand(VersionControlCommands.Lock, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.Lock)]
		protected void UpdateLock(CommandInfo item) {
			TestCommand(VersionControlCommands.Lock, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.Unlock)]
		protected void OnUnlock() {
			RunCommand(VersionControlCommands.Unlock, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.Unlock)]
		protected void UpdateUnlock(CommandInfo item) {
			TestCommand(VersionControlCommands.Unlock, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.Annotate)]
		protected void OnAnnotate() {
			RunCommand(VersionControlCommands.Annotate, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.Annotate)]
		protected void UpdateAnnotate(CommandInfo item) {
			TestCommand(VersionControlCommands.Annotate, item);
		}
		
		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.CreatePatch)]
		protected void OnCreatePatch() {
			RunCommand(VersionControlCommands.CreatePatch, false);
		}
		
		[CommandUpdateHandler (VersionControlCommands.CreatePatch)]
		protected void UpdateCreatePatch(CommandInfo item) {
			TestCommand(VersionControlCommands.CreatePatch, item);
		}

		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.Ignore)]
		protected void OnIgnore ()
		{
			RunCommand(VersionControlCommands.Ignore, false);
		}

		[CommandUpdateHandler (VersionControlCommands.Ignore)]
		protected void UpdateIgnore (CommandInfo item)
		{
			TestCommand(VersionControlCommands.Ignore, item);
		}

		[AllowMultiSelection]
		[CommandHandler (VersionControlCommands.Unignore)]
		protected void OnUnignore ()
		{
			RunCommand(VersionControlCommands.Unignore, false);
		}

		[CommandUpdateHandler (VersionControlCommands.Unignore)]
		protected void UpdateUnignore (CommandInfo item)
		{
			TestCommand(VersionControlCommands.Unignore, item);
		}

		[CommandHandler (VersionControlCommands.ResolveConflicts)]
		protected void OnResolveConflicts ()
		{
			RunCommand (VersionControlCommands.ResolveConflicts, false, false);
		}

		[CommandUpdateHandler (VersionControlCommands.ResolveConflicts)]
		protected void UpdateResolveConflicts (CommandInfo item)
		{
			if (!(CurrentNode.DataItem is UnknownSolutionItem)) {
				item.Visible = false;
				return;
			}

			TestCommand (VersionControlCommands.ResolveConflicts, item, false);
		}

		private void TestCommand(VersionControlCommands cmd, CommandInfo item, bool projRecurse = true)
		{
			TestResult res = RunCommand(cmd, true, projRecurse);
			if (res == TestResult.NoVersionControl && cmd == VersionControlCommands.Log) {
				// Use the update command to show the "not available" message
				item.Icon = null;
				item.Enabled = false;
				if (VersionControlService.IsGloballyDisabled)
					item.Text = GettextCatalog.GetString ("Version Control support is disabled");
				else
					item.Text = GettextCatalog.GetString ("This project or folder is not under version control");
			} else
				item.Visible = res == TestResult.Enable;
		}
		
		private TestResult RunCommand (VersionControlCommands cmd, bool test, bool projRecurse = true)
		{
			List<VersionControlItem> items = GetItems (projRecurse);

			foreach (VersionControlItem it in items) {
				if (it.Repository == null) {
					if (cmd != VersionControlCommands.Publish)
						return TestResult.NoVersionControl;
				} else if (it.Repository.VersionControlSystem != null && !it.Repository.VersionControlSystem.IsInstalled) {
					return TestResult.Disable;
				}
			}

			bool res = false;
			
			try {
				switch (cmd) {
				case VersionControlCommands.Update:
					res = UpdateCommand.Update (items, test);
					break;
				case VersionControlCommands.Diff:
					res = DiffCommand.Show (items, test);
					break;
				case VersionControlCommands.Log:
					res = LogCommand.Show (items, test);
					break;
				case VersionControlCommands.Status:
					res = StatusView.Show (items, test, false);
					break;
				case VersionControlCommands.Add:
					res = AddCommand.Add (items, test);
					break;
				case VersionControlCommands.Remove:
					res = RemoveCommand.Remove (items, test);
					break;
				case VersionControlCommands.Revert:
					res = RevertCommand.Revert (items, test);
					break;
				case VersionControlCommands.Lock:
					res = LockCommand.Lock (items, test);
					break;
				case VersionControlCommands.Unlock:
					res = UnlockCommand.Unlock (items, test);
					break;
				case VersionControlCommands.Publish:
					VersionControlItem it = items [0];
					if (items.Count == 1 && it.IsDirectory && it.WorkspaceObject != null)
						res = PublishCommand.Publish (it.WorkspaceObject, it.Path, test);
					break;
				case VersionControlCommands.Annotate:
					res = BlameCommand.Show (items, test);
					break;
				case VersionControlCommands.CreatePatch:
					res = CreatePatchCommand.CreatePatch (items, test);
					break;
				case VersionControlCommands.Ignore:
					res = IgnoreCommand.Ignore (items, test);
					break;
				case VersionControlCommands.Unignore:
					res = UnignoreCommand.Unignore (items, test);
					break;
				case VersionControlCommands.ResolveConflicts:
					res = ResolveConflictsCommand.ResolveConflicts (items, test);
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
