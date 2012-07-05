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
using NGit.Api;
using System.Threading;
using MonoDevelop.Core.ProgressMonitoring;

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
				string branch = dlg.SelectedRemoteBranch;
				
				IProgressMonitor monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Pushing changes..."), VersionControlOperationType.Push);
				System.Threading.ThreadPool.QueueUserWorkItem (delegate {
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
			}
		}
	
		public static void ShowConfigurationDialog (GitRepository repo)
		{
			var dlg = new GitConfigurationDialog (repo);
			MessageService.ShowCustomDialog (dlg);
		}
	
		public static void ShowMergeDialog (GitRepository repo, bool rebasing)
		{
			MergeDialog dlg = new MergeDialog (repo, rebasing);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
					dlg.Hide ();
					using (IProgressMonitor monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Merging branch '{0}'...", dlg.SelectedBranch))) {
						repo.Merge (dlg.SelectedBranch, dlg.StageChanges, monitor);
					}
				}
			} finally {
				dlg.Destroy ();
			}
		}
	
		public static void ShowStashManager (GitRepository repo)
		{
			StashManagerDialog dlg = new StashManagerDialog (repo);
			MessageService.RunCustomDialog (dlg);
			dlg.Destroy ();
		}
		
		public static void SwitchToBranch (GitRepository repo, string branch)
		{
			MessageDialogProgressMonitor monitor = new MessageDialogProgressMonitor (true, false, false, true);
			try {
				IdeApp.Workbench.AutoReloadDocuments = true;
				IdeApp.Workbench.LockGui ();
				System.Threading.ThreadPool.QueueUserWorkItem (delegate {
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
		
		public static IAsyncOperation ApplyStash (Stash s)
		{
			MessageDialogProgressMonitor monitor = new MessageDialogProgressMonitor (true, false, false, true);
			var statusTracker = IdeApp.Workspace.GetFileStatusTracker ();
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					NGit.Api.MergeCommandResult result;
					using (var gm = new GitMonitor (monitor))
						result = s.Apply (gm);
					ReportStashResult (monitor, result);
				} catch (Exception ex) {
					string msg = GettextCatalog.GetString ("Stash operation failed.");
					monitor.ReportError (msg, ex);
				}
				finally {
					monitor.Dispose ();
					statusTracker.NotifyChanges ();
				}
			});
			return monitor.AsyncOperation;
		}
		
		public static void ReportStashResult (IProgressMonitor monitor, MergeCommandResult result)
		{
			if (result.GetMergeStatus () == NGit.Api.MergeStatus.FAILED) {
				string msg = GettextCatalog.GetString ("Stash operation failed.");
				DispatchService.GuiDispatch (delegate {
					IdeApp.Workbench.StatusBar.ShowWarning (msg);
				});
				string txt = msg + "\n\n" + GetMergeResultErrorDetail (result);
				monitor.ReportError (txt, null);
			}
			else if (result.GetMergeStatus () == NGit.Api.MergeStatus.NOT_SUPPORTED) {
				string msg = GettextCatalog.GetString ("Operation not supported");
				monitor.ReportError (msg, null);
				DispatchService.GuiDispatch (delegate {
					IdeApp.Workbench.StatusBar.ShowWarning (msg);
				});
			}
			else if (result.GetMergeStatus () == NGit.Api.MergeStatus.CONFLICTING) {
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
		
		internal static string GetMergeResultErrorDetail (MergeCommandResult result)
		{
			string msg = "";
			if (result.GetFailingPaths () != null) {
				foreach (var f in result.GetFailingPaths ()) {
					if (msg.Length > 0)
						msg += "\n";
					switch (f.Value) {
					case NGit.Merge.ResolveMerger.MergeFailureReason.DIRTY_WORKTREE: msg += GettextCatalog.GetString ("The file '{0}' has unstaged changes", f.Key); break;
					case NGit.Merge.ResolveMerger.MergeFailureReason.DIRTY_INDEX: msg += GettextCatalog.GetString ("The file '{0}' has staged changes", f.Key); break;
					case NGit.Merge.ResolveMerger.MergeFailureReason.COULD_NOT_DELETE: msg += GettextCatalog.GetString ("The file '{0}' could not be deleted", f.Key); break;
					}
				}
			}
			return msg;
		}
	}
}

