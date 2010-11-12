
using System;
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
		Add,
		Remove,
		Publish,
		Checkout,
		Repository,
		Commit,
		Revert,
		Lock,
		Unlock,
		Annotate,
		ShowAnnotations,
		HideAnnotations,
		CreatePatch
	}
	
	class SolutionVersionControlCommandHalder: CommandHandler
	{
		VersionControlItemList GetItems ()
		{
			VersionControlItemList list = new VersionControlItemList ();
			
			IWorkspaceObject wob;
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
			
			list.Add (new VersionControlItem (repo, wob, wob.BaseDirectory, true));
			return list;
		}
		
		protected override void Run ()
		{
			VersionControlItemList items = GetItems ();
			RunCommand (items, false);
		}
		
		protected override void Update (CommandInfo info)
		{
			VersionControlItemList items = GetItems ();
			info.Enabled = items.Count > 0 && RunCommand (items, true);
		}
		
		protected virtual bool RunCommand (VersionControlItemList items, bool test)
		{
			return true;
		}
	}
	
	class FileVersionControlCommandHalder: CommandHandler
	{
		protected VersionControlItemList GetItems ()
		{
			VersionControlItemList list = new VersionControlItemList ();
			VersionControlItem it = GetItem ();
			if (it != null)
				list.Add (it);
			return list;
		}
		
		protected VersionControlItem GetItem ()
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || !doc.IsFile)
				return null;
			
			Project project = doc.Project ?? IdeApp.ProjectOperations.CurrentSelectedProject;
			
			Repository repo = VersionControlService.GetRepository (project);
			if (repo == null || repo.VersionControlSystem == null || !repo.VersionControlSystem.IsInstalled)
				return null;
			
			return new VersionControlItem (repo, project, doc.FileName, false);
		}
		
		protected override void Run ()
		{
			VersionControlItemList items = GetItems ();
			RunCommand (items, false);
		}
		
		protected override void Update (CommandInfo info)
		{
			VersionControlItemList items = GetItems ();
			info.Enabled = items.Count > 0 && RunCommand (items, true);
		}
		
		protected virtual bool RunCommand (VersionControlItemList items, bool test)
		{
			return true;
		}
	}	

	class UpdateCommandHandler: SolutionVersionControlCommandHalder
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return UpdateCommand.Update (items, test);
		}
	}
	
	class StatusCommandHandler: SolutionVersionControlCommandHalder
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return StatusView.Show (items, test);
		}
	}
	
	class CommitCommandHandler: SolutionVersionControlCommandHalder
	{
		protected override bool RunCommand (VersionControlItemList items, bool test)
		{
			return CommitCommand.Commit (items, test);
		}
	}
	
	class AddCommandHandler: FileVersionControlCommandHalder
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
	
	class RemoveCommandHandler: FileVersionControlCommandHalder
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
	
	class RevertCommandHandler: FileVersionControlCommandHalder
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
	
	class LockCommandHandler: FileVersionControlCommandHalder
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
	
	class UnlockCommandHandler: FileVersionControlCommandHalder
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

	class CurrentFileDiffHandler : FileVersionControlCommandHalder
	{
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			DiffView.AttachViewContents (doc, GetItem ());
			doc.Window.SwitchView (doc.Window.FindView (typeof (DiffView)));
		}
	}
	
	class CurrentFileBlameHandler : FileVersionControlCommandHalder
	{
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			DiffView.AttachViewContents (doc, GetItem ());
			doc.Window.SwitchView (doc.Window.FindView (typeof (BlameView)));
		}
	}
	
	class CurrentFileLogHandler : FileVersionControlCommandHalder
	{
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			DiffView.AttachViewContents (doc, GetItem ());
			doc.Window.SwitchView (doc.Window.FindView (typeof (LogView)));
		}
	}
}
