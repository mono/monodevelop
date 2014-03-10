//
// DockNotebookContainer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.DockNotebook
{
	class DockNotebookContainer : EventBox
	{
		readonly DockNotebook tabControl;

		public DockNotebook TabControl {
			get {
				return tabControl;
			}
		}

		public DockNotebookContainer (DockNotebook tabControl)
		{
			this.tabControl = tabControl;
			Child = tabControl;
		}

		void Insert(SdiWorkspaceWindow window, Action<DockNotebookContainer> callback)
		{
			var newNotebook = new SdiDragNotebook ((DefaultWorkbench)IdeApp.Workbench.RootWindow);
			PlaceholderWindow.newNotebooks.Add (newNotebook);
			newNotebook.InitSize ();
			newNotebook.Destroyed += delegate {
				PlaceholderWindow.newNotebooks.Remove (newNotebook);
			};
			newNotebook.PageRemoved += delegate {
				if (newNotebook.TabCount == 0) {
					((Container)tabControl.Parent).Remove (tabControl);
					Child.Destroy ();
					Child = tabControl;
					ShowAll ();
				}
			};

			var newTab = newNotebook.InsertTab (-1); 
			newTab.Content = window;
			window.SetDockNotebook (newNotebook, newTab); 
			Remove (Child); 
			callback (new DockNotebookContainer (newNotebook));

			tabControl.InitSize ();
			ShowAll ();

		}

		public void InsertLeft (SdiWorkspaceWindow window)
		{
			Insert (window, container => {
				var box = new HPaned ();
				box.Add1 (container); 
				box.Add2 (tabControl); 
				box.Position = Allocation.Width / 2;
				Child = box;
			}); 
		}

		public void InsertRight (SdiWorkspaceWindow window)
		{
			Insert (window, container => {
				var box = new HPaned ();
				box.Add1 (tabControl); 
				box.Add2 (container); 
				box.Position = Allocation.Width / 2;
				Child = box;
			}); 
		}

		public void InsertTop (SdiWorkspaceWindow window)
		{
			Insert (window, container => {
				var box = new VPaned ();
				box.Add1 (container); 
				box.Add2 (tabControl); 
				box.Position = Allocation.Height / 2;
				Child = box;
			}); 
		}

		public void InsertBottom (SdiWorkspaceWindow window)
		{
			Insert (window, container => {
				var box = new VPaned ();
				box.Add1 (tabControl); 
				box.Add2 (container); 
				box.Position = Allocation.Height / 2;
				Child = box;
			}); 
		}
	}
}

