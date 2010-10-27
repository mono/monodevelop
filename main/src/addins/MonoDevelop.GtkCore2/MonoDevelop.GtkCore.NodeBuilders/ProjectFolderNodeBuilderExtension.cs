//
// ProjectFolderNodeBuilderExtension.cs
//
// Author:
//   Lluis Sanchez Gual, Krzysztof Marecki
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
// Copyright (C) 2010 Krzysztof Marecki
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
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Components.Commands;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	class ProjectFolderNodeBuilderExtension: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectFolder).IsAssignableFrom (dataType) && !(dataType is GuiProjectFolder);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(UserInterfaceCommandHandler); }
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			if (dataObject is GuiProjectFolder)
				return;
			
			ProjectFolder folder = dataObject as ProjectFolder;
			if (folder != null && folder.Project is DotNetProject) {
				GtkDesignInfo info = GtkDesignInfo.FromProject (folder.Project);
				if (info.SteticFolder == folder.Path)
					attributes |= NodeAttributes.Hidden;
			}
		}
	}
	
	class UserInterfaceCommandHandler: NodeCommandHandler
	{
		[CommandHandler (MonoDevelop.GtkCore.GtkCommands.AddNewDialog)]
		public void AddNewDialogToProject()
		{
			AddNewWindow ("DialogFileTemplate");
		}
		
		[CommandUpdateHandler (MonoDevelop.GtkCore.GtkCommands.AddNewDialog)]
		public void UpdateAddNewDialogToProject (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow () && !(CurrentNode.DataItem is GuiProjectFolder);
		}
		
		[CommandHandler (MonoDevelop.GtkCore.GtkCommands.AddNewWindow)]
		public void AddNewWindowToProject()
		{
			AddNewWindow ("WindowFileTemplate");
		}
		
		[CommandUpdateHandler (MonoDevelop.GtkCore.GtkCommands.AddNewWindow)]
		public void UpdateAddNewWindowToProject (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow () && !(CurrentNode.DataItem is GuiProjectFolder);
		}
		
		[CommandHandler (MonoDevelop.GtkCore.GtkCommands.AddNewWidget)]
		public void AddNewWidgetToProject()
		{
			AddNewWindow ("WidgetFileTemplate");
		}
		
		[CommandUpdateHandler (MonoDevelop.GtkCore.GtkCommands.AddNewWidget)]
		public void UpdateAddNewWidgetToProject (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow () && !(CurrentNode.DataItem is GuiProjectFolder);
		} 
		
		[CommandHandler (MonoDevelop.GtkCore.GtkCommands.AddNewActionGroup)]
		public void AddNewActionGroupToProject()
		{
			AddNewWindow ("ActionGroupFileTemplate");
		}
		
		[CommandUpdateHandler (MonoDevelop.GtkCore.GtkCommands.AddNewActionGroup)]
		public void UpdateAddNewActionGroupToProject(CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow () && !(CurrentNode.DataItem is GuiProjectFolder);
		}
		
		public override bool CanDropMultipleNodes (object[] dataObjects, DragOperation operation, DropPosition position)
		{
			foreach (object dataObject in dataObjects) 
				if (dataObjects is GuiProjectFolder)
					return false;
			
			return base.CanDropMultipleNodes (dataObjects, operation, position);
		}
			
		bool CanAddWindow ()
		{
			DotNetProject project = CurrentNode.GetParentDataItem (typeof(Project), true) as DotNetProject;
			return GtkDesignInfo.SupportsDesigner (project);
		}
		
		public void AddNewWindow (string id)
		{
			DotNetProject project = CurrentNode.GetParentDataItem (typeof(Project), true) as DotNetProject;
			if (project == null)
				return;
			
			object dataItem = CurrentNode.DataItem;
			
			ProjectFolder folder = CurrentNode.GetParentDataItem (typeof(ProjectFolder), true) as ProjectFolder;
			id = "Partial" + id;
			
			string path;
			if (folder != null)
				path = folder.Path;
			else
				path = project.BaseDirectory;

			IdeApp.ProjectOperations.CreateProjectFile (project, path, id);
			IdeApp.ProjectOperations.Save (project);
			
			ITreeNavigator nav = Tree.GetNodeAtObject (dataItem);
			if (nav != null)
				nav.Expanded = true;
		}
	}
}
