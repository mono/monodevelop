//
// MockShell.cs
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
using MonoDevelop.Components.DockNotebook;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;

namespace IdeUnitTests
{
	public class MockShell: IShell
	{
		List<MockShellWindow> windows = new List<MockShellWindow> ();

		MockShellWindow activeWindow;
		MockShellNotebook mainNotebook = new MockShellNotebook ();

		EventHandler<WindowReorderedEventArgs> windowReordered;
		EventHandler<NotebookEventArgs> notebookClosed;

		public MockShell ()
		{
		}

		public List<MockShellWindow> Windows {
			get {
				return windows;
			}
		}

		event EventHandler<WindowReorderedEventArgs> IShell.WindowReordered {
			add { windowReordered += value; }
			remove { windowReordered -= value; }
		}

		event EventHandler<NotebookEventArgs> IShell.NotebookClosed {
			add { notebookClosed += value; }
			remove { notebookClosed -= value; }
		}

		public MockShellWindow ActiveWorkbenchWindow {
			get {
				return activeWindow;
			} set {
				if (activeWindow != value) {
					activeWindow = value;
					OnActiveWindowChanged ();
				}
			}
		}

		IWorkbenchWindow IShell.ActiveWorkbenchWindow => activeWindow;

		public event EventHandler ActiveWorkbenchWindowChanged;

		public void Close ()
		{
			foreach (var v in windows.ToArray ())
				CloseView (v);
		}

		public void CloseView (MockShellWindow window)
		{
			int i = windows.IndexOf (window);
			if (i != -1) {
				windows.RemoveAt (i);
				if (activeWindow == window) {
					if (windows.Count == 0) {
						activeWindow = null;
					} else {
						if (i >= windows.Count)
							i--;
						activeWindow = (MockShellWindow)windows [i];
					}
					OnActiveWindowChanged ();
				}
			}
		}

		public event EventHandler PresentCalled;

		void OnActiveWindowChanged ()
		{
			ActiveWorkbenchWindowChanged?.Invoke (this, EventArgs.Empty);
		}

		Task<IWorkbenchWindow> IShell.ShowView (DocumentController controller, IShellNotebook notebook, object viewCommandHandler)
		{
			var nb = ((MockShellNotebook)notebook) ?? mainNotebook;
			var view = new MockShellWindow (this, controller, nb);
			windows.Add (view);
			if (nb.ActiveWindow == null)
				nb.ActiveWindow = view;
			return Task.FromResult<IWorkbenchWindow> (view);
		}

		void IShell.CloseView (IWorkbenchWindow window, bool animate)
		{
			CloseView ((MockShellWindow)window);
		}

		public void Present ()
		{
			PresentCalled?.Invoke (this, EventArgs.Empty);
		}

		public Task ShowDocument (Document document)
		{
			foreach (var w in windows)
				if (w.Document == document)
					return w.Show ();
			return Task.CompletedTask;
		}
	}
}
