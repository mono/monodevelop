// 
// StashManagerDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl.Git
{
	public partial class StashManagerDialog : Gtk.Dialog
	{
		ListStore store;
		StashCollection stashes;
		
		public StashManagerDialog (GitRepository repo)
		{
			this.Build ();
			stashes = repo.GetStashes ();
			
			store = new ListStore (typeof(Stash), typeof(string), typeof(string));
			list.Model = store;
			
			list.AppendColumn (GettextCatalog.GetString ("Date/Time"), new CellRendererText (), "text", 1);
			list.AppendColumn (GettextCatalog.GetString ("Comment"), new CellRendererText (), "text", 2);
			Fill ();
			TreeIter it;
			if (store.GetIterFirst (out it))
				list.Selection.SelectIter (it);
			UpdateButtons ();
			
			list.Selection.Changed += delegate {
				UpdateButtons ();
			};
		}
		
		void Fill ()
		{
			TreeViewState tvs = new TreeViewState (list, 0);
			tvs.Save ();
			store.Clear ();
			foreach (var s in stashes) {
				string name = s.Comment;
				string branch = GitRepository.GetStashBranchName (name);
				if (branch != null) {
					if (branch == "_tmp_")
						name = GettextCatalog.GetString ("Temporary stash created by {0}", BrandingService.ApplicationName);
					else
						name = GettextCatalog.GetString ("Local changes of branch '{0}'", branch);
				}
				store.AppendValues (s, s.DateTime.LocalDateTime.ToString (), name);
			}
			tvs.Load ();
		}
		
		void UpdateButtons ()
		{
			vboxButtons.Sensitive = GetSelected () != null;
		}
		
		Stash GetSelected ()
		{
			TreeIter it;
			if (!list.Selection.GetSelected (out it))
				return null;
			return (Stash) store.GetValue (it, 0);
		}

		protected void OnButtonApplyClicked (object sender, System.EventArgs e)
		{
			Stash s = GetSelected ();
			if (s != null) {
				GitService.ApplyStash (s);
				Respond (ResponseType.Ok);
			}
		}
	
		protected void OnButtonBranchClicked (object sender, System.EventArgs e)
		{
		}
	
		protected void OnButtonDeleteClicked (object sender, System.EventArgs e)
		{
			Stash s = GetSelected ();
			if (s != null) {
				stashes.Remove (s);
				Fill ();
				UpdateButtons ();
			}
		}

		protected void OnButtonApplyRemoveClicked (object sender, System.EventArgs e)
		{
			Stash s = GetSelected ();
			if (s != null) {
				using (IdeApp.Workspace.GetFileStatusTracker ()) {
					GitService.ApplyStash (s).Completed += delegate(IAsyncOperation op) {
						if (op.Success)
							stashes.Remove (s);
					};
				}
				Respond (ResponseType.Ok);
			}
		}
	}
}

