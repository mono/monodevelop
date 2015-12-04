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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.ProgressMonitoring;
using System.Threading;
using LibGit2Sharp;

namespace MonoDevelop.VersionControl.Git
{
	public static class GitService
	{
		public static bool UseRebaseOptionWhenPulling
		{
			get { return PropertyService.Get ("MonoDevelop.VersionControl.Git.UseRebaseOptionWhenPulling", true); }
			set { PropertyService.Set ("MonoDevelop.VersionControl.Git.UseRebaseOptionWhenPulling", value); }
		}

		public static bool StashUnstashWhenUpdating
		{
			get { return PropertyService.Get ("MonoDevelop.VersionControl.Git.StashUnstashWhenUpdating", true); }
			set { PropertyService.Set ("MonoDevelop.VersionControl.Git.StashUnstashWhenUpdating", value); }
		}

		public static bool StashUnstashWhenSwitchingBranches
		{
			get { return PropertyService.Get ("MonoDevelop.VersionControl.Git.StashUnstashWhenSwitchingBranches", true); }
			set { PropertyService.Set ("MonoDevelop.VersionControl.Git.StashUnstashWhenSwitchingBranches", value); }
		}

		public static void Push (GitRepository repo)
		{
			var dlg = new PushDialog (repo);
			try {
				if (MessageService.RunCustomDialog (dlg) != (int) Gtk.ResponseType.Ok)
					return;

				string remote = dlg.SelectedRemote;
				string branch = dlg.SelectedRemoteBranch ?? repo.GetCurrentBranch ();

				IProgressMonitor monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Pushing changes..."), VersionControlOperationType.Push);
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
						using (IProgressMonitor monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Rebasing branch '{0}'...", dlg.SelectedBranch))) {
							if (dlg.IsRemote)
								repo.Fetch (monitor, dlg.RemoteName);
							repo.Rebase (dlg.SelectedBranch, dlg.StageChanges ? GitUpdateOptions.SaveLocalChanges : GitUpdateOptions.None, monitor);
						}
					} else {
						using (IProgressMonitor monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Merging branch '{0}'...", dlg.SelectedBranch))) {
							if (dlg.IsRemote)
								repo.Fetch (monitor, dlg.RemoteName);
							repo.Merge (dlg.SelectedBranch, dlg.StageChanges ? GitUpdateOptions.SaveLocalChanges : GitUpdateOptions.None, monitor);
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

		public static void SwitchToBranch (GitRepository repo, string branch)
		{
			var monitor = new MessageDialogProgressMonitor (true, false, false, true);
			try {
				IdeApp.Workbench.AutoReloadDocuments = true;
				IdeApp.Workbench.LockGui ();
				ThreadPool.QueueUserWorkItem (delegate {
					try {
						repo.SwitchToBranch (monitor, branch);
					} catch (Exception ex) {
						monitor.ReportError ("Branch switch failed", ex);
					} finally {
						monitor.Dispose ();
					}
				});
				monitor.AsyncOperation.WaitForCompleted ();
			} finally {
				IdeApp.Workbench.AutoReloadDocuments = false;
				IdeApp.Workbench.UnlockGui ();
			}
		}

		public static IAsyncOperation ApplyStash (GitRepository repo, int s)
		{
			var monitor = new MessageDialogProgressMonitor (true, false, false, true);
			var statusTracker = IdeApp.Workspace.GetFileStatusTracker ();
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					ReportStashResult (repo.ApplyStash (monitor, s));
				} catch (Exception ex) {
					string msg = GettextCatalog.GetString ("Stash operation failed.");
					monitor.ReportError (msg, ex);
				}
				finally {
					monitor.Dispose ();
					statusTracker.Dispose ();
				}
			});
			return monitor.AsyncOperation;
		}

		public static void ReportStashResult (StashApplyStatus status)
		{
			if (status == StashApplyStatus.Conflicts) {
				string msg = GettextCatalog.GetString ("Stash applied with conflicts");
				DispatchService.GuiDispatch (delegate {
					IdeApp.Workbench.StatusBar.ShowWarning (msg);
				});
			}
			else {
				string msg = GettextCatalog.GetString ("Stash successfully applied");
				DispatchService.GuiDispatch (delegate {
					IdeApp.Workbench.StatusBar.ShowMessage (msg);
				});
			}
		}
	}
}
