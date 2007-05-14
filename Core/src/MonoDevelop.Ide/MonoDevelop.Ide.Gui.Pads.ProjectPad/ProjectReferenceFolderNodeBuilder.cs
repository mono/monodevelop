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
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProjectReferenceFolderNodeBuilder: TypeNodeBuilder
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
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/ReferenceFolderNode"; }
		}
		
		protected override void Initialize ()
		{
			addedHandler = (ProjectReferenceEventHandler) Services.DispatchService.GuiDispatch (new ProjectReferenceEventHandler (OnAddReference));
			removedHandler = (ProjectReferenceEventHandler) Services.DispatchService.GuiDispatch (new ProjectReferenceEventHandler (OnRemoveReference));

			IdeApp.ProjectOperations.ReferenceAddedToProject += addedHandler;
			IdeApp.ProjectOperations.ReferenceRemovedFromProject += removedHandler;
		}
		
		public override void Dispose ()
		{
			IdeApp.ProjectOperations.ReferenceAddedToProject -= addedHandler;
			IdeApp.ProjectOperations.ReferenceRemovedFromProject -= removedHandler;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("References");
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
			ITreeBuilder tb = Context.GetTreeBuilder (e.Project);
			if (tb != null) {
				if (tb.FindChild (e.ProjectReference, true))
					tb.Remove ();
			}
		}
		
		void OnAddReference (object sender, ProjectReferenceEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.Project.ProjectReferences);
			if (tb != null) tb.AddChild (e.ProjectReference);
		}
	}
	
	public class ProjectReferenceFolderNodeCommandHandler: NodeCommandHandler
	{
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy | DragOperation.Move;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			return dataObject is ProjectReference || dataObject is Project;
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			// It allows dropping either project references or projects.
			// Dropping a project creates a new project reference to that project
			
			Project project = dataObject as Project;
			if (project != null) {
				ProjectReference pr = new ProjectReference (project);
				Project p = CurrentNode.GetParentDataItem (typeof(Project), false) as Project;
				if (ProjectReferencesProject (project, p.Name))
					return;
				p.ProjectReferences.Add (pr);
				IdeApp.ProjectOperations.SaveProject (p);
				return;
			}
			
			// It's dropping a ProjectReference object.
			
			ProjectReference pref = dataObject as ProjectReference;
			ITreeNavigator nav = CurrentNode;

			if (operation == DragOperation.Move) {
				NodePosition pos = nav.CurrentPosition;
				nav.MoveToObject (dataObject);
				Project p = nav.GetParentDataItem (typeof(Project), true) as Project;
				nav.MoveToPosition (pos);
				Project p2 = nav.GetParentDataItem (typeof(Project), true) as Project;
				
				p.ProjectReferences.Remove (pref);

				// Check if there is a cyclic reference after removing from the source project
				if (pref.ReferenceType == ReferenceType.Project) {
					Project pdest = p.RootCombine.FindProject (pref.Reference);
					if (pdest == null || ProjectReferencesProject (pdest, p2.Name)) {
						// Restore the dep
						p.ProjectReferences.Add (pref);
						return;
					}
				}
				
				p2.ProjectReferences.Add (pref);
				IdeApp.ProjectOperations.SaveProject (p);
				IdeApp.ProjectOperations.SaveProject (p2);
			} else {
				nav.MoveToParent (typeof(Project));
				Project p = nav.DataItem as Project;
				
				// Check for cyclic referencies
				if (pref.ReferenceType == ReferenceType.Project) {
					Project pdest = p.RootCombine.FindProject (pref.Reference);
					if (pdest == null || ProjectReferencesProject (pdest, p.Name))
						return;
				}
				p.ProjectReferences.Add ((ProjectReference) pref.Clone ());
				IdeApp.ProjectOperations.SaveProject (p);
			}
		}
		
		[CommandHandler (ProjectCommands.AddReference)]
		public void AddReferenceToProject ()
		{
			Project p = (Project) CurrentNode.GetParentDataItem (typeof(Project), false);
			if (IdeApp.ProjectOperations.AddReferenceToProject (p)) {
				IdeApp.ProjectOperations.SaveProject (p);
				CurrentNode.Expanded = true;
			}
		}
		
		bool ProjectReferencesProject (Project project, string targetProject)
		{
			if (project.Name == targetProject) {
				IdeApp.Services.MessageService.ShowError (GettextCatalog.GetString ("Cyclic project references are not allowed."));
				return true;
			}
			
			foreach (ProjectReference pr in project.ProjectReferences) {
				Project pref = project.RootCombine.FindProject (pr.Reference);
				if (pref != null && ProjectReferencesProject (pref, targetProject))
					return true;
			}
			return false;
		}
	}
}
