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
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;

namespace CBinding.ProjectPad
{
	public class ProjectPackageNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(Package); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(PackageNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Package)dataObject).File;
		}
		
		public override string ContextMenuAddinPath {
			get { return "/CBinding/Views/ProjectBrowser/ContextMenu/PackageNode"; }
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder,
		                                object dataObject,
		                                ref string label,
		                                ref Gdk.Pixbuf icon,
		                                ref Gdk.Pixbuf closedIcon)
		{
			label = ((Package)dataObject).Name;
			
			if (((Package)dataObject).IsProject)
				icon = new Gdk.Pixbuf (Assembly.GetExecutingAssembly (), "Icons.16x16.ProjectReference");
			else
				icon = Context.GetIcon (Stock.Reference);
		}
	}
	
	public class PackageNodeCommandHandler : NodeCommandHandler
	{
		public override void DeleteItem ()
		{
			Package package = (Package)CurrentNode.DataItem;
			CProject project = (CProject)CurrentNode.GetParentDataItem (
			    typeof(CProject), false);
			
			project.Packages.Remove (package);
			
			IdeApp.ProjectOperations.SaveProject (project);
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy;
		}
	}
}
