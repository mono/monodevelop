//
// ProjectReferenceFolderNodeBuilder.cs
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
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class ProjectReferenceFolderNodeBuilder: TypeNodeBuilder
	{
		ProjectReferenceEventHandler addedHandler;
		ProjectReferenceEventHandler removedHandler;

		public override Type NodeDataType {
			get { return typeof(ProjectReferenceCollection); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectReferenceFolderNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "References";
		}
		
		protected override void Initialize ()
		{
			addedHandler = (ProjectReferenceEventHandler) DispatchService.GuiDispatch (new ProjectReferenceEventHandler (OnAddReference));
			removedHandler = (ProjectReferenceEventHandler) DispatchService.GuiDispatch (new ProjectReferenceEventHandler (OnRemoveReference));

			IdeApp.Workspace.ReferenceAddedToProject += addedHandler;
			IdeApp.Workspace.ReferenceRemovedFromProject += removedHandler;
		}
		
		public override void Dispose ()
		{
			IdeApp.Workspace.ReferenceAddedToProject -= addedHandler;
			IdeApp.Workspace.ReferenceRemovedFromProject -= removedHandler;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = GLib.Markup.EscapeText (GettextCatalog.GetString ("References"));
			icon = Context.GetIcon (Stock.OpenReferenceFolder);
			closedIcon = Context.GetIcon (Stock.ClosedReferenceFolder);
		}

		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			ProjectReferenceCollection refs = (ProjectReferenceCollection) dataObject;
			foreach (ProjectReference pref in refs)
				ctx.AddChild (pref);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ((ProjectReferenceCollection) dataObject).Count > 0;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			return -1;
		}

		void OnRemoveReference (object sender, ProjectReferenceEventArgs e)
		{
			var p = e.Project as DotNetProject;
			if (p != null) {
				ITreeBuilder tb = Context.GetTreeBuilder (p.References);
				if (tb != null && tb.FindChild (e.ProjectReference, true))
					tb.Remove ();
			}
		}
		
		void OnAddReference (object sender, ProjectReferenceEventArgs e)
		{
			DotNetProject p = e.Project as DotNetProject;
			if (p != null) {
				ITreeBuilder tb = Context.GetTreeBuilder (p.References);
				if (tb != null)
					tb.AddChild (e.ProjectReference);
			}
		}
	}
	
	class ProjectReferenceFolderNodeCommandHandler: NodeCommandHandler
	{
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			return dataObject is ProjectReference || dataObject is Project;
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			// It allows dropping either project references or projects.
			// Dropping a project creates a new project reference to that project
			
			DotNetProject project = dataObject as DotNetProject;
			if (project != null) {
				ProjectReference pr = new ProjectReference (project);
				DotNetProject p = CurrentNode.GetParentDataItem (typeof(DotNetProject), false) as DotNetProject;
				// Circular dependencies are not allowed.
				if (HasCircularReference (project, p.Name))
					return;

				// If the reference already exists, bail out
				if (ProjectReferencesProject (p, project.Name))
					return;
				p.References.Add (pr);
				IdeApp.ProjectOperations.Save (p);
				return;
			}
			
			// It's dropping a ProjectReference object.
			
			ProjectReference pref = dataObject as ProjectReference;
			ITreeNavigator nav = CurrentNode;

			if (operation == DragOperation.Move) {
				NodePosition pos = nav.CurrentPosition;
				nav.MoveToObject (dataObject);
				DotNetProject p = nav.GetParentDataItem (typeof(DotNetProject), true) as DotNetProject;
				nav.MoveToPosition (pos);
				DotNetProject p2 = nav.GetParentDataItem (typeof(DotNetProject), true) as DotNetProject;
				
				p.References.Remove (pref);

				// Check if there is a cyclic reference after removing from the source project
				if (pref.ReferenceType == ReferenceType.Project) {
					DotNetProject pdest = p.ParentSolution.FindProjectByName (pref.Reference) as DotNetProject;
					if (pdest == null || ProjectReferencesProject (pdest, p2.Name)) {
						// Restore the dep
						p.References.Add (pref);
						return;
					}
				}
				
				p2.References.Add (pref);
				IdeApp.ProjectOperations.Save (p);
				IdeApp.ProjectOperations.Save (p2);
			} else {
				nav.MoveToParent (typeof(DotNetProject));
				DotNetProject p = nav.DataItem as DotNetProject;
				
				// Check for cyclic referencies
				if (pref.ReferenceType == ReferenceType.Project) {
					DotNetProject pdest = p.ParentSolution.FindProjectByName (pref.Reference) as DotNetProject;
					if (pdest == null)
						return;
					if (HasCircularReference (pdest, p.Name))
						return;

					// The reference is already there
					if (ProjectReferencesProject (p, pdest.Name))
						return;
				}
				p.References.Add ((ProjectReference) pref.Clone ());
				IdeApp.ProjectOperations.Save (p);
			}
		}
		
		public override void ActivateItem ()
		{
			AddReferenceToProject ();
		}
		
		[CommandHandler (ProjectCommands.AddReference)]
		public void AddReferenceToProject ()
		{
			DotNetProject p = (DotNetProject) CurrentNode.GetParentDataItem (typeof(DotNetProject), false);
			if (IdeApp.ProjectOperations.AddReferenceToProject (p)) {
				IdeApp.ProjectOperations.Save (p);
				CurrentNode.Expanded = true;
			}
		}

		bool HasCircularReference (DotNetProject project, string targetProject)
		{
			bool result = ProjectReferencesProject (project, targetProject);
			if (result)
				MessageService.ShowError (GettextCatalog.GetString ("Cyclic project references are not allowed."));
			return result;
		}

		bool ProjectReferencesProject (DotNetProject project, string targetProject)
		{
			if (project.Name == targetProject)
				return true;
			
			foreach (ProjectReference pr in project.References) {
				DotNetProject pref = project.ParentSolution.FindProjectByName (pr.Reference) as DotNetProject;
				if (pref != null && ProjectReferencesProject (pref, targetProject))
					return true;
			}
			return false;
		}
	}
}
