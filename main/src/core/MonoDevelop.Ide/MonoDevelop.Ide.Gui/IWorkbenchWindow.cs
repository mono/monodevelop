// IWorkbenchWindow.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
//
//

using System;
using System.ComponentModel;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Components.Docking;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Gui
{
	public interface IWorkbenchWindow
	{
		ViewContent ViewContent { get; }
		BaseViewContent ActiveViewContent { get; set; }

		IEnumerable<BaseViewContent> SubViewContents { get; }

		Document Document { get; set; }
		string DocumentType { get; set; }
		string Title { get; set; }
		bool ShowNotification { get; set; }
		ExtensionContext ExtensionContext { get; }

		void AttachViewContent (BaseViewContent subViewContent);
		void InsertViewContent (int index, BaseViewContent subViewContent);

		void SwitchView (int index);
		void SwitchView (BaseViewContent subViewContent);
		int FindView <T>();
		
		Task<bool> CloseWindow (bool force);
		void SelectWindow ();

		/// <summary>
		/// Returns a toolbar for the pad.
		/// </summary>
		DocumentToolbar GetToolbar (BaseViewContent targetView);

		event EventHandler DocumentChanged;
		event WorkbenchWindowEventHandler Closed;
		event WorkbenchWindowAsyncEventHandler Closing;
		event ActiveViewContentEventHandler ActiveViewContentChanged;
		event EventHandler ViewsChanged;
	}

	public delegate Task WorkbenchWindowAsyncEventHandler (object o, WorkbenchWindowEventArgs e);
	public delegate void WorkbenchWindowEventHandler (object o, WorkbenchWindowEventArgs e);
	public class WorkbenchWindowEventArgs : CancelEventArgs
	{
		private bool forced;
		public bool Forced {
			get { return forced; }
		}

		private bool wasActive;
		public bool WasActive {
			get { return wasActive; }
		}

		public WorkbenchWindowEventArgs (bool forced, bool wasActive)
		{
			this.forced = forced;
			this.wasActive = wasActive;
		}
	}

	public delegate void ActiveViewContentEventHandler (object o, ActiveViewContentEventArgs e);
	public class ActiveViewContentEventArgs : EventArgs
	{
		private BaseViewContent content = null;
		public BaseViewContent Content {
			get { return content; }
		}

		public ActiveViewContentEventArgs (BaseViewContent content)
		{
			this.content = content;
		}
	}
}
