// VersionControlCommandHandler.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using System;

namespace MonoDevelop.VersionControl
{
	public class VersionControlCommandHandler : NodeCommandHandler 
	{
		VersionControlItemList items;
		
		public void Init (VersionControlItemList items)
		{
			this.items = items;
		}
		
		protected override bool MultipleSelectedNodes {
			get {
				if (items != null)
					return items.Count > 1;
				else
					return base.MultipleSelectedNodes;
			}
		}
		
		public VersionControlItemList GetItems (bool projRecurse = true)
		{
			// Cached items are used only in the status view, not in the project pad.
			if (items != null)
				return items;

			// Don't cache node items because they can change
			VersionControlItemList nodeItems = new VersionControlItemList ();
			foreach (ITreeNavigator node in CurrentNodes) {
				VersionControlItem item = CreateItem (node.DataItem, projRecurse);
				if (item != null)
					nodeItems.Add (item);
			}
			return nodeItems;
		}
		
		public static VersionControlItem CreateItem (object obj, bool projRecurse = true)
		{
			string path;
			bool isDir;
			IWorkspaceObject pentry;
			VersionInfo versionInfo = null;

			if (obj is ProjectFile) {
				ProjectFile file = (ProjectFile)obj;
				path = file.FilePath;
				isDir = false;
				pentry = file.Project;
			} else if (obj is SystemFile) {
				SystemFile file = (SystemFile)obj;
				path = file.Path;
				isDir = false;
				pentry = file.ParentWorkspaceObject;
			} else if (obj is ProjectFolder) {
				ProjectFolder f = (ProjectFolder)obj;
				path = f.Path;
				isDir = true;
				pentry = f.ParentWorkspaceObject;
			} else if (!projRecurse && obj is Solution) {
				Solution sol = (Solution)obj;
				path = sol.FileName;
				isDir = false;
				pentry = sol;
			} else if (!projRecurse && obj is Project) {
				Project proj = (Project)obj;
				path = proj.FileName;
				isDir = false;
				pentry = proj;
			} else if (!projRecurse && obj is UnknownSolutionItem) {
				UnknownSolutionItem item = (UnknownSolutionItem)obj;
				path = item.FileName;
				isDir = false;
				pentry = item;
			} else if (obj is IWorkspaceObject) {
				pentry = ((IWorkspaceObject)obj);
				path = pentry.BaseDirectory;
				isDir = true;
			} else
				return null;

			if (pentry == null)
				return null;

			return new VersionControlItem (VersionControlService.GetRepository (pentry), pentry, path, isDir, versionInfo);
		}
		
	}
}
