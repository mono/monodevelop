//
// DirectoryNodeBuilder.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.IO;

using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Projects.Item;

namespace MonoDevelop.Ide.Gui.Pads.SolutionViewPad
{
	public class DirectoryNodeBuilder : TypeNodeBuilder
	{
		public DirectoryNodeBuilder ()
		{
		}

		public override Type NodeDataType {
			get { return typeof(DirectoryNode); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			DirectoryNode directoryNode = dataObject as DirectoryNode;
			if (directoryNode == null) 
				return "DirectoryNode";
			
			return Path.GetFileName (directoryNode.Path);
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
/*		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/CombineBrowserNode"; }
		}*/
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			DirectoryNode directoryNode = dataObject as DirectoryNode;
			if (directoryNode == null) 
				return;
			label = Path.GetFileName (directoryNode.Path);
			
			icon       = Context.GetIcon (directoryNode.IsInProject ? Stock.OpenFolder : Stock.OpenDashedFolder);
			closedIcon = Context.GetIcon (directoryNode.IsInProject ? Stock.ClosedFolder : Stock.ClosedDashedFolder);
		}
		
		public static bool IsFileInProject (IProject project, string fileName)
		{
			foreach (ProjectItem item in project.Items) {
				string fullName = Path.Combine (project.BasePath, SolutionProject.NormalizePath (item.Include));
				if (fullName == fileName) 
					return true;
			}
			return false;
		}
		
		public static bool IsDirectoryInProject (IProject project, string directoryName)
		{
			foreach (ProjectItem item in project.Items) {
				string fullName = Path.Combine (project.BasePath, SolutionProject.NormalizePath (item.Include));
				if (fullName.StartsWith(directoryName)) 
					return true;
			}
			return false;
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			DirectoryNode directoryNode = dataObject as DirectoryNode;
			if (directoryNode == null) 
				return;
			
			string basePath = directoryNode.Path;
			
			foreach (string fileName in Directory.GetFiles(basePath)) {
				bool isInProject = IsFileInProject(directoryNode.Project.Project, fileName);
				
				if (ProjectSolutionPad.Instance.ShowAllFiles || isInProject) { 
					if (isInProject)
						ctx.AddChild (new FileNode (directoryNode.Project, fileName));
					else
						ctx.AddChild (new SystemFileNode (directoryNode.Project, fileName));
				}
			}
			
			foreach (string directoryName in Directory.GetDirectories(basePath)) {
				bool isInProject = IsDirectoryInProject(directoryNode.Project.Project, directoryName);
				if (ProjectSolutionPad.Instance.ShowAllFiles || isInProject) 
					ctx.AddChild (new DirectoryNode (directoryNode.Project, directoryName, isInProject));
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			DirectoryNode directoryNode = dataObject as DirectoryNode;
			if (directoryNode == null) 
				return false;
			string basePath = directoryNode.Path;
			return Directory.GetFiles(basePath).Length > 0 || Directory.GetDirectories(basePath).Length > 0;
		}
	}
}

