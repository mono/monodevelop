//
// SystemFileNodeBuilder.cs
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
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Utils;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Projects.Item;

namespace MonoDevelop.Ide.Gui.Pads.SolutionViewPad
{
	public class SystemFileNodeBuilder : TypeNodeBuilder
	{
		public SystemFileNodeBuilder ()
		{
		}

		public override Type NodeDataType {
			get { return typeof(SystemFileNode); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/SystemFileNode"; }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(SystemFileNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			SystemFileNode systemFileNode = dataObject as SystemFileNode;
			if (systemFileNode == null) 
				return "FileNode";
			
			return Path.GetFileName (systemFileNode.FileName);
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			SystemFileNode systemFileNode = dataObject as SystemFileNode;
			if (systemFileNode == null) 
				return;
			label = Path.GetFileName (systemFileNode.FileName);
			icon  = Context.GetIcon (Stock.DashedFileIcon);
		}
	}
	
	public class SystemFileNodeCommandHandler: FileNodeCommandHandler
	{
		public static FileType GetFileType (string fileName, SolutionProject project)
		{
			BackendBindingCodon codon = BackendBindingService.GetBackendBindingCodon (project);
			if (codon != null && codon.IsCompileable (fileName))
				return FileType.Compile;
			return FileType.Content;
		}
		
		[CommandHandler (ProjectCommands.IncludeToProject)]
		public void IncludeToProject ()
		{
			SystemFileNode systemFileNode = CurrentNode.DataItem as SystemFileNode;
			if (systemFileNode == null) 
				return;
			string relativePath = Runtime.FileService.AbsoluteToRelativePath (systemFileNode.Project.Project.BasePath, systemFileNode.FileName);
			if (relativePath.StartsWith ("." + Path.DirectorySeparatorChar))
				relativePath = relativePath.Substring (2);
			systemFileNode.Project.Project.Items.Add (new ProjectFile (relativePath, GetFileType (systemFileNode.FileName, systemFileNode.Project)));
			ProjectService.SaveProject (systemFileNode.Project.Project);
		}
	}
}

