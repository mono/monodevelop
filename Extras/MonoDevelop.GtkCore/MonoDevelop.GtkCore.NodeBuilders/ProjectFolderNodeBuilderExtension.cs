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
using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.SolutionViewPad;
using MonoDevelop.Components.Commands;
using MonoDevelop.GtkCore.GuiBuilder;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	class ProjectFolderNodeBuilderExtension: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(FolderNode).IsAssignableFrom (dataType) ||
					typeof(SolutionProject).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(UserInterfaceCommandHandler); }
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			if (treeNavigator.Options ["ShowAllFiles"])
				return;

			FolderNode folder = dataObject as FolderNode;
			if (folder != null && folder.Project != null) {
				GtkDesignInfo info = GtkCoreService.GetGtkInfo (folder.Project.Project);
				if (info != null && info.GtkGuiFolder == folder.Path)
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
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (MonoDevelop.GtkCore.GtkCommands.AddNewWindow)]
		public void AddNewWindowToProject()
		{
			AddNewWindow ("WindowFileTemplate");
		}
		
		[CommandUpdateHandler (MonoDevelop.GtkCore.GtkCommands.AddNewWindow)]
		public void UpdateAddNewWindowToProject (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (MonoDevelop.GtkCore.GtkCommands.AddNewWidget)]
		public void AddNewWidgetToProject()
		{
			AddNewWindow ("WidgetFileTemplate");
		}
		
		[CommandUpdateHandler (MonoDevelop.GtkCore.GtkCommands.AddNewWidget)]
		public void UpdateAddNewWidgetToProject (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (MonoDevelop.GtkCore.GtkCommands.AddNewActionGroup)]
		public void AddNewActionGroupToProject()
		{
			AddNewWindow ("ActionGroupFileTemplate");
		}
		
		[CommandUpdateHandler (MonoDevelop.GtkCore.GtkCommands.AddNewActionGroup)]
		public void UpdateAddNewActionGroupToProject(CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (GtkCommands.ImportGladeFile)]
		protected void OnImportGladeFile ()
		{
			SolutionProject project = CurrentNode.GetParentDataItem (typeof(SolutionProject), true) as SolutionProject;
			GuiBuilderService.ImportGladeFile (project.Project);
		}
		
		[CommandUpdateHandler (GtkCommands.ImportGladeFile)]
		protected void UpdateImportGladeFile (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (GtkCommands.EditIcons)]
		protected void OnEditIcons ()
		{
			SolutionProject project = CurrentNode.GetParentDataItem (typeof(SolutionProject), true) as SolutionProject;
			GuiBuilderProject gp = GtkCoreService.GetGtkInfo (project.Project).GuiBuilderProject;
			Stetic.Project sp = gp.SteticProject;
			sp.EditIcons ();
			gp.Save (true);
		}
		
		[CommandUpdateHandler (GtkCommands.EditIcons)]
		protected void UpdateEditIcons (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandHandler (GtkCommands.GtkSettings)]
		protected void OnGtkSettings ()
		{
// TODO: Project Conversion
//	SolutionProject project = CurrentNode.GetParentDataItem (typeof(SolutionProject), true) as SolutionProject;
//			IdeApp.ProjectOperations.ShowOptions (project.Project, "SteticOptionsPanel");
		}
		
		[CommandUpdateHandler (GtkCommands.EditIcons)]
		protected void UpdateGtkSettings (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		bool CanAddWindow ()
		{
			MSBuildProject project = CurrentNode.GetParentDataItem (typeof(MSBuildProject), true) as MSBuildProject;
			if (project != null) {
				GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
				return (info == null || !info.GuiBuilderProject.HasError);
			}
			return false;
		}
		
		public void AddNewWindow (string id)
		{
			SolutionProject project = CurrentNode.GetParentDataItem (typeof(SolutionProject), true) as SolutionProject;
			if (project == null)
				return;
			
			FolderNode folder = CurrentNode.GetParentDataItem (typeof(FolderNode), true) as FolderNode;
			
			if (GtkCoreService.SupportsPartialTypes (project.Project as MSBuildProject))
				id = "Partial" + id;
			
			string path;
			if (folder != null)
				path = folder.Path;
			else
				path = project.Project.BasePath;

// TODO: Projct Conversion
//			IdeApp.ProjectOperations.CreateProjectFile (project, path, id);
			
			ProjectService.SaveProject (project.Project);
			CurrentNode.Expanded = true;
		}
	}
}
