//  
//  GuiProjectFolderNodeBuilder.cs
//  
//  Author:
//       Krzysztof Marecki <marecki.krzysztof@gmail.com>
// 
//  Copyright (c) 2010 KrzysztofMarecki
// 
//  Permission is hereby granted, free of charge, to any person obtaining
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
using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;

using MonoDevelop.GtkCore.GuiBuilder;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	public class GuiProjectFolderNodeBuilder : ProjectFolderNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(GuiProjectFolder); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(GuiProjectFolderCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			//nodes are sorted alphabetically and we want to have gui folder on the top
			return string.Empty;
		}
		
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			GuiProjectFolder folder = (GuiProjectFolder) dataObject;
			
			icon = Context.GetIcon (Stock.OpenResourceFolder);
			closedIcon = Context.GetIcon (Stock.ClosedResourceFolder);
			label = folder.Name;	
		}
	}
	
	public class GuiProjectFolderCommandHandler : ProjectFolderCommandHandler
	{
		[CommandHandler (GtkCommands.ImportGladeFile)]
		protected void OnImportGladeFile ()
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
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
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			GuiBuilderProject gp = GtkDesignInfo.FromProject (project).GuiBuilderProject;
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
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			IdeApp.ProjectOperations.ShowOptions (project, "SteticOptionsPanel");
		}
		
		[CommandUpdateHandler (GtkCommands.EditIcons)]
		protected void UpdateGtkSettings (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWindow ();
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		[CommandUpdateHandler (EditCommands.Rename)]
		[CommandUpdateHandler (ProjectCommands.AddNewFiles)]
		[CommandUpdateHandler (ProjectCommands.AddFiles)]
		[CommandUpdateHandler (ProjectCommands.AddSolutionFolder)]
		[CommandUpdateHandler (ProjectCommands.AddItem)]
		[CommandUpdateHandler (ProjectCommands.NewFolder)]
		[CommandUpdateHandler ("MonoDevelop.Refactoring.RefactoryCommands.Rename")]
		protected void UpdateDisabledCommands (CommandInfo cinfo)
		{
			cinfo.Visible = false;
			cinfo.Enabled = false;
		}
		
		bool CanAddWindow ()
		{
			DotNetProject project = CurrentNode.GetParentDataItem (typeof(Project), true) as DotNetProject;
			return GtkDesignInfo.SupportsDesigner (project);
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.None;
		}
		
		public override bool CanDeleteItem ()
		{
			return false;
		}
		
		public override bool CanDropMultipleNodes (object[] dataObjects, DragOperation operation)
		{
			return false;
		}
		
		public override bool CanDropMultipleNodes (object[] dataObjects, DragOperation operation, DropPosition position)
		{
			return false;
		}
		
		public override bool CanDeleteMultipleItems ()
		{
			return false;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			return false;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation, DropPosition position)
		{
			return false;
		}
		
		public override void RenameItem (string newName)
		{		
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			info.SteticFolderName = newName;
			
			base.RenameItem (newName);
		} 
	}
}

