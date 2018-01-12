//
// ShowAllFilesBuilderExtension.cs
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
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class ShowAllFilesBuilderExtension: NodeBuilderExtension
	{
		List<Project> projects = new List<Project> ();
		
		public override bool CanBuildNode (Type dataType)
		{
			if (typeof(SolutionFolder).IsAssignableFrom (dataType))
				return false;

			return typeof(IFolderItem).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ShowAllFilesCommandHandler); }
		}
		
		protected override void Initialize ()
		{
			base.Initialize ();

			IdeApp.Workspace.FileAddedToProject += OnAddFile;
			IdeApp.Workspace.FileRemovedFromProject += OnRemoveFile;
			
			FileService.FileRenamed += OnSystemFileRenamed;
			FileService.FileRemoved += OnSystemFileDeleted;
			FileService.FileCreated += OnSystemFileAdded;
		}
		
		public override void Dispose ()
		{
			IdeApp.Workspace.FileAddedToProject -= OnAddFile;
			IdeApp.Workspace.FileRemovedFromProject -= OnRemoveFile;
			FileService.FileRenamed -= OnSystemFileRenamed;
			FileService.FileRemoved -= OnSystemFileDeleted;
			FileService.FileCreated -= OnSystemFileAdded;

			base.Dispose ();
		}

		public override void OnNodeAdded (object dataObject)
		{
			base.OnNodeAdded (dataObject);
			Project p = dataObject as Project;
			if (p != null)
				projects.Add (p);
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			base.OnNodeRemoved (dataObject);
			Project p = dataObject as Project;
			if (p != null)
				projects.Remove (p);
		}
		
		internal static string GetFolderPath (object dataObject)
		{
			return ((IFolderItem)dataObject).BaseDirectory;
		}
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, NodeInfo nodeInfo)
		{
			if (!builder.Options ["ShowAllFiles"])
				return;

			string thisPath = GetFolderPath (dataObject);
			
			if ((dataObject is ProjectFolder) && Directory.Exists (thisPath))
			{
				ProjectFolder pf = (ProjectFolder) dataObject;
				if (pf.Project == null || !ProjectFolderCommandHandler.PathExistsInProject (pf.Project, thisPath)) {
					var gicon = Context.GetComposedIcon (nodeInfo.Icon, "fade");
					if (gicon == null) {
						gicon = nodeInfo.Icon.WithAlpha (0.5);
						Context.CacheComposedIcon (nodeInfo.Icon, "fade", gicon);
					}
					nodeInfo.Icon = gicon;
					gicon = Context.GetComposedIcon (nodeInfo.ClosedIcon, "fade");
					if (gicon == null) {
						gicon = nodeInfo.ClosedIcon.WithAlpha (0.5);
						Context.CacheComposedIcon (nodeInfo.ClosedIcon, "fade", gicon);
					}
					nodeInfo.ClosedIcon = gicon;
				}
			}
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (!builder.Options ["ShowAllFiles"])
				return;

			string path = GetFolderPath (dataObject);
			if (Directory.Exists (path))
			{
				Project project = (Project) builder.GetParentDataItem (typeof(Project), true);
				SolutionFolderFileCollection folderFiles = null;
				if (dataObject is Solution)
					folderFiles = ((Solution)dataObject).RootFolder.Files;
				else if (dataObject is SolutionFolder)
					folderFiles = ((SolutionFolder)dataObject).Files;

				builder.AddChildren (Directory.EnumerateFiles (path)
									 .Where (file => (project == null || project.Files.GetFile (file) == null) && (folderFiles == null || !folderFiles.Contains (file)))
									 .Select (file => new SystemFile (file, project)));

				builder.AddChildren (Directory.EnumerateDirectories (path)
									 .Where (folder => !builder.HasChild (Path.GetFileName (folder), typeof (ProjectFolder)))
									 .Select (folder => new ProjectFolder (folder, project)));
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (builder.Options ["ShowAllFiles"]) {
				string path = GetFolderPath (dataObject);
				return Directory.Exists (path) && (Directory.EnumerateFileSystemEntries (path).Any ());
			}
			else
				return false;
		}
		
		void OnAddFile (object sender, ProjectFileEventArgs args)
		{
			foreach (ProjectFileEventInfo e in args) {
				if (!e.ProjectFile.IsLink) {
					object target;
					if (e.ProjectFile.Subtype == Subtype.Directory) {
						target = new ProjectFolder (e.ProjectFile.FilePath, e.Project);
					} else {
						ITreeBuilder tb = Context.GetTreeBuilder (new SystemFile (e.ProjectFile.Name, e.Project));
						if (tb != null) tb.Remove (true);
						target = e.ProjectFile;
					}
					Context.Tree.AddNodeInsertCallback (target, new TreeNodeCallback (UpdateProjectFileParent));
				}
			}
		}
		
		void UpdateProjectFileParent (ITreeNavigator nav)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (nav);
			if (!(tb.DataItem is ProjectFolder))
				tb.MoveToParent ();
			while (tb.DataItem is ProjectFolder) {
				tb.Update ();
				tb.MoveToParent ();
			}
		}
		
		void OnRemoveFile (object sender, ProjectFileEventArgs args)
		{
			foreach (ProjectFileEventInfo e in args) {
				if (e.ProjectFile.Subtype != Subtype.Directory && 
					!e.ProjectFile.IsLink &&
					File.Exists (e.ProjectFile.Name)
				) {
					AddFile (e.ProjectFile.Name, e.Project);
				}
			}
		}
		
		void AddFile (string file, Project project)
		{
			if (!file.StartsWith (project.BaseDirectory))
				return;

			ITreeBuilder tb = Context.GetTreeBuilder ();
			string filePath = Path.GetDirectoryName (file);
			
			object data = new SystemFile (file, project);
				
			// Already there?
			if (tb.MoveToObject (data))
				return;
			
			if (filePath != project.BaseDirectory) {
				if (tb.MoveToObject (new ProjectFolder (filePath, project))) {
					if (tb.Filled && tb.Options ["ShowAllFiles"])
						tb.AddChild (data);
				} else {
					// Make sure there is a path to that folder
					EnsureReachable (project, file);
				}
			} else {
				if (tb.MoveToObject (project) && tb.Options ["ShowAllFiles"])
					tb.AddChild (data);
			}
		}
		
		ITreeBuilder FindParentFolderNode (string path, Project project, out string lastChildPath)
		{
			lastChildPath = path;
			string basePath = Path.GetDirectoryName (path);
			
			if (string.IsNullOrEmpty (basePath))
				return null;

			if (basePath == project.BaseDirectory)
				return Context.GetTreeBuilder (project);
				
			ITreeBuilder tb = Context.GetTreeBuilder (new ProjectFolder (basePath, project));
			if (tb != null) return tb;
			
			return FindParentFolderNode (basePath, project, out lastChildPath);
		}
		
		void OnSystemFileAdded (object sender, FileEventArgs args)
		{
			foreach (FileEventInfo e in args) {
				Project project = GetProjectForFile (e.FileName);
				if (project == null) return;
				
				if (e.IsDirectory) {
					EnsureReachable (project, e.FileName + "/");
				} else {
					if (project.Files.GetFile (e.FileName) == null)
						AddFile (e.FileName, project);
				}
			}
		}
		
		void OnSystemFileDeleted (object sender, FileEventArgs args)
		{
			foreach (FileEventInfo e in args) {
				Project project = GetProjectForFile (e.FileName);

				ITreeBuilder tb = Context.GetTreeBuilder ();
				
				if (e.IsDirectory) {
					if (tb.MoveToObject (new ProjectFolder (e.FileName, project))) {
						if (tb.Options ["ShowAllFiles"] && (project == null || !ProjectFolderCommandHandler.PathExistsInProject (project, e.FileName))) {
							tb.Remove ();
							return;
						}
					}
				}
				else {
					if (tb.MoveToObject (new SystemFile (e.FileName, project))) {
						tb.Remove ();
						return;
					}
				}
				
				// Find the parent folder, and update it's children count
				
				string parentPath = Path.GetDirectoryName (e.FileName);
				if (tb.MoveToObject (new ProjectFolder (parentPath, project))) {
					if (tb.Options ["ShowAllFiles"] && Directory.Exists (parentPath))
						tb.UpdateChildren ();
				}
			}
		}
		
		void OnSystemFileRenamed (object sender, FileCopyEventArgs args)
		{
			foreach (FileCopyEventInfo e in args) {
				Project project = GetProjectForFile (e.SourceFile);
				if (project == null) return;
				
				if (e.IsDirectory) {
	/*				string childPath;
					ITreeBuilder tb = FindParentFolderNode (e.SourceFile, project, out childPath);
					if (tb != null && tb.Options ["ShowAllFiles"]) {
						tb.UpdateAll ();
					}
	*/
				} else {
					ITreeBuilder tb = Context.GetTreeBuilder (new SystemFile (e.SourceFile, project));
					if (tb != null) {
						tb.Remove (true);
						tb.AddChild (new SystemFile (e.TargetFile, project));
					}
				}
			}
		}
		
		void EnsureReachable (Project project, string path)
		{
			string childPath;
			ITreeBuilder tb = FindParentFolderNode (path, project, out childPath);
			if (tb != null && childPath != path && tb.Options ["ShowAllFiles"]) {
				tb.AddChild (new ProjectFolder (childPath, project));
			}
		}
		
		Project GetProjectForFile (string path)
		{
			string baseDir = path + Path.DirectorySeparatorChar;
			foreach (Project p in projects) {
				if (baseDir.StartsWith (p.BaseDirectory + Path.DirectorySeparatorChar))
					return p;
			}
			return null;
		}
	}
	
	class ShowAllFilesCommandHandler: NodeCommandHandler
	{
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			if (dataObject is SystemFile) {
				FilePath targetPath = ShowAllFilesBuilderExtension.GetFolderPath (CurrentNode.DataItem);
				return ((SystemFile)dataObject).Path.ParentDirectory != targetPath || operation == DragOperation.Copy;
			}
			return false;
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			FilePath targetPath = ShowAllFilesBuilderExtension.GetFolderPath (CurrentNode.DataItem);
			Project targetProject = (Project) CurrentNode.GetParentDataItem (typeof(Project), true);
			FilePath source = ((SystemFile)dataObject).Path;
			targetPath = targetPath.Combine (source.FileName);
			if (targetPath == source)
				targetPath = ProjectOperations.GetTargetCopyName (targetPath, false);
			
			using (ProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString("Copying files..."), Stock.StatusWorking, true))
			{
				bool move = operation == DragOperation.Move;
				IdeApp.ProjectOperations.TransferFiles (monitor, null, source, targetProject, targetPath, move, false);
			}
		}
	}
}
