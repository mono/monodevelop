//  WholeCombineDocumentIterator.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
//  Copyright (C) 2008 Novell, Inc. (http://www.novell.com)
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.IO;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Gui.Search
{
	internal class WholeCombineDocumentIterator : FilesDocumentIterator
	{
		public WholeCombineDocumentIterator () : base ()
		{
		}
		
		public override string GetSearchDescription (string pattern)
		{
			return GettextCatalog.GetString ("Looking for '{0}' in all projects", pattern);
		}
		
		public override string GetReplaceDescription (string pattern)
		{
			return GettextCatalog.GetString ("Replacing '{0}' in all projects", pattern);
		}
		
		public override void Reset() 
		{
			files.Clear();
			if (IdeApp.Workspace.IsOpen) {
				foreach (Project p in IdeApp.Workspace.GetAllProjects ()) {
					AddFiles (p);
				}
			}
			
			curIndex = -1;
		}
		
		void AddFiles(Project project)
		{
			foreach (ProjectFile file in project.Files) {
				if (file.Subtype == Subtype.Code) {
					files.Add(file.Name);
				}
			}
		}
	}
}
