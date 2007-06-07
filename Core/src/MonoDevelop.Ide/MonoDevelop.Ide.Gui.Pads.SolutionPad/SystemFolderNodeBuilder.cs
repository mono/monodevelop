//
// SystemFolderNodeBuilder.cs
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
using System.Collections.Generic;
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
	public class SystemFolderNodeBuilder : TypeNodeBuilder
	{
		public SystemFolderNodeBuilder ()
		{
		}

		public override Type NodeDataType {
			get { return typeof(SystemFolderNode); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(SystemFolderNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			SystemFolderNode systemFolderNode = dataObject as SystemFolderNode;
			if (systemFolderNode == null) 
				return "SystemFolderNode";
			
			return Path.GetFileName (systemFolderNode.Path);
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/SystemFolderNode"; }
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			SystemFolderNode systemFolderNode = dataObject as SystemFolderNode;
			if (systemFolderNode == null) 
				return;
			label = Path.GetFileName (systemFolderNode.Path);
			
			icon       = Context.GetIcon (Stock.OpenDashedFolder);
			closedIcon = Context.GetIcon (Stock.ClosedDashedFolder);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			SystemFolderNode systemFolderNode = dataObject as SystemFolderNode;
			if (systemFolderNode == null) 
				return;
			
			string basePath = systemFolderNode.Path;
			
			foreach (string fileName in Directory.GetFiles(basePath)) {
				ctx.AddChild (new SystemFileNode (systemFolderNode.Project, fileName));
			}
			
			foreach (string directoryName in Directory.GetDirectories(basePath)) {
				ctx.AddChild (new SystemFolderNode (systemFolderNode.Project, directoryName));
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			SystemFolderNode systemFolderNode = dataObject as SystemFolderNode;
			if (systemFolderNode == null) 
				return false;
			string basePath = systemFolderNode.Path;
			return Directory.GetFiles(basePath).Length > 0 || Directory.GetDirectories(basePath).Length > 0;
		}
	}
	
	public class SystemFolderNodeCommandHandler: NodeCommandHandler
	{
		[CommandHandler (ProjectCommands.IncludeToProject)]
		public void IncludeToProject ()
		{
			SystemFolderNode systemFolderNode = CurrentNode.DataItem as SystemFolderNode;
			if (systemFolderNode == null) 
				return;
			
			string[] fileNames = Directory.GetFiles (systemFolderNode.Path, "*", SearchOption.AllDirectories);
			foreach (string fileName in fileNames) {
				FileType fileType = FileType.Content;
				BackendBindingCodon codon = BackendBindingService.GetBackendBindingCodon (systemFolderNode.Project);
				if (codon != null && codon.IsCompileable (fileName))
					fileType = FileType.Compile;
				string relativePath = Runtime.FileService.AbsoluteToRelativePath (systemFolderNode.Project.Project.BasePath, fileName);
				if (relativePath.StartsWith ("." + Path.DirectorySeparatorChar))
					relativePath = relativePath.Substring (2);
				systemFolderNode.Project.Project.Add (new ProjectFile (relativePath, fileType));
			}
			List<string> directories = new List<string> ();
			directories.Add (systemFolderNode.Path);
			directories.AddRange (Directory.GetDirectories (systemFolderNode.Path, "*", SearchOption.AllDirectories));
			foreach (string directory in directories) {
				string[] subFiles = Directory.GetFiles (directory);
				if (subFiles == null || subFiles.Length == 0) {
					string relativePath = Runtime.FileService.AbsoluteToRelativePath (systemFolderNode.Project.Project.BasePath, directory);
					if (relativePath.StartsWith ("." + Path.DirectorySeparatorChar))
						relativePath = relativePath.Substring (2);
					systemFolderNode.Project.Project.Add (new ProjectFile (relativePath, FileType.Folder));
				}
			}
			
			ProjectService.SaveProject (systemFolderNode.Project.Project);
		}
	}
}