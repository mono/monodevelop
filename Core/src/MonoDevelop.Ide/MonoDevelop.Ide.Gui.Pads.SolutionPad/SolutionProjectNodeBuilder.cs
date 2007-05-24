//
// SolutionProjectNodeBuilder.cs
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
		
/*		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/CombineBrowserNode"; }
		}*/
		
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
				
				Console.WriteLine (BackendBindingService.GetBackendBindingCodonByGuid (solutionProject.TypeGuid).Id);
				
				icon = Context.GetIcon (Services.Icons.GetImageForProjectType (BackendBindingService.GetBackendBindingCodonByGuid (solutionProject.TypeGuid).Id));
			}
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			SolutionProject solutionProject = dataObject as SolutionProject;
			if (solutionProject == null) 
				return;
			
			string basePath = solutionProject.Project.BasePath;
			ctx.AddChild (new ReferenceFolderNode (solutionProject));
			
			foreach (string fileName in Directory.GetFiles(basePath)) {
				bool isInProject = DirectoryNodeBuilder.IsFileInProject(solutionProject.Project, fileName);
				
				if (ProjectSolutionPad.Instance.ShowAllFiles || isInProject) {
					if (isInProject)
						ctx.AddChild (new FileNode (solutionProject, fileName));
					else
						ctx.AddChild (new SystemFileNode (solutionProject, fileName));
				}
			}
			
			foreach (string directoryName in Directory.GetDirectories(basePath)) {
				bool isInProject = DirectoryNodeBuilder.IsDirectoryInProject(solutionProject.Project, directoryName);
				if (ProjectSolutionPad.Instance.ShowAllFiles || isInProject) 
					ctx.AddChild (new DirectoryNode (solutionProject, directoryName, isInProject));
			}
			
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			SolutionProject solutionProject = dataObject as SolutionProject;
			if (solutionProject == null) 
				return false;
			return solutionProject.Project != null && solutionProject.Project.Items.Count > 0;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
/*			combine.EntryAdded += combineEntryAdded;
			combine.EntryRemoved += combineEntryRemoved;
			combine.NameChanged += combineNameChanged;
			combine.StartupPropertyChanged += startupChanged;*/
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
/*			combine.EntryAdded -= combineEntryAdded;
			combine.EntryRemoved -= combineEntryRemoved;
			combine.NameChanged -= combineNameChanged;
			combine.StartupPropertyChanged -= startupChanged;*/
		}
	}
}
