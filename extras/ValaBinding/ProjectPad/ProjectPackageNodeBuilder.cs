//
// ProjectPackageNodeBuilder.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
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
using System.Reflection;
using Mono.Addins;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.ValaBinding.ProjectPad
{
	public class ProjectPackageNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ProjectPackage); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(PackageNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ProjectPackage)dataObject).File;
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/ValaBinding/Views/ProjectBrowser/ContextMenu/PackageNode"; }
		}

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            nodeInfo.Label = ((ProjectPackage)dataObject).Name;

            if (((ProjectPackage)dataObject).IsProject)
                nodeInfo.Icon = Context.GetIcon("md-vala-project-reference");
			else
                nodeInfo.Icon = Context.GetIcon(Stock.Reference);
        }
	}
	
	public class PackageNodeCommandHandler : NodeCommandHandler
	{
		public override void DeleteItem ()
		{
			ProjectPackage package = CurrentNode.DataItem as ProjectPackage;
			ValaProject project = CurrentNode.GetParentDataItem (
			    typeof(ValaProject), false) as ValaProject;
			
			project.Packages.Remove (package);
			
			IdeApp.ProjectOperations.Save (project);
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy;
		}
		
		[CommandHandler (MonoDevelop.ValaBinding.ValaProjectCommands.ShowPackageDetails)]
		public void ShowPackageDetails ()
		{
			ProjectPackage package = (ProjectPackage)CurrentNode.DataItem;
			
			// package.ParsePackage ();
			
			PackageDetails details = new PackageDetails (package);
			details.Show ();
		}
	}
}
