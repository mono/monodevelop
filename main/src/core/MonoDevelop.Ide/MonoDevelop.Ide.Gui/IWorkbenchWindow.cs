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
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Components.DockNotebook;
using MonoDevelop.Ide.Gui.Shell;

namespace MonoDevelop.Ide.Gui
{
	interface IWorkbenchWindow
	{
		Document Document { get; set; }
		string Title { get; }
		bool ShowNotification { get; set; }
		IShellNotebook Notebook { get; }
		bool ContentVisible { get; }

		void SelectWindow ();

		IShellDocumentViewContent CreateViewContent ();
		IShellDocumentViewContainer CreateViewContainer ();

		void SetRootView (IShellDocumentViewItem view);

		event EventHandler CloseRequested;
		event EventHandler<NotebookChangeEventArgs> NotebookChanged;
	}

	internal class NotebookChangeEventArgs : EventArgs
	{
		public IShellNotebook OldNotebook { get; set; }
		public IShellNotebook NewNotebook { get; set; }
	}
}
