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
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Components.Commands;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	class ProjectFolderNodeBuilderExtension: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectFolder).IsAssignableFrom (dataType) ||
			       typeof(DotNetProject).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(UserInterfaceCommandHandler); }
		}
		
		public override void GetNodeAttributes (ITreeNavigator parentNode, object dataObject, ref NodeAttributes attributes)
		{
			if (parentNode.Options ["ShowAllFiles"])
				return;

			var folder = dataObject as ProjectFolder;
			if (folder != null && folder.Project is DotNetProject) {
				GtkDesignInfo info = GtkDesignInfo.FromProject (folder.Project);
				if (info.GtkGuiFolder == folder.Path)
					attributes |= NodeAttributes.Hidden;
			}
		}
	}
	
	class UserInterfaceCommandHandler: NodeCommandHandler
	{
		[CommandHandler (GtkCommands.AddNewDialog)]
		public void AddNewDialogToProject()
		{
			AddNewWindow ("DialogFileTemplate");
		}
		
		[CommandUpdateHandler (GtkCommands.AddNewDialog)]
		public void UpdateAddNewDialogToProject (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (GtkCommands.AddNewWindow)]
		public void AddNewWindowToProject()
		{
			AddNewWindow ("WindowFileTemplate");
		}
		
		[CommandUpdateHandler (GtkCommands.AddNewWindow)]
		public void UpdateAddNewWindowToProject (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (GtkCommands.AddNewWidget)]
		public void AddNewWidgetToProject()
		{
			AddNewWindow ("WidgetFileTemplate");
		}
		
		[CommandUpdateHandler (GtkCommands.AddNewWidget)]
		public void UpdateAddNewWidgetToProject (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (GtkCommands.AddNewActionGroup)]
		public void AddNewActionGroupToProject()
		{
			AddNewWindow ("ActionGroupFileTemplate");
		}
		
		[CommandUpdateHandler (GtkCommands.AddNewActionGroup)]
		public void UpdateAddNewActionGroupToProject(CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (GtkCommands.ImportGladeFile)]
		protected void OnImportGladeFile ()
		{
			var project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			GuiBuilderService.ImportGladeFile (project);
		}
		
		[CommandUpdateHandler (GtkCommands.ImportGladeFile)]
		protected void UpdateImportGladeFile (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (GtkCommands.EditIcons)]
		protected void OnEditIcons ()
		{
			var project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			GuiBuilderProject gp = GtkDesignInfo.FromProject (project).GuiBuilderProject;
			Stetic.Project sp = gp.SteticProject;
			sp.EditIcons ();
			gp.SaveProject (true);
		}
		
		[CommandUpdateHandler (GtkCommands.EditIcons)]
		protected void UpdateEditIcons (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (GtkCommands.GtkSettings)]
		protected void OnGtkSettings ()
		{
			var project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			IdeApp.ProjectOperations.ShowOptions (project, "SteticOptionsPanel");
		}
		
		[CommandUpdateHandler (GtkCommands.EditIcons)]
		protected void UpdateGtkSettings (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		bool CanAddWindow ()
		{
			var project = CurrentNode.GetParentDataItem (typeof(Project), true) as DotNetProject;
			return GtkDesignInfo.SupportsDesigner (project);
		}
		
		public void AddNewWindow (string id)
		{
			var project = CurrentNode.GetParentDataItem (typeof(Project), true) as DotNetProject;
			if (project == null)
				return;
			
			object dataItem = CurrentNode.DataItem;
			
			var folder = CurrentNode.GetParentDataItem (typeof(ProjectFolder), true) as ProjectFolder;
			
			if (project.UsePartialTypes)
				id = "Partial" + id;
			
			string path = folder != null ? folder.Path : project.BaseDirectory;

			IdeApp.ProjectOperations.CreateProjectFile (project, path, id);
			
			IdeApp.ProjectOperations.Save (project);
			
			ITreeNavigator nav = Tree.GetNodeAtObject (dataItem);
			if (nav != null)
				nav.Expanded = true;
		}
	}
}
