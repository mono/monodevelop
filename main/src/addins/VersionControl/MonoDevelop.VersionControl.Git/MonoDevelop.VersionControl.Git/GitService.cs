//
// GitService.cs
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.ProgressMonitoring;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using MonoDevelop.Ide.Status;

namespace MonoDevelop.VersionControl.Git
{
	public static class GitService
	{
		public static ConfigurationProperty<bool> UseRebaseOptionWhenPulling = ConfigurationProperty.Create ("MonoDevelop.VersionControl.Git.UseRebaseOptionWhenPulling", true);
		public static ConfigurationProperty<bool> StashUnstashWhenUpdating = ConfigurationProperty.Create ("MonoDevelop.VersionControl.Git.StashUnstashWhenUpdating", true);
		public static ConfigurationProperty<bool> StashUnstashWhenSwitchingBranches = ConfigurationProperty.Create ("MonoDevelop.VersionControl.Git.StashUnstashWhenSwitchingBranches", true);

		public static void Push (GitRepository repo)
		{
			bool hasCommits = repo.RootRepository.Commits.Any ();
			if (!hasCommits) {
				MessageService.ShowMessage (
					GettextCatalog.GetString ("There are no changes to push."),
					GettextCatalog.GetString ("Create an initial commit first.")
				);
				return;
			}

			var dlg = new PushDialog (repo);
			try {
				if (MessageService.RunCustomDialog (dlg) != (int) Gtk.ResponseType.Ok)
					return;

				string remote = dlg.SelectedRemote;
				string branch = dlg.SelectedRemoteBranch ?? repo.GetCurrentBranch ();

				ProgressMonitor monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Pushing changes..."), VersionControlOperationType.Push);
				ThreadPool.QueueUserWorkItem (delegate {
					try {
						repo.Push (monitor, remote, branch);
					} catch (Exception ex) {
						monitor.ReportError (ex.Message, ex);
					} finally {
						monitor.Dispose ();
					}
				});
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}

		public static void ShowConfigurationDialog (GitRepository repo)
		{
			using (var dlg = new GitConfigurationDialog (repo))
				MessageService.ShowCustomDialog (dlg);
		}

		public static void ShowMergeDialog (GitRepository repo, bool rebasing)
		{
			var dlg = new MergeDialog (repo, rebasing);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
					dlg.Hide ();
					if (rebasing) {
						using (ProgressMonitor monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Rebasing branch '{0}'...", dlg.SelectedBranch))) {
							if (dlg.IsRemote)
								repo.Fetch (monitor, dlg.RemoteName);
							repo.Rebase (dlg.SelectedBranch, dlg.StageChanges ? GitUpdateOptions.SaveLocalChanges : GitUpdateOptions.None, monitor);
						}
					} else {
						using (ProgressMonitor monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Merging branch '{0}'...", dlg.SelectedBranch))) {
							if (dlg.IsRemote)
								repo.Fetch (monitor, dlg.RemoteName);
							repo.Merge (dlg.SelectedBranch, dlg.StageChanges ? GitUpdateOptions.SaveLocalChanges : GitUpdateOptions.None, monitor, FastForwardStrategy.NoFastForward);
						}
					}
				}
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}

		public static void ShowStashManager (GitRepository repo)
		{
			using (var dlg = new StashManagerDialog (repo))
				MessageService.ShowCustomDialog (dlg);
		}

		public async static Task<bool> SwitchToBranch (GitRepository repo, string branch)
		{
			var monitor = new MessageDialogProgressMonitor (true, false, false, true);
			try {
				IdeApp.Workbench.AutoReloadDocuments = true;
				IdeApp.Workbench.LockGui ();
				var t = await Task.Run (delegate {
					try {
						return repo.SwitchToBranch (monitor, branch);
					} catch (Exception ex) {
						monitor.ReportError (GettextCatalog.GetString ("Branch switch failed"), ex);
						return false;
					} finally {
						monitor.Dispose ();
					}
				});
				return t;
			} finally {
				IdeApp.Workbench.AutoReloadDocuments = false;
				IdeApp.Workbench.UnlockGui ();
			}
		}

		public static Task<bool> ApplyStash (GitRepository repo, int s)
		{
			var monitor = new MessageDialogProgressMonitor (true, false, false, true);
			var statusTracker = IdeApp.Workspace.GetFileStatusTracker ();
			var t = Task.Run (delegate {
				try {
					var res = repo.ApplyStash (monitor, s);
					ReportStashResult (res);
					return true;
				} catch (Exception ex) {
					string msg = GettextCatalog.GetString ("Stash operation failed.");
					monitor.ReportError (msg, ex);
					return false;
				}
				finally {
					monitor.Dispose ();
					statusTracker.Dispose ();
				}
			});
			return t;
		}

		public static void ReportStashResult (StashApplyStatus status)
		{
			if (status == StashApplyStatus.Conflicts) {
				string msg = GettextCatalog.GetString ("Stash applied with conflicts");
				Runtime.RunInMainThread (delegate {
					StatusService.MainContext.ShowWarning (msg);
				});
			}
			else {
				string msg = GettextCatalog.GetString ("Stash successfully applied");
				Runtime.RunInMainThread (delegate {
					StatusService.MainContext.ShowMessage (msg);
				});
			}
		}
	}
}
