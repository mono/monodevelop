//
// IWorkbench.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Threading.Tasks;
using MonoDevelop.Components.DockNotebook;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.Ide.Gui.Shell
{
	[DefaultServiceImplementation (typeof (DefaultWorkbench))]
	internal interface IShell
	{
		IWorkbenchWindow ActiveWorkbenchWindow { get; }

		event EventHandler ActiveWorkbenchWindowChanged;

		Task<IWorkbenchWindow> ShowView (DocumentController controller, IShellNotebook notebook, object viewCommandHandler);
		void CloseView (IWorkbenchWindow window, bool animate);

		void Present ();
	
		event EventHandler<WindowReorderedEventArgs> WindowReordered;
		event EventHandler<NotebookEventArgs> NotebookClosed;
	}

	internal class WindowReorderedEventArgs: EventArgs
	{
		public DockNotebookTab OldPosition { get; set; }
		public DockNotebookTab NewPosition { get; set; }
	}

	internal class NotebookEventArgs : EventArgs
	{
		public IShellNotebook Notebook { get; set; }
	}
}
