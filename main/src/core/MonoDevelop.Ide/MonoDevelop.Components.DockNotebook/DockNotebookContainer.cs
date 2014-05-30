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
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.DockNotebook
{
	class DockNotebookContainer : EventBox
	{
		bool isMasterTab;

		DockNotebook tabControl;

		List<DockNotebook> notebooks = new List<DockNotebook> ();

		public bool AllowLeftInsert {
			get {
				return IdeApp.Workbench.Splits.Count == 0;
			}
		}

		public bool AllowRightInsert {
			get {
				return IdeApp.Workbench.Splits.Count == 0;
			}
		}

		public DockNotebook TabControl {
			get {
				return tabControl;
			}
		}

		public DockNotebookContainer (DockNotebook tabControl, bool isMasterTab = false)
		{
			this.isMasterTab = isMasterTab;
			this.tabControl = tabControl;
			Child = tabControl;
			
			if (!isMasterTab)
				tabControl.PageRemoved += HandlePageRemoved;
		}

		public DockNotebookContainer MotherContainer ()
		{
			if (Parent == null)
				return null;

			var paned = Parent as Paned;
			return paned != null ? (DockNotebookContainer)paned.Parent : null;
		}

		public void SetSingleMode ()
		{
			var mother = MotherContainer ();

			if (mother == null)
				return;

			var paned = mother.Child as Paned;

			var container1 = paned.Child1 as DockNotebookContainer;
			var container2 = paned.Child2 as DockNotebookContainer;

			DockNotebook notebook1, notebook2;
			if (container1.isMasterTab) {
				notebook1 = container1.TabControl;
				notebook2 = container2.TabControl;
			} else {
				notebook1 = container2.TabControl;
				notebook2 = container1.TabControl;
			}
			var tabCount = notebook2.TabCount;

			for (var i = 0; i < tabCount; i++) {
				var tab = notebook2.GetTab (0);
				var window = (SdiWorkspaceWindow)tab.Content;
				notebook2.RemoveTab (0, false);

				var newTab = notebook1.InsertTab (-1);
				newTab.Content = window;
				window.SetDockNotebook (notebook1, newTab);
			}
		}

		public static void MoveToFloatingWindow (SdiWorkspaceWindow workspaceWindow)
		{
			MoveToFloatingWindow (workspaceWindow, 0, 0, 640, 480);
		}

		public static void MoveToFloatingWindow (SdiWorkspaceWindow workspaceWindow, int x, int y, int width, int height)
		{
			var window = new DockWindow ();
			var notebook = new SdiDragNotebook ((DefaultWorkbench)IdeApp.Workbench.RootWindow);

			notebook.NavigationButtonsVisible = false;

			window.Container = new DockNotebookContainer (notebook);
			notebook.InitSize ();

			var tab = notebook.InsertTab (-1);
			tab.Content = workspaceWindow;

			window.Title = DefaultWorkbench.GetTitle (workspaceWindow);

			workspaceWindow.SetDockNotebook (notebook, tab);

			window.ShowAll ();
			window.Move (x, y);
			window.Resize (width, height);
		}

		static void HandlePageRemoved (object sender, EventArgs e)
		{
			var control = (DockNotebook)sender;
			if (control.TabCount != 0)
				return;
			var controlContainer = control.Parent as DockNotebookContainer;
			if (controlContainer == null || controlContainer.Parent == null || controlContainer.isMasterTab)
				return;
			
			var paned = controlContainer.Parent as Paned;
			if (paned != null) {
				var otherContainer = (paned.Child1 == control.Parent ? paned.Child2 : paned.Child1) as DockNotebookContainer;
				if (otherContainer == null)
					return;
				
				var motherContainer = (DockNotebookContainer)paned.Parent;

				var newChild = otherContainer.Child;
				otherContainer.Remove (newChild);
				
				motherContainer.tabControl = otherContainer.tabControl;
				if (motherContainer.isMasterTab) {
					((DefaultWorkbench)IdeApp.Workbench.RootWindow).TabControl = (SdiDragNotebook)motherContainer.tabControl;
				}
				motherContainer.isMasterTab |= otherContainer.isMasterTab;
				motherContainer.Remove (paned);
				motherContainer.Child = newChild;
				motherContainer.ShowAll ();
				paned.Destroy ();
				return;
			}
			
			// window case.
			controlContainer.Parent.Destroy ();
		}

		void Insert(SdiWorkspaceWindow window, Func<DockNotebookContainer, Split> callback)
		{
			var newNotebook = new SdiDragNotebook ((DefaultWorkbench)IdeApp.Workbench.RootWindow);

			newNotebook.NavigationButtonsVisible = false;
			PlaceholderWindow.newNotebooks.Add (newNotebook);
			newNotebook.InitSize ();
			var newContainer = new DockNotebookContainer (newNotebook);
			newNotebook.Destroyed += delegate {
				PlaceholderWindow.newNotebooks.Remove (newNotebook);
			};
			newNotebook.PageRemoved += HandlePageRemoved;

			var newTab = newNotebook.InsertTab (-1);
			newTab.Content = window;
			window.SetDockNotebook (newNotebook, newTab);
			Remove (Child);

			var split = callback (newContainer);

			newNotebook.Destroyed += delegate(object sender, EventArgs e) {
				IdeApp.Workbench.Splits.Remove (split);
			};

			tabControl.InitSize ();
			ShowAll ();
		}

		public void InsertLeft (SdiWorkspaceWindow window)
		{
			Insert (window, container => {
				var box = new HPaned ();
				var new_container = new DockNotebookContainer (tabControl);

				box.Add1 (container);
				box.Add2 (new_container);
				box.Position = Allocation.Width / 2;
				Child = box;

				return AddSplit (container, new_container);
			});
		}

		public void InsertRight (SdiWorkspaceWindow window)
		{
			Insert (window, container => {
				var box = new HPaned ();
				var new_container = new DockNotebookContainer (tabControl);

				box.Add1 (new_container);
				box.Add2 (container);
				box.Position = Allocation.Width / 2;
				Child = box;

				return AddSplit (new_container, container);
			});
		}

		private Split AddSplit (DockNotebookContainer container1, DockNotebookContainer container2)
		{
			var split = new Split ();

			split.Notebook1 = container1;
			split.Notebook2 = container2;

			IdeApp.Workbench.Splits.Add (split);

			return split;
		}
	}
}

