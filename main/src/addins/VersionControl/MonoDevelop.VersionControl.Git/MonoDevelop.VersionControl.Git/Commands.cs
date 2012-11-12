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
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Ide.ProgressMonitoring;
using System.Threading;

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
				IWorkspaceObject wob = IdeApp.ProjectOperations.CurrentSelectedSolutionItem;
				if (wob == null)
					wob = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem;
				if (wob != null)
					return VersionControlService.GetRepository (wob) as GitRepository;
				else
					return null;
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Visible = Repository != null;
		}
	}
	
	class PushCommandHandler: GitCommandHandler
	{
		protected override void Run ()
		{
			GitService.Push (Repository);
		}
	}
	
	class SwitchToBranchHandler: GitCommandHandler
	{
		protected override void Run (object dataItem)
		{
			GitService.SwitchToBranch (Repository, (string)dataItem);
		}
		
		protected override void Update (CommandArrayInfo info)
		{
			var repo = Repository;
			if (repo == null)
				return;

			IWorkspaceObject wob = IdeApp.ProjectOperations.CurrentSelectedItem as IWorkspaceObject;
			if (wob == null)
				return;
			if (((wob is WorkspaceItem) && ((WorkspaceItem)wob).ParentWorkspace == null) ||
			    (wob.BaseDirectory.CanonicalPath == repo.RootPath.CanonicalPath))
			{
				string currentBranch = repo.GetCurrentBranch ();
				foreach (Branch branch in repo.GetBranches ()) {
					CommandInfo ci = info.Add (branch.Name, branch.Name);
					if (branch.Name == currentBranch)
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
			var stashes = Repository.GetStashes ();
			NewStashDialog dlg = new NewStashDialog ();
			if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
				string comment = dlg.Comment;
				MessageDialogProgressMonitor monitor = new MessageDialogProgressMonitor (true, false, false, true);
				var statusTracker = IdeApp.Workspace.GetFileStatusTracker ();
				ThreadPool.QueueUserWorkItem (delegate {
					try {
						using (var gm = new GitMonitor (monitor))
							stashes.Create (gm, comment);
					} catch (Exception ex) {
						MessageService.ShowException (ex);
					}
					finally {
						monitor.Dispose ();
						statusTracker.NotifyChanges ();
					}
				});
			}
			dlg.Destroy ();
		}
	}
	
	class StashPopHandler: GitCommandHandler
	{
		protected override void Run ()
		{
			var stashes = Repository.GetStashes ();
			MessageDialogProgressMonitor monitor = new MessageDialogProgressMonitor (true, false, false, true);
			var statusTracker = IdeApp.Workspace.GetFileStatusTracker ();
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					NGit.Api.MergeCommandResult result;
					using (var gm = new GitMonitor (monitor))
						result = stashes.Pop (gm);
					GitService.ReportStashResult (monitor, result);
				} catch (Exception ex) {
					MessageService.ShowException (ex);
				}
				finally {
					monitor.Dispose ();
					statusTracker.NotifyChanges ();
				}
			});
		}
		
		protected override void Update (CommandInfo info)
		{
			var repo = Repository;
			if (repo != null) {
				var s = repo.GetStashes ();
				info.Enabled = s.Any ();
			} else
				info.Visible = false;
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

