// 
// EditBranchDialog.cs
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl.Git
{
	public partial class EditBranchDialog : Gtk.Dialog
	{
		ListStore comboStore;
		string currentTracking;
		string oldName;
		GitRepository repo;
		
		public EditBranchDialog (GitRepository repo, Branch branch, bool isNew)
		{
			this.Build ();
			this.repo =  repo;
			
			comboStore = new ListStore (typeof(string), typeof(Gdk.Pixbuf), typeof (string));
			comboSources.Model = comboStore;
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			comboSources.PackStart (crp, false);
			comboSources.AddAttribute (crp, "pixbuf", 1);
			CellRendererText crt = new CellRendererText ();
			comboSources.PackStart (crt, true);
			comboSources.AddAttribute (crt, "text", 2);
			
			if (branch != null) {
				if (!isNew)
					oldName = branch.Name;
				currentTracking = branch.Tracking;
				entryName.Text = branch.Name;
				if (currentTracking != null)
					checkTrack.Active = true;
			}
			
			foreach (Branch b in repo.GetBranches ()) {
				AddValues (b.Name, ImageService.GetPixbuf ("vc-git-branch", IconSize.Menu));
			}
			
			foreach (string t in repo.GetTags ())
				AddValues (t, ImageService.GetPixbuf ("vc-git-tag", IconSize.Menu));
			
			foreach (RemoteSource r in repo.GetRemotes ()) {
				foreach (string b in repo.GetRemoteBranches (r.Name))
					AddValues (r.Name + "/" + b, ImageService.GetPixbuf ("md-web-search-icon", IconSize.Menu));
			}
				
			UpdateStatus ();
		}
		
		void AddValues (string name, Gdk.Pixbuf icon)
		{
			TreeIter it = comboStore.AppendValues (name, icon, name);
			if (name == currentTracking)
				comboSources.SetActiveIter (it);
		}
		
		public string TrackSource {
			get {
				if (checkTrack.Active) {
					TreeIter it;
					if (comboSources.GetActiveIter (out it))
						return (string) comboStore.GetValue (it, 0);
				}
				return null;
			}
		}
		
		public string BranchName {
			get { return entryName.Text; }
		}
		
		void UpdateStatus ()
		{
			comboSources.Sensitive = checkTrack.Active;
			buttonOk.Sensitive = entryName.Text.Length > 0;
			if (oldName != entryName.Text && repo.GetBranches ().Any (b => b.Name == entryName.Text)) {
				labelError.Markup = "<span color='red'>" + GettextCatalog.GetString ("A branch with this name already exists") + "</span>";
				labelError.Show ();
				buttonOk.Sensitive = false;
			} else if (!IsValidBranchName (entryName.Text)) {
				labelError.Markup = "<span color='red'>" + GettextCatalog.GetString (@"A branch name can not:
Start with '.' or end with '/' or '.lock'
Contain a ' ', '..', '~', '^', ':', '\', '?', '['") + "</span>";
				labelError.Show ();
				buttonOk.Sensitive = false;
			} else
				labelError.Hide ();
		}

		static bool IsValidBranchName (string name)
		{
			// List from: https://github.com/git/git/blob/master/refs.c#L21
			if (name.StartsWith (".", System.StringComparison.Ordinal) ||
				name.EndsWith ("/", System.StringComparison.Ordinal) ||
				name.EndsWith (".lock", System.StringComparison.Ordinal))
				return false;

			if (name.Contains (" ") || name.Contains ("~") || name.Contains ("..") || name.Contains ("^") || name.Contains (":") || name.Contains ("\\") || name.Contains ("?") || name.Contains ("["))
				return false;
			return true;
		}

		protected virtual void OnCheckTrackToggled (object sender, System.EventArgs e)
		{
			UpdateStatus ();
		}
		
		protected virtual void OnEntryNameChanged (object sender, System.EventArgs e)
		{
			UpdateStatus ();
		}
	}
}

