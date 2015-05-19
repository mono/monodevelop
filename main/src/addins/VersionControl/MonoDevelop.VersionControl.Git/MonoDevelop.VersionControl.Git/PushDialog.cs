//
// PushDialog.cs
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Git
{
	partial class PushDialog : Gtk.Dialog
	{
		readonly GitRepository repo;

		public PushDialog (GitRepository repo)
		{
			this.Build ();
			this.repo = repo;
			HasSeparator = false;

			changeList.DiffLoader = DiffLoader;

			var list = new List<string> (repo.GetRemotes ().Select (r => r.Name));
			foreach (string s in list)
				remoteCombo.AppendText (s);
			remoteCombo.Active = list.IndexOf (repo.GetCurrentRemote ());

			UpdateChangeSet ();
		}

		public string SelectedRemote {
			get { return remoteCombo.ActiveText; }
		}

		public string SelectedRemoteBranch {
			get { return branchCombo.ActiveText; }
		}

		void UpdateRemoteBranches ()
		{
			((Gtk.ListStore)branchCombo.Model).Clear ();
			if (remoteCombo.Active == -1) {
				branchCombo.Sensitive = false;
				return;
			}
			branchCombo.Sensitive = true;
			var list = new List<string> (repo.GetRemoteBranches (remoteCombo.ActiveText));
			foreach (string s in list)
				branchCombo.AppendText (s);
			branchCombo.Active = list.IndexOf (repo.GetCurrentBranch ());
		}

		void UpdateChangeSet ()
		{
			if (remoteCombo.Active == -1 || branchCombo.Active == -1) {
				changeList.Clear ();
				return;
			}
			ChangeSet changeSet = repo.GetPushChangeSet (remoteCombo.ActiveText, branchCombo.ActiveText);
			changeList.Load (changeSet);
		}

		DiffInfo[] DiffLoader (FilePath path)
		{
			return repo.GetPushDiff (remoteCombo.ActiveText, branchCombo.ActiveText);
		}

		protected virtual void OnRemoteComboChanged (object sender, System.EventArgs e)
		{
			UpdateRemoteBranches ();
		}

		protected virtual void OnBranchComboChanged (object sender, System.EventArgs e)
		{
			UpdateChangeSet ();
		}
	}
}
