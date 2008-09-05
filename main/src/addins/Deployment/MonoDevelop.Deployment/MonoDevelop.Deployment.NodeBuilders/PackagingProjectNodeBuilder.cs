//
// PackagingProjectNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Deployment.Gui;
using GuiServices = MonoDevelop.Core.Gui.Services;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Deployment.NodeBuilders
{
	internal class PackagingProjectNodeBuilder: TypeNodeBuilder
	{
		EventHandler configsChanged;
		
		public PackagingProjectNodeBuilder ()
		{
			configsChanged = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnConfigurationsChanged));
		}
		
		public override Type CommandHandlerType {
			get { return typeof(PackagingProjectNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Deployment/ContextMenu/ProjectPad/PackagingProject"; }
		}

		public override Type NodeDataType {
			get { return typeof(PackagingProject); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((PackagingProject)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			PackagingProject project = dataObject as PackagingProject;
			label = project.Name;
			icon = Context.GetIcon ("md-packaging-project");
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			PackagingProject project = dataObject as PackagingProject;
				
			foreach (Package p in project.Packages)
				builder.AddChild (p);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			PackagingProject project = dataObject as PackagingProject;
			return project.Packages.Count > 0;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			PackagingProject project = dataObject as PackagingProject;
			project.PackagesChanged += configsChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			PackagingProject project = dataObject as PackagingProject;
			project.PackagesChanged -= configsChanged;
		}
		
		public void OnConfigurationsChanged (object sender, EventArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (sender);
			if (tb != null) tb.UpdateAll ();
		}
	}
	
	class PackagingProjectNodeCommandHandler: NodeCommandHandler
	{
		public override void DeleteItem ()
		{
			PackagingProject project = CurrentNode.DataItem as PackagingProject;
			if (MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to delete the project '{0}'?", project.Name), AlertButton.Cancel, AlertButton.Delete) == AlertButton.Delete) {
				SolutionFolder c = project.ParentFolder;
				c.Items.Remove (project);
				project.Dispose ();
				IdeApp.ProjectOperations.Save (c.ParentSolution);
			}
		}
		
		[CommandHandler (ProjectCommands.Build)]
		protected void OnBuild ()
		{
			PackagingProject project = CurrentNode.DataItem as PackagingProject;
			DeployOperations.BuildPackages (project);
		}
	}
}
