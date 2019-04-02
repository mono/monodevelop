//
// VersionControlView.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Documents;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl.Views
{
	public class VersionControlDocumentInfo
	{
		bool alreadyStarted = false;

		public Document Document {
			get;
			set;
		}

		public VersionControlDocumentController VersionControlExtension {
			get;
			set;
		}

		public DocumentController Controller { get; }

		public VersionControlItem Item {
			get;
			set;
		}

		public Revision[] History {
			get;
			set;
		}

		public Repository Repository {
			get { return Item.Repository; }
			set { Item.Repository = value; }
		}

		public bool Started {
			get { return alreadyStarted; }
		}

		public VersionControlDocumentInfo (VersionControlDocumentController versionControlExtension, DocumentController controller, VersionControlItem item, Repository repository)
		{
			this.VersionControlExtension = versionControlExtension;
			Controller = controller;
			this.Item = item;
			item.Repository = repository;
		}

		public void Start (bool rerun = false)
		{
			if (!rerun && alreadyStarted)
				return;
			alreadyStarted = true;
			ThreadPool.QueueUserWorkItem (delegate {
				lock (updateLock) {
					try {
						History      = Item.Repository.GetHistory (Item.Path, null);
						Item.VersionInfo  = Item.Repository.GetVersionInfo (Item.Path, VersionInfoQueryFlags.IgnoreCache);
					} catch (Exception ex) {
						LoggingService.LogError ("Error retrieving history", ex);
					}

					Runtime.RunInMainThread (delegate {
						OnUpdated (EventArgs.Empty);
					});
					mre.Set ();
				}
			});
		}

		object updateLock = new object ();
		ManualResetEvent mre = new ManualResetEvent (false);

		// Runs an action in the GUI thread.
		public void RunAfterUpdate (Action act)
		{
			if (mre == null) {
				act ();
				return;
			}

			ThreadPool.QueueUserWorkItem (delegate {
				mre.WaitOne ();
				mre.Dispose ();
				mre = null;
				Runtime.RunInMainThread (delegate {
					act ();
				});
			});
		}


		public void RunAfterUpdate (Func<Task> act)
		{
			if (mre == null) {
				act ().Ignore ();
				return;
			}
			Task.Run (async delegate {
				mre.WaitOne ();
				mre.Dispose ();
				mre = null;
				await Runtime.RunInMainThread (act);
			}).Ignore ();
			return;
		}
		protected virtual void OnUpdated (EventArgs e)
		{
			Updated?.Invoke (this, e);
		}

		public event EventHandler Updated;

	}
}
