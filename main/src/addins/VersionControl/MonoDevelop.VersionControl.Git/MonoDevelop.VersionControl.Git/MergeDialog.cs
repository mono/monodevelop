//
// MergeDialog.cs
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
using LibGit2Sharp;

namespace MonoDevelop.VersionControl.Git
{
	partial class MergeDialog : Gtk.Dialog
	{
		readonly TreeStore store;
		readonly GitRepository repo;
		string currentSel;
		string currentType;
		string currentRemote;
		readonly bool rebasing;

		public MergeDialog (GitRepository repo, bool rebasing)
		{
			this.Build ();

			this.UseNativeContextMenus ();

			this.repo = repo;
			this.rebasing = rebasing;

			store = new TreeStore (typeof(string), typeof(Xwt.Drawing.Image), typeof (string), typeof(string));
			tree.Model = store;
			tree.SearchColumn = -1; // disable the interactive search

			var crp = new CellRendererImage ();
			var col = new TreeViewColumn ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "image", 1);
			var crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", 2);
			tree.AppendColumn (col);

			tree.Selection.Changed += HandleTreeSelectionChanged;

			if (rebasing) {
				labelHeader.Text = GettextCatalog.GetString ("Select the branch to which to rebase:");
				checkStage.Label = GettextCatalog.GetString ("Stash/unstash local changes before/after rebasing");
				buttonOk.Label = GettextCatalog.GetString ("Rebase");
			}

			checkStage.Active = true;

			Fill ();
		}

		public string SelectedBranch {
			get { return currentSel; }
		}

		public string RemoteName {
			get { return currentRemote; }
		}

		public bool StageChanges {
			get { return checkStage.Active; }
		}

		public bool IsRemote {
			get { return currentType == "remote"; }
		}

		void HandleTreeSelectionChanged (object sender, EventArgs e)
		{
			TreeIter it;
			if (tree.Selection.GetSelected (out it)) {
				currentSel = (string) store.GetValue (it, 0);
				currentType = (string) store.GetValue (it, 3);
				if (IsRemote) {
					TreeIter it2;
					store.IterParent (out it2, it);
					currentRemote = (string)store.GetValue (it2, 2);
				} else {
					currentRemote = null;
				}
			}
			else
				currentSel = null;
			UpdateStatus ();
		}

		void Fill ()
		{
			store.Clear ();

			foreach (Branch b in repo.GetBranches ())
				store.AppendValues (b.FriendlyName, ImageService.GetIcon ("vc-branch", IconSize.Menu), b.FriendlyName, "branch");

			foreach (string t in repo.GetTags ())
				store.AppendValues (t, ImageService.GetIcon ("vc-tag", IconSize.Menu), t, "tag");

			foreach (Remote r in repo.GetRemotes ()) {
				TreeIter it = store.AppendValues (null, ImageService.GetIcon ("vc-repository", IconSize.Menu), r.Name, null);
				foreach (string b in repo.GetRemoteBranches (r.Name))
					store.AppendValues (it, r.Name + "/" + b, ImageService.GetIcon ("vc-branch", IconSize.Menu), b, "remote");
			}
			UpdateStatus ();
		}

		void UpdateStatus ()
		{
			if (currentSel != null) {
				string cb = repo.GetCurrentBranch ();
				string txt = null;
				if (rebasing) {
					switch (currentType) {
					case "branch": txt = GettextCatalog.GetString ("The branch <b>{1}</b> will be rebased to the branch <b>{0}</b>.", currentSel, cb); break;
					case "tag": txt = GettextCatalog.GetString ("The branch <b>{1}</b> will be rebased to the tag <b>{0}</b>.", currentSel, cb); break;
					case "remote": txt = GettextCatalog.GetString ("The branch <b>{1}</b> will be rebased to the remote branch <b>{0}</b>.", currentSel, cb); break;
					}
				}
				else {
					switch (currentType) {
					case "branch": txt = GettextCatalog.GetString ("The branch <b>{0}</b> will be merged into the branch <b>{1}</b>.", currentSel, cb); break;
					case "tag": txt = GettextCatalog.GetString ("The tag <b>{0}</b> will be merged into the branch <b>{1}</b>.", currentSel, cb); break;
					case "remote": txt = GettextCatalog.GetString ("The remote branch <b>{0}</b> will be merged into the branch <b>{1}</b>.", currentSel, cb); break;
					}
				}
				labelOper.Visible = true;
				labelOper.Markup = txt;
				buttonOk.Sensitive = true;
			} else {
				labelOper.Visible = false;
				buttonOk.Sensitive = false;
			}
		}
	}
}
