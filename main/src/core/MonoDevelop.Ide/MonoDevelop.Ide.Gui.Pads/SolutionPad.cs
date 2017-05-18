//
// SolutionPad.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads
{
	public class SolutionPad : TreeViewPad
	{
		public SolutionPad ()
		{
			IdeApp.Workspace.WorkspaceItemOpened += OnOpenWorkspace;
			IdeApp.Workspace.WorkspaceItemClosed += OnCloseWorkspace;
		}
		
		public override void Initialize (NodeBuilder[] builders, TreePadOption[] options, string contextMenuPath)
		{
			base.Initialize (builders, options, contextMenuPath);
			foreach (WorkspaceItem it in IdeApp.Workspace.Items)
				treeView.AddChild (it);
			base.TreeView.Tree.Name = "solutionBrowserTree";

			base.TreeView.Tree.SetCommonAccessibilityAttributes ("SolutionBrowserTree",
			                                                     GettextCatalog.GetString ("Solution"),
			                                                     GettextCatalog.GetString ("Explore the current solution's files and structure"));
		}
		
		protected virtual void OnOpenWorkspace (object sender, WorkspaceItemEventArgs e)
		{
			treeView.AddChild (e.Item);
		}

		protected virtual void OnCloseWorkspace (object sender, WorkspaceItemEventArgs e)
		{
			treeView.RemoveChild (e.Item);
		}
	}
}
