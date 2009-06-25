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
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

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
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			ProjectReference pref = (ProjectReference) dataObject;
			
			switch (pref.ReferenceType) {
				case ReferenceType.Project:
					label = pref.Reference;
					icon = Context.GetIcon ("md-reference-project");
					break;
				case ReferenceType.Assembly:
					label = Path.GetFileName(pref.Reference);
					icon = Context.GetIcon ("md-reference-folder");
					break;
				case ReferenceType.Gac:
					label = pref.Reference.Split(',')[0];
					icon = Context.GetIcon ("md-reference-package");
					break;
				default:
					label = pref.Reference;
					icon = Context.GetIcon (Stock.Reference);
					break;
			}

			if (!pref.IsValid) {
				label = "<span color='red'>" + label + "</span>";
				icon = Context.GetIcon ("md-reference-warning");
			}
		}
		
		public override bool HasChildNodes (MonoDevelop.Ide.Gui.Components.ITreeBuilder builder, object dataObject)
		{
			ProjectReference pref = (ProjectReference) dataObject;
			return !pref.IsValid;
		}

		public override void BuildChildNodes (MonoDevelop.Ide.Gui.Components.ITreeBuilder treeBuilder, object dataObject)
		{
			ProjectReference pref = (ProjectReference) dataObject;
			if (!pref.IsValid)
				treeBuilder.AddChild (new TreeViewItem (pref.ValidationErrorMessage, Gtk.Stock.DialogWarning));
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			ProjectReference pref = (ProjectReference) dataObject;
			pref.StatusChanged += ReferenceStatusChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			ProjectReference pref = (ProjectReference) dataObject;
			pref.StatusChanged -= ReferenceStatusChanged;
		}

		void ReferenceStatusChanged (object sender, EventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (sender);
			if (tb != null)
				tb.UpdateAll ();
		}


	}
	
	public class ProjectReferenceNodeCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			ProjectReference pref = CurrentNode.DataItem as ProjectReference;
			if (pref != null) {
				foreach (string fileName in pref.GetReferencedFileNames (IdeApp.Workspace.ActiveConfiguration))
					IdeApp.Workbench.OpenDocument (fileName);
			}
		}
				
		public override void DeleteMultipleItems ()
		{
			Dictionary<Project,Project> projects = new Dictionary<Project,Project> ();
			foreach (ITreeNavigator nav in CurrentNodes) {
				ProjectReference pref = (ProjectReference) nav.DataItem;
				DotNetProject project = nav.GetParentDataItem (typeof(DotNetProject), false) as DotNetProject;
				project.References.Remove (pref);
				projects [project] = project;
			}
			foreach (Project p in projects.Values)
				IdeApp.ProjectOperations.Save (p);
		}
		
		[CommandHandler (ProjectCommands.LocalCopyReference)]
		[AllowMultiSelection]
		public void ChangeLocalReference ()
		{
			Dictionary<Project,Project> projects = new Dictionary<Project,Project> ();
			ProjectReference firstRef = null;
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectReference pref = (ProjectReference) node.DataItem;
				if (firstRef == null) {
					firstRef = pref;
					pref.LocalCopy = !pref.LocalCopy;
				} else
					pref.LocalCopy = firstRef.LocalCopy;
				Project project = node.GetParentDataItem (typeof(Project), false) as Project;
				projects [project] = project;
			}
			foreach (Project p in projects.Values)
				IdeApp.ProjectOperations.Save (p);
		}
		
		[CommandUpdateHandler (ProjectCommands.LocalCopyReference)]
		public void UpdateLocalReference (CommandInfo info)
		{
			ProjectReference lastRef = null;
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectReference pref = (ProjectReference) node.DataItem;
				if (pref.ReferenceType != ReferenceType.Gac) {
					if (lastRef == null || lastRef.LocalCopy == pref.LocalCopy) {
						lastRef = pref;
						info.Checked = pref.LocalCopy;
					} else
						info.CheckedInconsistent = true;
				}
				else {
					info.Checked = false;
					info.Enabled = false;
				}
			}
		}
		
		[CommandHandler (ProjectCommands.SpecificAssemblyVersion)]
		[AllowMultiSelection]
		public void RequireSpecificAssemblyVersion ()
		{
			Dictionary<Project,Project> projects = new Dictionary<Project,Project> ();
			ProjectReference firstRef = null;
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectReference pref = (ProjectReference) node.DataItem;
				if (firstRef == null) {
					firstRef = pref;
					pref.SpecificVersion = !pref.SpecificVersion;
				} else
					pref.SpecificVersion = firstRef.SpecificVersion;
				Project project = node.GetParentDataItem (typeof(Project), false) as Project;
				projects [project] = project;
			}
			foreach (Project p in projects.Values)
				IdeApp.ProjectOperations.Save (p);
		}
		
		[CommandUpdateHandler (ProjectCommands.SpecificAssemblyVersion)]
		public void UpdateRequireSpecificAssemblyVersion (CommandInfo info)
		{
			ProjectReference lastRef = null;
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectReference pref = (ProjectReference) node.DataItem;
				if (pref.ReferenceType == ReferenceType.Gac) {
					if (lastRef == null || lastRef.LocalCopy == pref.LocalCopy) {
						lastRef = pref;
						info.Checked = pref.SpecificVersion;
					} else
						info.CheckedInconsistent = true;
				}
				else {
					info.Checked = false;
					info.Enabled = false;
				}
			}
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy;
		}
	}
}
