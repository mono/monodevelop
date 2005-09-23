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

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Commands;

namespace MonoDevelop.Gui.Pads.ProjectPad
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
			addedHandler = (ProjectReferenceEventHandler) Runtime.DispatchService.GuiDispatch (new ProjectReferenceEventHandler (OnAddReference));
			removedHandler = (ProjectReferenceEventHandler) Runtime.DispatchService.GuiDispatch (new ProjectReferenceEventHandler (OnRemoveReference));

			Runtime.ProjectService.ReferenceAddedToProject += addedHandler;
			Runtime.ProjectService.ReferenceRemovedFromProject += removedHandler;
		}
		
		public override void Dispose ()
		{
			Runtime.ProjectService.ReferenceAddedToProject -= addedHandler;
			Runtime.ProjectService.ReferenceRemovedFromProject -= removedHandler;
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
			return dataObject is ProjectReference;
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			ProjectReference pref = dataObject as ProjectReference;
			ITreeNavigator nav = CurrentNode;

			if (operation == DragOperation.Move) {
				NodePosition pos = nav.CurrentPosition;
				nav.MoveToObject (dataObject);
				nav.MoveToParent (typeof(Project));
				Project p = nav.DataItem as Project;
				p.ProjectReferences.Remove (pref);
				
				nav.MoveToPosition (pos);
				nav.MoveToParent (typeof(Project));
				p = nav.DataItem as Project;
				p.ProjectReferences.Add (pref);
			} else {
				nav.MoveToParent (typeof(Project));
				Project p = nav.DataItem as Project;
				p.ProjectReferences.Add ((ProjectReference) pref.Clone ());
			}
			Runtime.ProjectService.SaveCombine();
		}
		
		[CommandHandler (ProjectCommands.AddReference)]
		public void AddReferenceToProject ()
		{
			Project p = (Project) CurrentNode.GetParentDataItem (typeof(Project), false);
			if (Runtime.ProjectService.AddReferenceToProject (p)) {
				Runtime.ProjectService.SaveCombine();
				CurrentNode.Expanded = true;
			}
		}
	}
}
