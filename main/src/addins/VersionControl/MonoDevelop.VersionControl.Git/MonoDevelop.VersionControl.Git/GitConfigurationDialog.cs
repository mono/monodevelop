// 
// GitConfigurationDialog.cs
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components;

namespace MonoDevelop.VersionControl.Git
{
	public partial class GitConfigurationDialog : Gtk.Dialog
	{
		GitRepository repo;
		ListStore storeBranches;
		TreeStore storeRemotes;
		
		public GitConfigurationDialog (GitRepository repo)
		{
			this.Build ();
			this.repo = repo;
			this.HasSeparator = false;
			
			// Branches list
			
			storeBranches = new ListStore (typeof(Branch), typeof(string), typeof(string), typeof(string));
			listBranches.Model = storeBranches;
			listBranches.HeadersVisible = true;
			
			listBranches.AppendColumn (GettextCatalog.GetString ("Branch"), new CellRendererText (), "markup", 1);
			listBranches.AppendColumn (GettextCatalog.GetString ("Tracking"), new CellRendererText (), "text", 2);
			
			// Sources tree
			
			storeRemotes = new TreeStore (typeof(RemoteSource), typeof(string), typeof(string), typeof(string), typeof(string));
			treeRemotes.Model = storeRemotes;
			treeRemotes.HeadersVisible = true;
			
			treeRemotes.AppendColumn ("Remote Source / Branch", new CellRendererText (), "markup", 1);
			treeRemotes.AppendColumn ("Url", new CellRendererText (), "text", 2);
			
			// Fill data
			
			FillBranches ();
			FillRemotes ();
		}
		
		void FillBranches ()
		{
			TreeViewState state = new TreeViewState (listBranches, 3);
			state.Save ();
			storeBranches.Clear ();
			string currentBranch = repo.GetCurrentBranch ();
			foreach (Branch branch in repo.GetBranches ()) {
				string text = branch.Name == currentBranch ? "<b>" + branch.Name + "</b>" : branch.Name;
				storeBranches.AppendValues (branch, text, branch.Tracking, branch.Name);
			}
			state.Load ();
		}
		
		void FillRemotes ()
		{
			TreeViewState state = new TreeViewState (treeRemotes, 4);
			state.Save ();
			storeRemotes.Clear ();
			string currentRemote = repo.GetCurrentRemote ();
			foreach (RemoteSource remote in repo.GetRemotes ()) {
				string text = remote.Name == currentRemote ? "<b>" + remote.Name + "</b>" : remote.Name;
				string url;
				if (remote.FetchUrl == remote.PushUrl)
					url = remote.FetchUrl;
				else
					url = remote.FetchUrl + " (fetch)\n" + remote.PushUrl + " (push)";
				TreeIter it = storeRemotes.AppendValues (remote, text, url, null, remote.Name);
				foreach (string branch in repo.GetRemoteBranches (remote.Name))
					storeRemotes.AppendValues (it, null, branch, null, branch, remote.Name + "/" + branch);
			}
			state.Load ();
		}
		
		protected virtual void OnButtonAddBranchClicked (object sender, System.EventArgs e)
		{
			var dlg = new EditBranchDialog (repo, null, true);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) ResponseType.Ok) {
					repo.CreateBranch (dlg.BranchName, dlg.TrackSource);
					FillBranches ();
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		protected virtual void OnButtonEditBranchClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (!listBranches.Selection.GetSelected (out it))
				return;
			Branch b = (Branch) storeBranches.GetValue (it, 0);
			var dlg = new EditBranchDialog (repo, b, false);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) ResponseType.Ok) {
					if (dlg.BranchName != b.Name)
						repo.RenameBranch (b.Name, dlg.BranchName);
					repo.SetBranchTrackSource (dlg.BranchName, dlg.TrackSource);
					FillBranches ();
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		protected virtual void OnButtonRemoveBranchClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (!listBranches.Selection.GetSelected (out it))
				return;
			Branch b = (Branch) storeBranches.GetValue (it, 0);
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the branch '{0}'?", b.Name), AlertButton.Delete)) {
				repo.RemoveBranch (b.Name);
				FillBranches ();
			}
		}
		
		protected virtual void OnButtonSetDefaultBranchClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (!listBranches.Selection.GetSelected (out it))
				return;
			Branch b = (Branch) storeBranches.GetValue (it, 0);
			GitService.SwitchToBranch (repo, b.Name);
			FillBranches ();
		}
		
		protected virtual void OnButtonAddRemoteClicked (object sender, System.EventArgs e)
		{
			var remote = new RemoteSource ();
			var dlg = new EditRemoteDialog (remote, true);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
					repo.AddRemote (remote, dlg.ImportTags);
					FillRemotes ();
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		protected virtual void OnButtonEditRemoteClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (!treeRemotes.Selection.GetSelected (out it))
				return;
			
			RemoteSource remote = (RemoteSource) storeRemotes.GetValue (it, 0);
			string oldName = remote.Name;
			
			var dlg = new EditRemoteDialog (remote, false);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
					if (remote.Name != oldName)
						repo.RenameRemote (oldName, remote.Name);
					repo.UpdateRemote (remote);
					FillRemotes ();
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		protected virtual void OnButtonRemoveRemoteClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (!treeRemotes.Selection.GetSelected (out it))
				return;
			RemoteSource remote = (RemoteSource) storeRemotes.GetValue (it, 0);
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the remote '{0}'?", remote.Name), AlertButton.Delete)) {
				repo.RemoveRemote (remote.Name);
				FillBranches ();
			}
		}
		
		void UpdateRemoteButtons ()
		{
			TreeIter it;
			if (!treeRemotes.Selection.GetSelected (out it)) {
				buttonAddRemote.Sensitive = buttonEditRemote.Sensitive = buttonRemoveRemote.Sensitive = buttonTrackRemote.Sensitive = false;
				return;
			}
			RemoteSource remote = (RemoteSource) storeRemotes.GetValue (it, 0);
			buttonTrackRemote.Sensitive = remote == null;
			buttonAddRemote.Sensitive = buttonEditRemote.Sensitive = buttonRemoveRemote.Sensitive = remote != null;
		}
		
		protected virtual void OnButtonTrackRemoteClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (!treeRemotes.Selection.GetSelected (out it))
				return;
			string branchName = (string) storeRemotes.GetValue (it, 3);
			if (branchName == null)
				return;
			
			storeRemotes.IterParent (out it, it);
			RemoteSource remote = (RemoteSource) storeRemotes.GetValue (it, 0);
			
			Branch b = new Branch ();
			b.Name = branchName;
			b.Tracking = remote.Name + "/" + branchName;
			
			var dlg = new EditBranchDialog (repo, b, true);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) ResponseType.Ok) {
					repo.CreateBranch (dlg.BranchName, dlg.TrackSource);
					FillBranches ();
				}
			} finally {
				dlg.Destroy ();
			}
		}
	}
}

