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

namespace MonoDevelop.VersionControl.Git
{
	public static class GitService
	{
		public static void Push (GitRepository repo)
		{
			var dlg = new PushDialog (repo);
			try {
				if (MessageService.RunCustomDialog (dlg) != (int) Gtk.ResponseType.Ok)
					return;
				
				string remote = dlg.SelectedRemote;
				string branch = dlg.SelectedRemoteBranch;
				
				IProgressMonitor monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Pushing changes..."));
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
	
		public static void ShowMergeDialog (GitRepository repo)
		{
			MergeDialog dlg = new MergeDialog (repo);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
					dlg.Hide ();
					using (IProgressMonitor monitor = VersionControlService.GetProgressMonitor (GettextCatalog.GetString ("Merging branch '{0}'...", dlg.SelectedBranch))) {
						repo.Merge (dlg.SelectedBranch, monitor);
					}
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		public static void SwitchToBranch (GitRepository repo, string branch)
		{
			IdeApp.Workbench.AutoReloadDocuments = true;
			try {
				repo.SwitchToBranch (branch);
			} finally {
				IdeApp.Workbench.AutoReloadDocuments = false;
			}
		}
	}
}

