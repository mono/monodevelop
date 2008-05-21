//
// ProjectReferenceNodeBuilder.cs
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
using System.IO;
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProjectReferenceNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ProjectReference); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectReferenceNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ProjectReference)dataObject).Reference;
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ProjectPad/Reference"; }
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			ProjectReference pref = (ProjectReference) dataObject;
			
			switch (pref.ReferenceType) {
				case ReferenceType.Project:
					label = pref.Reference;
					break;
				case ReferenceType.Assembly:
					label = Path.GetFileName(pref.Reference);
					break;
				case ReferenceType.Gac:
					label = pref.Reference.Split(',')[0];
					break;
				default:
					label = pref.Reference;
					break;
			}
			
			icon = Context.GetIcon (Stock.Reference);
		}
	}
	
	public class ProjectReferenceNodeCommandHandler: NodeCommandHandler
	{

		public override void ActivateItem ()
		{
			ProjectReference pref = CurrentNode.DataItem as ProjectReference;
			if (pref == null)
				return;
			foreach (string fileName in pref.GetReferencedFileNames (IdeApp.Workspace.ActiveConfiguration)) {
				IdeApp.Workbench.OpenDocument (fileName);
			}
		}
				
		public override void DeleteItem ()
		{
			ProjectReference pref = (ProjectReference) CurrentNode.DataItem;
			DotNetProject project = CurrentNode.GetParentDataItem (typeof(DotNetProject), false) as DotNetProject;
			project.References.Remove (pref);
			IdeApp.ProjectOperations.Save (project);
		}
		
		[CommandHandler (ProjectCommands.LocalCopyReference)]
		public void ChangeLocalReference ()
		{
			ProjectReference pref = (ProjectReference) CurrentNode.DataItem;
			pref.LocalCopy = !pref.LocalCopy;
			Project project = CurrentNode.GetParentDataItem (typeof(Project), false) as Project;
			IdeApp.ProjectOperations.Save (project);
		}
		
		[CommandUpdateHandler (ProjectCommands.LocalCopyReference)]
		public void UpdateLocalReference (CommandInfo info)
		{
			ProjectReference pref = (ProjectReference) CurrentNode.DataItem;
			if (pref.ReferenceType != ReferenceType.Gac)
				info.Checked = pref.LocalCopy;
			else {
				info.Checked = false;
				info.Enabled = false;
			}
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy;
		}
	}
}
