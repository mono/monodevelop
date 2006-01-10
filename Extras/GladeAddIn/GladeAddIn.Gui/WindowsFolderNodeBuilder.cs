//
// TestNodeBuilder.cs
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;

namespace GladeAddIn.Gui
{
	public class WindowsFolderNodeBuilder: TypeNodeBuilder
	{
		ProjectFileEventHandler updateDelegate;
		
		public WindowsFolderNodeBuilder ()
		{
			updateDelegate = (ProjectFileEventHandler) MonoDevelop.Core.Gui.Services.DispatchService.GuiDispatch (new ProjectFileEventHandler (OnUpdateFiles));
		}
		
		public override Type NodeDataType {
			get { return typeof(WindowsFolder); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "WindowsAndDialogs";
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("Windows and Dialogs");
			icon = Context.GetIcon (Stock.OpenResourceFolder);
			closedIcon = Context.GetIcon (Stock.ClosedResourceFolder);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project p = ((WindowsFolder)dataObject).Project;
			GuiBuilderProject[] projects = GladeService.GetGuiBuilderProjects (p);
			foreach (GuiBuilderProject project in projects)
				builder.AddChild (project);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project p = ((WindowsFolder)dataObject).Project;
			GuiBuilderProject[] projects = GladeService.GetGuiBuilderProjects (p);
			return projects.Length != 0;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is ResourceFolder || otherNode.DataItem is ProjectReferenceCollection)
				return 1;
			else
				return -1;
		}

		public override void OnNodeAdded (object dataObject)
		{
			Project project = ((WindowsFolder) dataObject).Project;
			project.FileAddedToProject += updateDelegate;
			project.FileRemovedFromProject += updateDelegate;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			Project project = ((WindowsFolder) dataObject).Project;
			project.FileAddedToProject -= updateDelegate;
			project.FileRemovedFromProject -= updateDelegate;
		}
		
		void OnUpdateFiles (object s, ProjectFileEventArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (args.Project);
			if (tb != null) {
				if (tb.MoveToChild ("WindowsAndDialogs", typeof(WindowsFolder))) {
					tb.UpdateAll ();
				}
			}
		}
	}
}
