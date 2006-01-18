//
// ProjectFolderNodeBuilderExtension.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Components.Commands;

namespace GladeAddIn.Gui
{
	class ProjectFolderNodeBuilderExtension: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectFolder).IsAssignableFrom (dataType) ||
					typeof(Project).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectFolderNodeBuilderExtensionCommandHandler); }
		}
	}
	
	class ProjectFolderNodeBuilderExtensionCommandHandler: NodeCommandHandler
	{
		[CommandHandler (GladeAddIn.GladeCommands.AddNewDialog)]
		public void AddNewDialogToProject()
		{
			AddNewWindow ("GladeDialogFileTemplate");
		}
		
		[CommandHandler (GladeAddIn.GladeCommands.AddNewWindow)]
		public void AddNewWindowToProject()
		{
			AddNewWindow ("GladeWindowFileTemplate");
		}
		
		public void AddNewWindow (string id)
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			string path;
			if (CurrentNode.DataItem is ProjectFolder)
				path = ((ProjectFolder)CurrentNode.DataItem).Path;
			else
				path = ((Project)CurrentNode.DataItem).BaseDirectory;

			IdeApp.ProjectOperations.CreateProjectFile (project, path, id);
			
			using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor ()) {
				project.Save (m);
			}
			CurrentNode.Expanded = true;
		}
	}
}
