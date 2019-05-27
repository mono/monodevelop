//
// GitCommitDialogExtensionWidget.cs
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
using MonoDevelop.Core;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl.Git
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class GitCommitDialogExtensionWidget : Gtk.Bin
	{
		public GitCommitDialogExtensionWidget (GitRepository repo)
		{
			this.Build ();

			repo.GetCurrentRemoteAsync ().ContinueWith (t => {
				if (IsDestroyed)
					return;
				bool hasRemote = t.Result != null;
				if (!hasRemote) {
					checkPush.Sensitive = false;
					checkPush.TooltipText = GettextCatalog.GetString ("Pushing is only available for repositories with configured remotes.");
				}

			}, Runtime.MainTaskScheduler);
		}

		public bool PushAfterCommit {
			get { return checkPush.Active; }
		}

		public bool CommitterIsAuthor {
			get { return !checkAuthor.Active; }
		}

		public string AuthorName {
			get { return entryName.Text; }
		}

		public string AuthorMail {
			get { return entryEmail.Text; }
		}

		protected void OnCheckAuthorToggled (object sender, EventArgs e)
		{
			authorBox.Visible = checkAuthor.Active;
			OnChanged (sender, e);
		}

		void OnChanged (object sender, EventArgs e)
		{
			Changed?.Invoke (this, EventArgs.Empty);
		}

		public event EventHandler Changed;

		bool IsDestroyed { get; set; }

		protected override void OnDestroyed ()
		{
			IsDestroyed = true;
			base.OnDestroyed ();
		}
	}
}
