//
// SolutionProjectNodeBuilder.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.IO;

using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Projects.Gui.ProjectOptions;

namespace MonoDevelop.Ide.Gui.Pads.SolutionViewPad
{
	public class SolutionProjectNodeBuilder : TypeNodeBuilder
	{
		public SolutionProjectNodeBuilder ()
		{
		}

		public override Type NodeDataType {
			get { return typeof(SolutionProject); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			SolutionProject solutionProject = dataObject as SolutionProject;
			if (solutionProject == null) 
				return "SolutionProject";
			
			return solutionProject.Name;
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		public override Type CommandHandlerType {
			get { return typeof(SolutionProjectNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/ProjectBrowserNode"; }
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			SolutionProject solutionProject = dataObject as SolutionProject;
			if (solutionProject == null) 
				return;
			if (solutionProject.Project is UnknownProject) {
				icon = Context.GetIcon (Stock.Error);
				label = String.Format (GettextCatalog.GetString ("Error loading {0}: Project type guid unknown {1})."), solutionProject.Name, solutionProject.TypeGuid);
			} else {
				label = solutionProject.Name;
				icon = Context.GetIcon (Services.Icons.GetImageForProjectType (BackendBindingService.GetBackendBindingCodonByGuid (solutionProject.TypeGuid).Id));
			}
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			SolutionProject solutionProject = dataObject as SolutionProject;
			if (solutionProject == null) 
				return;
			
			string basePath = solutionProject.Project.BasePath;
			Console.WriteLine ("base path: " + basePath);
			
			ctx.AddChild (new ReferenceFolderNode (solutionProject));
			
			foreach (string fileName in Directory.GetFiles(basePath)) {
				bool isInProject = FolderNodeBuilder.IsFileInProject(solutionProject.Project, fileName);
				
				if (ProjectSolutionPad.Instance.ShowAllFiles || isInProject) {
					if (isInProject)
						ctx.AddChild (new FileNode (solutionProject, fileName));
					else
						ctx.AddChild (new SystemFileNode (solutionProject, fileName));
				}
			}
			
			foreach (string directoryName in Directory.GetDirectories(basePath)) {
				bool isInProject = FolderNodeBuilder.IsDirectoryInProject(solutionProject.Project, directoryName);
				if (ProjectSolutionPad.Instance.ShowAllFiles || isInProject) { 
					if (isInProject)
						ctx.AddChild (new FolderNode (solutionProject, directoryName));
					else
						ctx.AddChild (new SystemFolderNode (solutionProject, directoryName));
				}
			}
			
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			SolutionProject solutionProject = dataObject as SolutionProject;
			if (solutionProject == null) 
				return false;
			return solutionProject.Project != null && solutionProject.Project.Items.Count > 0;
		}
	}
	
	public class SolutionProjectNodeCommandHandler : FolderNodeCommandHandler
	{
		protected override string GetPath(object dataItem)
		{
			SolutionProject solutionProject = CurrentNode.DataItem as SolutionProject;
			if (solutionProject == null) 
				return null;
			return solutionProject.Project.BasePath;
		}
		
		protected override SolutionProject GetProject(object dataItem)
		{
			SolutionProject solutionProject = CurrentNode.DataItem as SolutionProject;
			if (solutionProject == null) 
				return null;
			return solutionProject;
		}
		
		[CommandHandler (ProjectCommands.SetAsStartupProject)]
		public void SetAsStartupProject ()
		{
			SolutionProject solutionProject = CurrentNode.DataItem as SolutionProject;
			if (solutionProject == null) 
				return;
			// TODO
		}
		
		[CommandHandler (ProjectCommands.Options)]
		public void Options ()
		{
			SolutionProject solutionProject = CurrentNode.DataItem as SolutionProject;
			if (solutionProject == null) 
				return;
			using (ProjectOptionsDialog optionsDialog = new ProjectOptionsDialog (solutionProject.Project)) {
				optionsDialog.Run ();
			}
		}
	}
	
}
