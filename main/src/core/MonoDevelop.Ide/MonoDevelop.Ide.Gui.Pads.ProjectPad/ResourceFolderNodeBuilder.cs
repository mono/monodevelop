//
// ResourceFolderNodeBuilder.cs
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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ResourceFolderNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ResourceFolder); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			// Don't localize this string.
			return "Resources";
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ResourceFolderNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ProjectPad/ResourceFolder"; }
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("Resources");
			icon = Context.GetIcon (Stock.OpenResourceFolder);
			closedIcon = Context.GetIcon (Stock.ClosedResourceFolder);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = ((ResourceFolder)dataObject).Project;
			foreach (ProjectFile file in project.Files)
				if (file.BuildAction == BuildAction.EmbedAsResource)
					return true;
			return false;
		}

		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is ProjectReferenceCollection)
				return 1;
			else
				return -1;
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = ((ResourceFolder)dataObject).Project;
			foreach (ProjectFile file in project.Files)
				if (file.BuildAction == BuildAction.EmbedAsResource)
					builder.AddChild (file);
		}
	}
	
	public class ResourceFolderNodeCommandHandler: NodeCommandHandler
	{
		[CommandHandler (ProjectCommands.AddResource)]
		public void AddResourceToProject ()
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			if (project == null) return;
			
			string [] files;
			do {
				files = AskFiles (project);
				if (files == null) return;
			}
			while (!CheckFiles (files));
			
			CurrentNode.Expanded = true;
		
			foreach (string fileName in files) {
				ProjectFile file = project.Files.GetFile (fileName);
				if (file != null)
					file.BuildAction = BuildAction.EmbedAsResource;
				else
					project.AddFile (fileName, BuildAction.EmbedAsResource);
			}
			IdeApp.ProjectOperations.Save (project);
		}
		
		string[] AskFiles (Project project)
		{
			FileSelector fs = new FileSelector (GettextCatalog.GetString ("Add Resource"));
			try {
				fs.SelectMultiple = true;
				fs.SetCurrentFolder (project.BaseDirectory);
				int response = fs.Run ();
				string [] files = fs.Filenames;
				fs.Hide ();

				if (response != (int)Gtk.ResponseType.Ok)
					return null;
				else
					return files;
			} finally {
				if (fs != null)
					fs.Destroy ();
			}
		}
		
		bool CheckFiles (string[] files)
		{
			foreach (string file in files) {
				if (!System.IO.File.Exists (file)) {
					MessageService.ShowError (GettextCatalog.GetString ("Resource file '{0}' does not exist", file));
					return false;
				}
			}
			return true;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			ProjectFile file = dataObject as ProjectFile;
			return file != null && file.Project == project;
		}

		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			ProjectFile file = dataObject as ProjectFile;
			file.BuildAction = BuildAction.EmbedAsResource;
		}
	}
}
