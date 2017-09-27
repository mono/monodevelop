//
// Command.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using System.Linq;
using MonoDevelop.Ide.ProgressMonitoring;
using System.Threading;
using LibGit2Sharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.Status;

namespace MonoDevelop.VersionControl.Git
{
	public enum Commands
	{
		Push,
		SwitchToBranch,
		ManageBranches,
		Merge,
		Rebase,
		Stash,
		StashPop,
		ManageStashes
	}

	class GitCommandHandler: CommandHandler
	{
		public GitRepository Repository {
			get {
				WorkspaceObject wob = IdeApp.ProjectOperations.CurrentSelectedSolutionItem;
				if (wob == null)
					wob = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem;
				if (wob != null)
					return VersionControlService.GetRepository (wob) as GitRepository;
				return null;
			}
		}

		protected GitRepository UpdateVisibility (CommandInfo info)
		{
			var repo = Repository;
			info.Visible = repo != null;
			return repo;
		}

		protected override void Update (CommandInfo info)
		{
			UpdateVisibility (info);
		}
	}

	class PushCommandHandler: GitCommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			var repo = UpdateVisibility (info);
			if (repo != null)
				info.Enabled = repo.GetCurrentRemote () != null;
		}

		protected override void Run ()
		{
			GitService.Push (Repository);
		}
	}

	class SwitchToBranchHandler: GitCommandHandler
	{
		protected async override void Run (object dataItem)
		{
			await GitService.SwitchToBranch (Repository, (string)dataItem).ConfigureAwait (false);
		}

		protected override void Update (CommandArrayInfo info)
		{
			var repo = Repository;
			if (repo == null)
				return;

			var wob = IdeApp.ProjectOperations.CurrentSelectedItem as WorkspaceObject;
			if (wob == null)
				return;
			if (((wob is WorkspaceItem) && ((WorkspaceItem)wob).ParentWorkspace == null) ||
			    (wob.BaseDirectory.CanonicalPath == repo.RootPath.CanonicalPath))
			{
				string currentBranch = repo.GetCurrentBranch ();
				foreach (Branch branch in repo.GetBranches ()) {
					CommandInfo ci = info.Add (branch.FriendlyName, branch.FriendlyName);
					if (branch.FriendlyName == currentBranch)
						ci.Checked = true;
				}
			}
		}
	}

	class ManageBranchesHandler: GitCommandHandler
	{
		protected override void Run ()
		{
			GitService.ShowConfigurationDialog (Repository);
		}
	}

	class MergeBranchHandler: GitCommandHandler
	{
		protected override void Run ()
		{
			GitService.ShowMergeDialog (Repository, false);
		}
	}

	class RebaseBranchHandler: GitCommandHandler
	{
		protected override void Run ()
		{
			GitService.ShowMergeDialog (Repository, true);
		}
	}

	class StashHandler: GitCommandHandler
	{
		protected override void Run ()
		{
			var dlg = new NewStashDialog ();
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
					string comment = dlg.Comment;
					var monitor = new MessageDialogProgressMonitor (true, false, false, true);
					var statusTracker = IdeApp.Workspace.GetFileStatusTracker ();
					ThreadPool.QueueUserWorkItem (delegate {
						try {
							Stash stash;
							if (Repository.TryCreateStash (monitor, comment, out stash)) {
								string msg;
								if (stash != null) {
									msg = GettextCatalog.GetString ("Changes successfully stashed");
								} else {
									msg = GettextCatalog.GetString ("No changes were available to stash");
								}

								Runtime.RunInMainThread (delegate {
									StatusService.MainContext.ShowMessage (msg);
								});
							}

						} catch (Exception ex) {
							MessageService.ShowError (GettextCatalog.GetString ("Stash operation failed"), ex);
						}
						finally {
							monitor.Dispose ();
							statusTracker.Dispose ();
						}
					});
				}
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}

		protected override void Update (CommandInfo info)
		{
			var repo = UpdateVisibility (info);
			if (repo != null)
				info.Enabled = !repo.RootRepository.Info.IsHeadUnborn;
		}
	}

	class StashPopHandler: GitCommandHandler
	{
		protected override void Run ()
		{
			var monitor = new MessageDialogProgressMonitor (true, false, false, true);
			var statusTracker = IdeApp.Workspace.GetFileStatusTracker ();
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					GitService.ReportStashResult (Repository.PopStash (monitor, 0));
				} catch (Exception ex) {
					MessageService.ShowError (GettextCatalog.GetString ("Stash operation failed"), ex);
				}
				finally {
					monitor.Dispose ();
					statusTracker.Dispose ();
				}
			});
		}

		protected override void Update (CommandInfo info)
		{
			var repo = UpdateVisibility (info);
			if (repo != null)
				info.Enabled = repo.GetStashes ().Any ();
		}
	}

	class ManageStashesHandler: GitCommandHandler
	{
		protected override void Run ()
		{
			GitService.ShowStashManager (Repository);
		}
	}
}
