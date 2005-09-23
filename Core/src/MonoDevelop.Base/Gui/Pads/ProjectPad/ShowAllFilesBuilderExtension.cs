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

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;

namespace MonoDevelop.Gui.Pads.ProjectPad
{
	public class ShowAllFilesBuilderExtension: NodeBuilderExtension
	{
		ProjectFileEventHandler fileAddedHandler;
		ProjectFileEventHandler fileRemovedHandler;
		
		FileEventHandler createdHandler;
		FileEventHandler deletedHandler;
		FileEventHandler renamedHandler;
		
		ArrayList projects = new ArrayList ();
		
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectFolder).IsAssignableFrom (dataType) ||
					typeof(Project).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ShowAllFilesCommandHandler); }
		}
		
		protected override void Initialize ()
		{
			fileAddedHandler = (ProjectFileEventHandler) Runtime.DispatchService.GuiDispatch (new ProjectFileEventHandler (OnAddFile));
			fileRemovedHandler = (ProjectFileEventHandler) Runtime.DispatchService.GuiDispatch (new ProjectFileEventHandler (OnRemoveFile));
			
			createdHandler = (FileEventHandler) Runtime.DispatchService.GuiDispatch (new FileEventHandler (OnSystemFileAdded));
			deletedHandler = (FileEventHandler) Runtime.DispatchService.GuiDispatch (new FileEventHandler (OnSystemFileDeleted));
			renamedHandler = (FileEventHandler) Runtime.DispatchService.GuiDispatch (new FileEventHandler (OnSystemFileRenamed));
			
			Runtime.ProjectService.FileAddedToProject += fileAddedHandler;
			Runtime.ProjectService.FileRemovedFromProject += fileRemovedHandler;
			
			Runtime.FileService.FileRenamed += renamedHandler;
			Runtime.FileService.FileRemoved += deletedHandler;
			Runtime.FileService.FileCreated += createdHandler;
		}
		
		public override void Dispose ()
		{
			Runtime.ProjectService.FileAddedToProject -= fileAddedHandler;
			Runtime.ProjectService.FileRemovedFromProject -= fileRemovedHandler;
			Runtime.FileService.FileRenamed -= renamedHandler;
			Runtime.FileService.FileRemoved -= deletedHandler;
			Runtime.FileService.FileCreated -= createdHandler;
		}

		public override void OnNodeAdded (object dataObject)
		{
			base.OnNodeAdded (dataObject);
			if (dataObject is Project)
				projects.Add (dataObject);
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			base.OnNodeRemoved (dataObject);
			if (dataObject is Project)
				projects.Remove (dataObject);
		}
		
		internal static string GetFolderPath (object dataObject)
		{
			if (dataObject is Project) return ((Project)dataObject).BaseDirectory;
			else return ((ProjectFolder)dataObject).Path;
		}
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			string thisPath = GetFolderPath (dataObject);
			
			if ((dataObject is ProjectFolder) && builder.Options ["ShowAllFiles"] && Directory.Exists (thisPath))
			{
				ProjectFolder pf = (ProjectFolder) dataObject;
				if (!HasProjectFiles (pf.Project, thisPath)) {
					Gdk.Pixbuf gicon = Context.GetComposedIcon (icon, "fade");
					if (gicon == null) {
						gicon = Runtime.Gui.Icons.MakeTransparent (icon, 0.5);
						Context.CacheComposedIcon (icon, "fade", gicon);
					}
					icon = gicon;
					gicon = Context.GetComposedIcon (closedIcon, "fade");
					if (gicon == null) {
						gicon = Runtime.Gui.Icons.MakeTransparent (closedIcon, 0.5);
						Context.CacheComposedIcon (closedIcon, "fade", gicon);
					}
					closedIcon = gicon;
				}
			}
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			string path = GetFolderPath (dataObject);
			if (builder.Options ["ShowAllFiles"] && Directory.Exists (path))
			{
				Project project = (Project) builder.GetParentDataItem (typeof(Project), true);
				
				foreach (string file in Directory.GetFiles (path)) {
					if (project.ProjectFiles.GetFile (file) == null)
						builder.AddChild (new SystemFile (file, project));
				}
				
				foreach (string folder in Directory.GetDirectories (path))
					if (!builder.HasChild (Path.GetFileName (folder), typeof(ProjectFolder)))
						builder.AddChild (new ProjectFolder (folder, project));
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (builder.Options ["ShowAllFiles"]) {
				string path = GetFolderPath (dataObject);
				return Directory.Exists (path) && (Directory.GetFiles (path).Length > 0 || Directory.GetDirectories (path).Length > 0);
			}
			else
				return false;
		}
		
		void OnAddFile (object sender, ProjectFileEventArgs e)
		{
			if (e.ProjectFile.BuildAction != BuildAction.EmbedAsResource) {
				ITreeBuilder tb = Context.GetTreeBuilder (new SystemFile (e.ProjectFile.Name, e.Project));
				if (tb != null) tb.Remove (true);
				Context.Tree.AddNodeInsertCallback (e.ProjectFile, new TreeNodeCallback (UpdateProjectFileParent));
			}
		}
		
		void UpdateProjectFileParent (ITreeNavigator nav)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (nav);
			tb.MoveToParent ();
			while (tb.DataItem is ProjectFolder) {
				tb.Update ();
				tb.MoveToParent ();
			}
		}
		
		void OnRemoveFile (object sender, ProjectFileEventArgs e)
		{
			if (e.ProjectFile.BuildAction != BuildAction.EmbedAsResource && e.ProjectFile.Subtype != Subtype.Directory) {
				AddFile (e.ProjectFile.Name, e.Project);
			}
		}
		
		void AddFile (string file, Project project)
		{
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
			
			if (basePath == project.BaseDirectory)
				return Context.GetTreeBuilder (project);
				
			ITreeBuilder tb = Context.GetTreeBuilder (new ProjectFolder (basePath, project));
			if (tb != null) return tb;
			
			return FindParentFolderNode (basePath, project, out lastChildPath);
		}
		
		void OnSystemFileAdded (object sender, FileEventArgs e)
		{
			Project project = GetProjectForFile (e.FileName);
			if (project == null) return;
			
			if (e.IsDirectory) {
				EnsureReachable (project, e.FileName + "/");
			} else {
				if (project.ProjectFiles.GetFile (e.FileName) == null)
					AddFile (e.FileName, project);
			}
		}
		
		void OnSystemFileDeleted (object sender, FileEventArgs e)
		{
			Project project = GetProjectForFile (e.FileName);
			if (project == null) return;
			
			ITreeBuilder tb = Context.GetTreeBuilder ();
			
			if (e.IsDirectory) {
				if (tb.MoveToObject (new ProjectFolder (e.FileName, project))) {
					if (tb.Options ["ShowAllFiles"] && !HasProjectFiles (project, e.FileName)) {
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
		
		void OnSystemFileRenamed (object sender, FileEventArgs e)
		{
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
		
		void EnsureReachable (Project project, string path)
		{
			string childPath;
			ITreeBuilder tb = FindParentFolderNode (path, project, out childPath);
			if (tb != null && childPath != path && tb.Options ["ShowAllFiles"]) {
				tb.AddChild (new ProjectFolder (childPath, project));
			}
		}
		
		bool HasProjectFiles (Project project, string path)
		{
			string basePath = path + Path.DirectorySeparatorChar;
			foreach (ProjectFile f in project.ProjectFiles)
				if (f.Name.StartsWith (basePath))
					return true;
			return false;
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
	
	public class ShowAllFilesCommandHandler: NodeCommandHandler
	{
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			if (dataObject is SystemFile) {
				string targetPath = ShowAllFilesBuilderExtension.GetFolderPath (CurrentNode.DataItem);
				return Path.GetDirectoryName (((SystemFile)dataObject).Path) != targetPath;
			}
			return false;
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			string targetPath = ShowAllFilesBuilderExtension.GetFolderPath (CurrentNode.DataItem);
			Project targetProject = (Project) CurrentNode.GetParentDataItem (typeof(Project), true);
			string source = ((SystemFile)dataObject).Path;
			
			using (IProgressMonitor monitor = Runtime.TaskService.GetStatusProgressMonitor (GettextCatalog.GetString("Copying files ..."), Stock.CopyIcon, true))
			{
				bool move = operation == DragOperation.Move;
				Runtime.ProjectService.TransferFiles (monitor, null, source, targetProject, targetPath, move, false);
			}
		}
	}
}
