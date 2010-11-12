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

namespace MonoDevelop.Ide.Gui
{
	public interface IWorkbenchWindow
	{
		IViewContent ViewContent { get; }
		IBaseViewContent ActiveViewContent { get; set; }

		IEnumerable<IAttachableViewContent> SubViewContents { get; }

		Document Document { get; set; }
		string DocumentType { get; set; }
		string Title { get; set; }
		bool ShowNotification { get; set; }

		void AttachViewContent (IAttachableViewContent subViewContent);
		void SwitchView (int index);
		void SwitchView (IAttachableViewContent subViewContent);
		int FindView (Type viewType);
		
		bool CloseWindow (bool force, bool fromMenu, int pageNum);
		void SelectWindow ();
		
		event EventHandler DocumentChanged;
		event WorkbenchWindowEventHandler Closed;
		event WorkbenchWindowEventHandler Closing;
		event ActiveViewContentEventHandler ActiveViewContentChanged;
		
	}

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
		private IBaseViewContent content = null;
		public IBaseViewContent Content {
			get { return content; }
		}

		public ActiveViewContentEventArgs (IBaseViewContent content)
		{
			this.content = content;
		}
	}
}
