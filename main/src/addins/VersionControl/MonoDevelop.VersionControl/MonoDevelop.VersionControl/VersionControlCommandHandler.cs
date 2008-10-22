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

using System;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;

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
		
		public VersionControlItemList GetItems ()
		{
			// Cached items are used only in the status view, not in the project pad.
			if (items != null)
				return items;

			// Don't cache node items because they can change
			VersionControlItemList nodeItems = new VersionControlItemList ();
			foreach (ITreeNavigator node in CurrentNodes)
				nodeItems.Add (CreateItem (node.DataItem));
			return nodeItems;
		}
		
		public VersionControlItem CreateItem (object obj)
		{
			string path;
			bool isDir;
			IWorkspaceObject pentry;
			Repository repo;
			
			if (obj is ProjectFile) {
				ProjectFile file = (ProjectFile)obj;
				path = file.FilePath;
				isDir = false;
				pentry = file.Project;
			} else if (obj is SystemFile) {
				SystemFile file = (SystemFile)obj;
				path = file.Path;
				isDir = false;
				pentry = file.Project;
			} else if (obj is ProjectFolder) {
				ProjectFolder f = ((ProjectFolder)obj);
				path = f.Path;
				isDir = true;
				pentry = f.Project;
			} else if (obj is IWorkspaceObject) {
				pentry = ((IWorkspaceObject)obj);
				path = pentry.BaseDirectory;
				isDir = true;
			} else
				return null;

			repo = VersionControlService.GetRepository (pentry);
			return new VersionControlItem (repo, pentry, path, isDir);
		}
		
	}
}
