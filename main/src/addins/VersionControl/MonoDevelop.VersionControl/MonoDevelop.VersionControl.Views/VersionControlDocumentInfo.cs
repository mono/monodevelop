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
using System.Linq;
using System.IO;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using Mono.Addins;

namespace MonoDevelop.VersionControl.Views
{
	public class VersionControlDocumentInfo
	{
		bool alreadyStarted = false;
		
		public DocumentView Document {
			get;
			set;
		}

		public VersionControlItem Item {
			get;
			set;
		}

		public Revision[] History {
			get;
			set;
		}
		
		public VersionInfo VersionInfo {
			get;
			set;
		}
		
		public Repository Repository {
			get;
			set;
		}
		
		public bool Started {
			get { return alreadyStarted; }
		}

		public VersionControlDocumentInfo (DocumentView document, VersionControlItem item, Repository repository)
		{
			this.Document = document;
			this.Item = item;
			this.Repository = repository;
		}

		public void Start ()
		{
			if (alreadyStarted)
				return;
			alreadyStarted = true;
			ThreadPool.QueueUserWorkItem (delegate {
				lock (updateLock) {
					try {
						History      = Item.Repository.GetHistory (Item.Path, null);
						VersionInfo  = Item.Repository.GetVersionInfo (Item.Path);
					} catch (Exception ex) {
						LoggingService.LogError ("Error retrieving history", ex);
					}
					
					DispatchService.GuiDispatch (delegate {
						OnUpdated (EventArgs.Empty);
					});
					isUpdated = true;
				}
			});
		}
		
		object updateLock = new object ();
		bool isUpdated = false;
		
		public void RunAfterUpdate (Action act) 
		{
			if (isUpdated) {
				act ();
				return;
			}
			while (!isUpdated)
				Thread.Sleep (10);
			act ();
		}
		
		protected virtual void OnUpdated (EventArgs e)
		{
			EventHandler handler = this.Updated;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler Updated;

	}
}
