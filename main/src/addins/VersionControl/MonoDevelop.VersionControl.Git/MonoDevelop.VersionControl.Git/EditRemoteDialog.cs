//
// EditRemoteDialog.cs
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
using LibGit2Sharp;


namespace MonoDevelop.VersionControl.Git
{
	partial class EditRemoteDialog : Gtk.Dialog
	{
		// TODO: Add user possibility to choose refspecs.
		public EditRemoteDialog () : this (null)
		{
		}

		public EditRemoteDialog (Remote remote)
		{
			this.Build ();
			if (remote != null) {
				entryName.Text = remote.Name;
				entryUrl.Text = remote.Url ?? "";
				entryPushUrl.Text = remote.PushUrl ?? "";
			}
			checkImportTags.Visible = remote == null;
			UpdateButtons ();
		}

		public string RemoteName {
			get { return entryName.Text; }
		}

		public string RemoteUrl {
			get { return entryUrl.Text; }
		}

		public string RemotePushUrl {
			get { return entryPushUrl.Text; }
		}

		public bool ImportTags {
			get { return checkImportTags.Active; }
		}

		void UpdateButtons ()
		{
			buttonOk.Sensitive = entryName.Text.Length > 0 && entryUrl.Text.Length > 0;
		}

		protected virtual void OnEntryNameChanged (object sender, System.EventArgs e)
		{
			UpdateButtons ();
		}

		protected virtual void OnEntryUrlChanged (object sender, System.EventArgs e)
		{
			UpdateButtons ();
		}
	}
}
