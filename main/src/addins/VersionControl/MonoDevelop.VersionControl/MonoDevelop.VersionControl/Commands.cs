
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.Views;

namespace MonoDevelop.VersionControl
{
	public enum Commands {
		Update,
		Diff,
		Log,
		Status,
		SolutionStatus,
		Add,
		Remove,
		Publish,
		Checkout,
		Repository,
		Revert,
		Lock,
		Unlock,
		Annotate,
		ShowAnnotations,
		HideAnnotations,
		CreatePatch,
		Ignore,
		Unignore,
		ResolveConflicts
	}
	
	class SolutionVersionControlCommandHandler: CommandHandler
	{
		static VersionControlItemList GetItems ()
		{
			VersionControlItemList list = new VersionControlItemList ();
			
			WorkspaceObject wob;
			Repository repo = null;
			wob = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem;
			if (wob != null)
				repo = VersionControlService.GetRepository (wob);
			if (repo == null) {
				wob = IdeApp.ProjectOperations.CurrentSelectedSolutionItem;
				if (wob != null)
					repo = VersionControlService.GetRepository (wob);
			}
			if (repo == null || repo.VersionControlSystem == null || !repo.VersionControlSystem.IsInstalled)
				return list;

			list.Add (new VersionControlItem (repo, wob, wob.BaseDirectory, true, null));
			return list;
		}
		
		protected override void Run ()
		{
			VersionControlItemList items = GetItems ();
			RunCommand (items, false);
		}
		
		protected override void Update (CommandInfo info)
		{
			if (VersionControlService.IsGloballyDisabled) {
				info.Visible = false;
				return;
			}

			VersionControlItemList items = GetItems ();
			info.Enabled = items.Count > 0 && RunCommand (items, true);
		}
		
		protected virtual bool RunCommand (VersionControlItemList items, bool test)
		{
			return true;
		}
	}
	
	class FileVersionControlCommandHandler: CommandHandler
	{
		protected static VersionControlItemList GetItems ()
		{
			VersionControlItemList list = new VersionControlItemList ();
			VersionControlItem it = GetItem ();

			if (it != null)
				list.Add (it);
			return list;
		}
		
		protected static VersionControlItem GetItem ()
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || !doc.IsFile)
				return null;
			
			Project project = doc.Project ?? IdeApp.ProjectOperations.CurrentSelectedProject;
			if (project == null)
				return null;
			
			Repository repo = VersionControlService.GetRepository (project);
			if (repo == null || repo.VersionControlSystem == null || !repo.VersionControlSystem.IsInstalled)
				return null;
			
			return new VersionControlItem (repo, project, doc.FileName, false, null);
		}
		
		protected sealed override void Run ()
		{
			VersionControlItemList items = GetItems ();
			RunCommand (items, false);
		}
		
		protected override void Update (CommandInfo info)
		{
			if (VersionControlService.IsGloballyDisabled) {
				info.Visible = false;
				return;
			}

			VersionControlItemList items = GetItems ();
			info.Enabled = items.Count > 0 && RunCommand (items, true);
		}
		
		protected virtual bool RunCommand (VersionControlItemList items, bool test)
		{
			return true;
		}
	}	

	class UpdateCommandHandler: SolutionVersionControlCommandHandler
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return UpdateCommand.Update (items, test);
		}
	}
	
	class StatusCommandHandler: SolutionVersionControlCommandHandler
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return StatusView.Show (items, test, true);
		}
	}

	class AddCommandHandler: FileVersionControlCommandHandler
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return AddCommand.Add (items, test);
		}
		
		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Text = GettextCatalog.GetString ("Add File");
		}
	}
	
	class RemoveCommandHandler: FileVersionControlCommandHandler
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return RemoveCommand.Remove (items, test);
		}
		
		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Text = GettextCatalog.GetString ("Remove File");
		}
	}
	
	class RevertCommandHandler: FileVersionControlCommandHandler
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return RevertCommand.Revert (items, test);
		}
		
		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Text = GettextCatalog.GetString ("Revert File");
		}
	}
	
	class LockCommandHandler: FileVersionControlCommandHandler
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return LockCommand.Lock (items, test);
		}
		
		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Text = GettextCatalog.GetString ("Lock File");
		}
	}
	
	class UnlockCommandHandler: FileVersionControlCommandHandler
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return UnlockCommand.Unlock (items, test);
		}
		
		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Text = GettextCatalog.GetString ("Unlock File");
		}
	}

	class IgnoreCommandHandler : FileVersionControlCommandHandler
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return IgnoreCommand.Ignore (items, test);
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Text = GettextCatalog.GetString ("Add to ignore list");
		}
	}

	class UnignoreCommandHandler : FileVersionControlCommandHandler
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return UnignoreCommand.Unignore (items, test);
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Text = GettextCatalog.GetString ("Remove from ignore list");
		}
	}

	class CurrentFileViewHandler<T> : FileVersionControlCommandHandler
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			if (test)
				return true;

			var window = IdeApp.Workbench.ActiveDocument.Window;
			window.SwitchView (window.FindView<T> ());
			return true;
		}
	}

	class CurrentFileDiffHandler : CurrentFileViewHandler<IDiffView>
	{
	}
	
	class CurrentFileBlameHandler : CurrentFileViewHandler<IBlameView>
	{
	}
	
	class CurrentFileLogHandler : CurrentFileViewHandler<ILogView>
	{
	}
}
