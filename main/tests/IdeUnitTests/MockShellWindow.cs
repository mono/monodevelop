//
// MockShellWindow.cs
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;

namespace IdeUnitTests
{
	public class MockShellWindow: IWorkbenchWindow
	{
		EventHandler<NotebookChangeEventArgs> notebookChanged;

		public MockShellWindow (MockShell shell, DocumentController controller, MockShellNotebook notebook)
		{
			Shell = shell;
			Controller = controller;
			Notebook = notebook;
		}

		public MockShell Shell { get; }
		public DocumentController Controller { get; }
		public MockShellNotebook Notebook { get; }
		public MockShellDocumentView RootView { get; set; }

		public event EventHandler CloseRequested;

		event EventHandler<NotebookChangeEventArgs> IWorkbenchWindow.NotebookChanged {
			add { notebookChanged += value; }
			remove { notebookChanged -= value; }
		}

		public Document Document { get; set; }

		public string Title => Controller.DocumentTitle;

		public bool ShowNotification { get; set; }

		IShellNotebook IWorkbenchWindow.Notebook => Notebook;

		public void SimulateClose ()
		{
			CloseRequested?.Invoke (this, EventArgs.Empty);
		}

		public void SelectWindow ()
		{
			if (Notebook != null)
				Notebook.ActiveWindow = this;
			Shell.ActiveWorkbenchWindow = this;
		}

		public bool ContentVisible => Notebook.ActiveWindow == this;

		IShellDocumentViewContent IWorkbenchWindow.CreateViewContent ()
		{
			return new MockShellDocumentViewContent ();
		}

		IShellDocumentViewContainer IWorkbenchWindow.CreateViewContainer ()
		{
			return new MockShellDocumentViewContainer ();
		}

		void IWorkbenchWindow.SetRootView (IShellDocumentViewItem view)
		{
			RootView = (MockShellDocumentView)view;
		}

		public async Task Show ()
		{
			if (RootView != null)
				await RootView.Show ();
		}
	}
}
