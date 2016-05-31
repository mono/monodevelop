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

using MonoDevelop.Components.Commands;
using MonoDevelop.Deployment.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Deployment.NodeBuilders
{
	internal class PackagingProjectNodeBuilder: TypeNodeBuilder
	{
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
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			PackagingProject project = dataObject as PackagingProject;
			nodeInfo.Label = project.Name;
			nodeInfo.Icon = Context.GetIcon ("md-package-project");
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			PackagingProject project = dataObject as PackagingProject;
			builder.AddChildren (project.Packages);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			PackagingProject project = dataObject as PackagingProject;
			return project.Packages.Count > 0;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			PackagingProject project = dataObject as PackagingProject;
			project.PackagesChanged += OnConfigurationsChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			PackagingProject project = dataObject as PackagingProject;
			project.PackagesChanged -= OnConfigurationsChanged;
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
			IdeApp.ProjectOperations.RemoveSolutionItem (project);
		}
		
		[CommandHandler (ProjectCommands.Build)]
		protected void OnBuild ()
		{
			PackagingProject project = CurrentNode.DataItem as PackagingProject;
			DeployOperations.BuildPackages (project);
		}
	}
}
