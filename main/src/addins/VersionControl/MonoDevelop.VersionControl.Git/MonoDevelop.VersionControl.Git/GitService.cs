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
using System.Text;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.ProgressMonitoring;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace MonoDevelop.VersionControl.Git
{
	public static class GitService
	{
		public static ConfigurationProperty<bool> UseRebaseOptionWhenPulling = ConfigurationProperty.Create ("MonoDevelop.VersionControl.Git.UseRebaseOptionWhenPulling", true);
		public static ConfigurationProperty<bool> StashUnstashWhenUpdating = ConfigurationProperty.Create ("MonoDevelop.VersionControl.Git.StashUnstashWhenUpdating", false);
		public static ConfigurationProperty<bool> StashUnstashWhenSwitchingBranches = ConfigurationProperty.Create ("MonoDevelop.VersionControl.Git.StashUnstashWhenSwitchingBranches", false);

		public static void Push (GitRepository repo)
		{
			bool hasCommits = false;
			using (var RootRepository = new LibGit2Sharp.Repository (repo.RootPath))
				hasCommits = RootRepository.Commits.Any ();
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
				Task.Run (async () => {
					try {
						await repo.PushAsync (monitor, remote, branch);
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

		public static void ShowConfigurationDialog (VersionControlSystem vcs, string repoPath, string repoUrl)
		{
			using (var dlg = new GitConfigurationDialog (vcs, repoPath, repoUrl))
				MessageService.ShowCustomDialog (dlg);
		}

		public static void ShowMergeDialog (GitRepository repo, bool rebasing)
		{
			var dlg = new MergeDialog (repo, rebasing);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
					var selectedBranch = dlg.SelectedBranch;
					var isRemote = dlg.IsRemote;
					var remoteName = dlg.RemoteName;
					var stageChanges = dlg.StageChanges;
					dlg.Hide ();

					Task.Run ((Func<Task>)(async () => {
						try {
							if (rebasing) {
								using (var monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Rebasing branch '{0}'...", selectedBranch))) {
									if (isRemote)
										await repo.FetchAsync ((ProgressMonitor)monitor, (string)remoteName);
									await repo.RebaseAsync (selectedBranch, stageChanges ? GitUpdateOptions.SaveLocalChanges : GitUpdateOptions.None, monitor);
								}
							} else {
								using (var monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Merging branch '{0}'...", selectedBranch))) {
									if (isRemote)
										await repo.FetchAsync ((ProgressMonitor)monitor, (string)remoteName);
									await repo.MergeAsync (selectedBranch, stageChanges ? GitUpdateOptions.SaveLocalChanges : GitUpdateOptions.None, monitor, FastForwardStrategy.NoFastForward);
								}
							}
						} catch (Exception e) {
							LoggingService.LogError ("Error while showing merge dialog.", e);
						}
					}));
				}
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}

		public static async Task<bool> SwitchToBranchAsync (GitRepository repo, string branch)
		{
			var monitor = new MessageDialogProgressMonitor (true, false, false, true);
			try {
				IdeApp.Workbench.AutoReloadDocuments = true;
				IdeApp.Workbench.LockGui ();
				try {
					return await repo.SwitchToBranchAsync (monitor, branch);
				} catch (Exception ex) {
					monitor.ReportError (GettextCatalog.GetString ("Branch switch failed"), ex);
					return false;
				} finally {
					monitor.Dispose ();
				}
			} finally {
				IdeApp.Workbench.AutoReloadDocuments = false;
				IdeApp.Workbench.UnlockGui ();
			}
		}

		public static Task<bool> ApplyStash (GitRepository repo, int s)
		{
			var monitor = new MessageDialogProgressMonitor (true, false, false, true);
			var t = Task.Run (delegate {
				try {
					var res = repo.ApplyStash (monitor, s);
					ReportStashResult (repo, res, null);
					return res == StashApplyStatus.Applied;
				} catch (Exception ex) {
					string msg = GettextCatalog.GetString ("Stash operation failed.");
					monitor.ReportError (msg, ex);
					return false;
				}
				finally {
					monitor.Dispose ();
				}
			});
			return t;
		}

		public static void ReportStashResult (Repository repo, StashApplyStatus status, int? stashCount)
		{
			string msg;
			StashResultType stashResultType;

			switch (status) {
			case StashApplyStatus.Conflicts:
				bool stashApplied = false;
				StringBuilder info = new StringBuilder (GettextCatalog.GetString ("A conflicting change has been detected in the index. "));
				// Include conflicts in the msg
				if (stashCount != null && repo is GitRepository gitRepo) {
					int actualStashCount = gitRepo.GetStashes ().Count ();
					stashApplied = actualStashCount != stashCount;
					if (stashApplied) {
						info.AppendLine (GettextCatalog.GetString ("The following conflicts have been found:"));
						foreach (var conflictFile in gitRepo.RootRepository.Index.Conflicts) {
							info.AppendLine (conflictFile.Ancestor.Path);
						}
					} else
						info.Append (GettextCatalog.GetString ("Stash not applied."));
				}
				msg = info.ToString ();
				stashResultType = !stashApplied ? StashResultType.Error : StashResultType.Warning;
				break;
			case StashApplyStatus.UncommittedChanges:
				msg = GettextCatalog.GetString ("The stash application was aborted due to uncommitted changes in the index.");
				stashResultType = StashResultType.Warning;
				break;
			case StashApplyStatus.NotFound:
				msg = GettextCatalog.GetString ("The stash index given was not found.");
				stashResultType = StashResultType.Error;
				break;
			default:
				msg = GettextCatalog.GetString ("Stash successfully applied.");
				stashResultType = StashResultType.Message;
				break;
			}

			ShowStashResult (msg, stashResultType);
		}

		enum StashResultType
		{
			Error,
			Message,
			Warning
		}

		static void ShowStashResult (string msg, StashResultType stashResultType)
		{
			Runtime.RunInMainThread (delegate {
				switch (stashResultType)
				{
					case StashResultType.Error:
						IdeApp.Workbench.StatusBar.ShowError (msg);
						MessageService.ShowError (msg);
						break;
					case StashResultType.Message:
						IdeApp.Workbench.StatusBar.ShowMessage (msg);
						break;
					case StashResultType.Warning:
						IdeApp.Workbench.StatusBar.ShowWarning (msg);
						MessageService.ShowWarning (msg);
						break;
				}
			});
		}
	}
}
