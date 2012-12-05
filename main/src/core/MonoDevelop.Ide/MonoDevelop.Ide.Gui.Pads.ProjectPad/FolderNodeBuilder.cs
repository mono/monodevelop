//
// FolderNodeBuilder.cs
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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Collections;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Projects;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	abstract class FolderNodeBuilder: TypeNodeBuilder
	{
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public abstract string GetFolderPath (object dataObject);
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			string path = GetFolderPath (dataObject);
			
			Project project = builder.GetParentDataItem (typeof(Project), true) as Project;
			if (project == null)
				return;
			
			ProjectFileCollection files;
			ArrayList folders;
			GetFolderContent (project, path, out files, out folders);
			
			foreach (ProjectFile file in files)
				builder.AddChild (file);
			
			foreach (string folder in folders)
				builder.AddChild (new ProjectFolder (folder, project, dataObject));
		}
				
		void GetFolderContent (Project project, string folder, out ProjectFileCollection files, out ArrayList folders)
		{
			files = new ProjectFileCollection ();
			folders = new ArrayList ();
			string folderPrefix = folder + Path.DirectorySeparatorChar;
			
			foreach (ProjectFile file in project.Files)
			{
				string dir;

				if (file.Subtype != Subtype.Directory) {
					if (file.DependsOnFile != null)
						continue;
					
					dir = file.IsLink
						? project.BaseDirectory.Combine (file.ProjectVirtualPath).ParentDirectory
						: file.FilePath.ParentDirectory;
						
					if (dir == folder) {
						files.Add (file);
						continue;
					}
				} else
					dir = file.Name;
				
				// add the directory if it isn't already present
				if (dir.StartsWith (folderPrefix)) {
					int i = dir.IndexOf (Path.DirectorySeparatorChar, folderPrefix.Length);
					if (i != -1) dir = dir.Substring (0,i);
					if (!folders.Contains (dir))
						folders.Add (dir);
				}
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = builder.GetParentDataItem (typeof(Project), true) as Project;
			if (project == null)
				return false;
			
			// For big projects, a real HasChildNodes value is too slow to get
			if (project.Files.Count > 500)
				return true;

			ProjectFileCollection files;
			ArrayList folders;
			
			string path = GetFolderPath (dataObject);
			
			GetFolderContent (project, path, out files, out folders);

			if (files.Count > 0 || folders.Count > 0) return true;
			
			return false;
		}
	}
	
	abstract class FolderCommandHandler: NodeCommandHandler
	{
		// CommandHandlers are constantly re-created so it's not possible to cache data inside the instance
		// Since 'AddExistingFolder' can only be run from the UI thread anyway we can safely just make this static.
		static FilePath PreviousFolderPath {
			get; set;
		}

		public abstract string GetFolderPath (object dataObject);

		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			string targetDirectory = GetFolderPath (CurrentNode.DataItem);
			
			if (dataObject is ProjectFile) {
				ProjectFile file = (ProjectFile) dataObject;
				var srcDir = (file.Project != null && file.IsLink)
					? file.Project.BaseDirectory.Combine (file.ProjectVirtualPath)
					: file.FilePath.ParentDirectory;

				switch (operation) {
				case DragOperation.Move:
					// allow grouped files to be unlinked from their parent
					return srcDir != targetDirectory || file.DependsOnFile != null;
				case DragOperation.Copy:
					return true;
				default:
					return false;
				}
			}
			else if (dataObject is ProjectFolder) {
				return ((ProjectFolder)dataObject).Path != targetDirectory || operation == DragOperation.Copy;
			}
			else if (dataObject is Gtk.SelectionData) {
				SelectionData data = (SelectionData) dataObject;
				if (data.Type == "text/uri-list")
					return true;
			}
			return false;
		}
		
		public override void OnMultipleNodeDrop (object[] dataObjects, DragOperation operation)
		{
			var projectsToSave = new HashSet<SolutionEntityItem> ();
			var groupedFiles = new HashSet<ProjectFile> ();

			foreach (var pf in dataObjects.OfType<ProjectFile> ()) {
				foreach (var child in pf.DependentChildren)
					groupedFiles.Add (child);
			}

			foreach (object dataObject in dataObjects)
				DropNode (projectsToSave, dataObject, groupedFiles, operation);

			IdeApp.ProjectOperations.Save (projectsToSave);
		}
		
		void DropNode (HashSet<SolutionEntityItem> projectsToSave, object dataObject, HashSet<ProjectFile> groupedFiles, DragOperation operation)
		{
			FilePath targetDirectory = GetFolderPath (CurrentNode.DataItem);
			FilePath source;
			string what;
			Project targetProject = (Project) CurrentNode.GetParentDataItem (typeof(Project), true);
			Project sourceProject;
			System.Collections.Generic.IEnumerable<ProjectFile> groupedChildren = null;
			
			if (dataObject is ProjectFolder) {
				source = ((ProjectFolder) dataObject).Path;
				sourceProject = ((ProjectFolder) dataObject).Project;
				what = Path.GetFileName (source);
			}
			else if (dataObject is ProjectFile) {
				ProjectFile file = (ProjectFile) dataObject;

				// if this ProjectFile is one of the grouped files being pulled in by a parent being copied/moved, ignore it
				if (groupedFiles.Contains (file))
					return;

				if (file.DependsOnFile != null && operation == DragOperation.Move) {
					// unlink this file from its parent (since its parent is not being moved)
					file.DependsOn = null;

					// if moving a linked file into its containing folder, simply unlink it from its parent
					if (file.FilePath.ParentDirectory == targetDirectory) {
						projectsToSave.Add (targetProject);
						return;
					}
				}

				sourceProject = file.Project;
				if (sourceProject != null && file.IsLink) {
					source = sourceProject.BaseDirectory.Combine (file.ProjectVirtualPath);
				} else {
					source = file.FilePath;
				}
				groupedChildren = file.DependentChildren;
				what = null;
			}
			else if (dataObject is Gtk.SelectionData) {
				SelectionData data = (SelectionData) dataObject;
				if (data.Type != "text/uri-list")
					return;
				string sources = System.Text.Encoding.UTF8.GetString (data.Data);
				Console.WriteLine ("text/uri-list:\n{0}", sources);
				string[] files = sources.Split (new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
				for (int n=0; n<files.Length; n++) {
					Uri uri = new Uri (files[n]);
					if (uri.Scheme != "file")
						return;
					if (Directory.Exists (uri.LocalPath))
						return;
					files[n] = uri.LocalPath;
				}
				
				IdeApp.ProjectOperations.AddFilesToProject (targetProject, files, targetDirectory);
				projectsToSave.Add (targetProject);
				return;
			}
			else
				return;

			var targetPath = targetDirectory.Combine (source.FileName);
			// If copying to the same directory, make a copy with a different name
			if (targetPath == source)
				targetPath = ProjectOperations.GetTargetCopyName (targetPath, dataObject is ProjectFolder);

			var targetChildPaths = groupedChildren != null ? groupedChildren.Select (child => {
				var targetChildPath = targetDirectory.Combine (child.FilePath.FileName);

				if (targetChildPath == child.FilePath)
					targetChildPath = ProjectOperations.GetTargetCopyName (targetChildPath, false);

				return targetChildPath;
			}).ToList () : null;

			if (dataObject is ProjectFolder) {
				string q;
				if (operation == DragOperation.Move) {
					if (targetPath.ParentDirectory == targetProject.BaseDirectory)
						q = GettextCatalog.GetString ("Do you really want to move the folder '{0}' to the root folder of project '{1}'?", what, targetProject.Name);
					else
						q = GettextCatalog.GetString ("Do you really want to move the folder '{0}' to the folder '{1}'?", what, targetDirectory.FileName);
					if (!MessageService.Confirm (q, AlertButton.Move))
						return;
				}
				else {
					if (targetPath.ParentDirectory == targetProject.BaseDirectory)
						q = GettextCatalog.GetString ("Do you really want to copy the folder '{0}' to the root folder of project '{1}'?", what, targetProject.Name);
					else
						q = GettextCatalog.GetString ("Do you really want to copy the folder '{0}' to the folder '{1}'?", what, targetDirectory.FileName);
					if (!MessageService.Confirm (q, AlertButton.Copy))
						return;
				}
			} else if (dataObject is ProjectFile) {
				foreach (var file in new FilePath[] { targetPath }.Concat (targetChildPaths)) {
					if (File.Exists (file))
						if (!MessageService.Confirm (GettextCatalog.GetString ("The file '{0}' already exists. Do you want to overwrite it?", file.FileName), AlertButton.OverwriteFile))
							return;
				}
			}
			
			var filesToSave = new List<Document> ();
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.IsDirty && doc.IsFile) {
					if (doc.Name == source || doc.Name.StartsWith (source + Path.DirectorySeparatorChar)) {
						filesToSave.Add (doc);
					} else if (groupedChildren != null) {
						foreach (ProjectFile f in groupedChildren)
							if (doc.Name == f.Name)
								filesToSave.Add (doc);
					}
				}
			}
			
			if (filesToSave.Count > 0) {
				StringBuilder sb = new StringBuilder ();
				foreach (Document doc in filesToSave) {
					if (sb.Length > 0) sb.Append (",\n");
					sb.Append (Path.GetFileName (doc.Name));
				}
				
				string question;
				
				if (operation == DragOperation.Move) {
					if (filesToSave.Count == 1)
						question = GettextCatalog.GetString ("Do you want to save the file '{0}' before the move operation?", sb.ToString ());
					else
						question = GettextCatalog.GetString ("Do you want to save the following files before the move operation?\n\n{0}", sb.ToString ());
				} else {
					if (filesToSave.Count == 1)
						question = GettextCatalog.GetString ("Do you want to save the file '{0}' before the copy operation?", sb.ToString ());
					else
						question = GettextCatalog.GetString ("Do you want to save the following files before the copy operation?\n\n{0}", sb.ToString ());
				}
				AlertButton noSave = new AlertButton (GettextCatalog.GetString ("Don't Save"));
				AlertButton res = MessageService.AskQuestion (question, AlertButton.Cancel, noSave, AlertButton.Save);
				if (res == AlertButton.Cancel)
					return;
				else if (res == AlertButton.Save) { 
					try {
						foreach (Document doc in filesToSave) {
							doc.Save ();
						}
					} catch (Exception ex) {
						MessageService.ShowException (ex, GettextCatalog.GetString ("Save operation failed."));
						return;
					}
				}
			}

			if (operation == DragOperation.Move && sourceProject != null)
				projectsToSave.Add (sourceProject);
			if (targetProject != null)
				projectsToSave.Add (targetProject);

			bool move = operation == DragOperation.Move;
			var opText = move ? GettextCatalog.GetString ("Moving files...") : GettextCatalog.GetString ("Copying files...");
			
			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (opText, Stock.StatusSolutionOperation, true)) {
				// If we drag and drop a node in the treeview corresponding to a directory, do not move
				// the entire directory. We should only move the files which exist in the project. Otherwise
				// we will need a lot of hacks all over the code to prevent us from incorrectly moving version
				// control related files such as .svn directories

				// Note: if we are transferring a ProjectFile, this will copy/move the ProjectFile's DependentChildren as well.
				IdeApp.ProjectOperations.TransferFiles (monitor, sourceProject, source, targetProject, targetPath, move, true);
			}
		}
		
		[CommandHandler (ProjectCommands.AddFiles)]
		public void AddFilesToProject()
		{
			Project project = (Project) CurrentNode.GetParentDataItem (typeof(Project), true);
			var targetRoot = ((FilePath) GetFolderPath (CurrentNode.DataItem)).CanonicalPath;

			AddFileDialog fdiag  = new AddFileDialog (GettextCatalog.GetString ("Add files"));
			fdiag.CurrentFolder = !PreviousFolderPath.IsNullOrEmpty ? PreviousFolderPath : targetRoot;
			fdiag.SelectMultiple = true;
			fdiag.TransientFor = IdeApp.Workbench.RootWindow;
			fdiag.BuildActions = project.GetBuildActions ();	
			
			string overrideAction = null;
			
			if (!fdiag.Run ())
				return;
			PreviousFolderPath = fdiag.SelectedFiles.Select (f => f.FullPath.ParentDirectory).FirstOrDefault ();

			var files = fdiag.SelectedFiles;
			overrideAction = fdiag.OverrideAction;
			
			ProjectFolder folder = CurrentNode.GetParentDataItem (typeof(ProjectFolder), true) as ProjectFolder;
			FilePath baseDirectory = folder != null ? folder.Path : project.BaseDirectory;
			
			IdeApp.ProjectOperations.AddFilesToProject (project, files, baseDirectory, overrideAction);
			
			IdeApp.ProjectOperations.Save (project);
		}
		
		[CommandHandler (ProjectCommands.AddNewFiles)]
		public void AddNewFileToProject()
		{
			Project project = (Project) CurrentNode.GetParentDataItem (typeof(Project), true);
			IdeApp.ProjectOperations.CreateProjectFile (project, GetFolderPath (CurrentNode.DataItem));
			IdeApp.ProjectOperations.Save (project);
			CurrentNode.Expanded = true;
			if (IdeApp.Workbench.ActiveDocument != null)
				IdeApp.Workbench.ActiveDocument.Window.SelectWindow ();
		}
		
		void OnFileInserted (ITreeNavigator nav)
		{
			nav.Selected = true;
			Tree.StartLabelEdit ();
		}

		///<summary>Imports files and folders from a target folder into the current folder</summary>
		[CommandHandler (ProjectCommands.AddFilesFromFolder)]
		public void AddFilesFromFolder ()
		{
			var project = (Project) CurrentNode.GetParentDataItem (typeof(Project), true);
			var targetRoot = ((FilePath) GetFolderPath (CurrentNode.DataItem)).CanonicalPath;
			
			var ofdlg = new SelectFolderDialog (GettextCatalog.GetString ("Import From Folder")) {
				CurrentFolder = !PreviousFolderPath.IsNullOrEmpty ? PreviousFolderPath : targetRoot
			};
			if(!ofdlg.Run ())
				return;
			PreviousFolderPath = ofdlg.SelectedFile.CanonicalPath;
			if (!PreviousFolderPath.ParentDirectory.IsNullOrEmpty)
				PreviousFolderPath = PreviousFolderPath.ParentDirectory;

			var srcRoot = ofdlg.SelectedFile.CanonicalPath;
			var foundFiles = Directory.GetFiles (srcRoot, "*", SearchOption.AllDirectories);
			
			var impdlg = new IncludeNewFilesDialog (GettextCatalog.GetString ("Select files to add from {0}", srcRoot.FileName), srcRoot);
			impdlg.AddFiles (foundFiles);
			if (MessageService.ShowCustomDialog (impdlg) != (int) ResponseType.Ok)
				return;
				
			var srcFiles = impdlg.SelectedFiles;
			var targetFiles = srcFiles.Select (f => targetRoot.Combine (f.ToRelative (srcRoot)));

			var added = IdeApp.ProjectOperations.AddFilesToProject (project, srcFiles.ToArray (), targetFiles.ToArray (), null).Any ();
			if (added)
				IdeApp.ProjectOperations.Save (project);
		}

		///<summary>Adds an existing folder to the current folder</summary>
		[CommandHandler (ProjectCommands.AddExistingFolder)]
		public void AddExistingFolder ()
		{
			var project = (Project) CurrentNode.GetParentDataItem (typeof(Project), true);
			var selectedFolder = ((FilePath) GetFolderPath (CurrentNode.DataItem)).CanonicalPath;
			
			var ofdlg = new SelectFolderDialog (GettextCatalog.GetString ("Add Existing Folder")) {
				CurrentFolder = !PreviousFolderPath.IsNullOrEmpty ? PreviousFolderPath : selectedFolder 
			};
			if(!ofdlg.Run ())
				return;

			// We store the parent directory of the folder the user chooses as they will not need to add the same
			// directory twice. We can save them navigating up one directory by doing it for them
			PreviousFolderPath = ofdlg.SelectedFile.CanonicalPath;
			if (!PreviousFolderPath.ParentDirectory.IsNullOrEmpty)
				PreviousFolderPath = PreviousFolderPath.ParentDirectory;

			var srcRoot = ofdlg.SelectedFile.CanonicalPath;
			var targetRoot = selectedFolder.Combine (srcRoot.FileName);

			bool changedProject = false;

			if (File.Exists (targetRoot)) {
				MessageService.ShowWarning (GettextCatalog.GetString (
					"There is already a file with the name '{0}' in the target directory", srcRoot.FileName));
				return;
			}

			var existingPf = project.Files.GetFileWithVirtualPath (targetRoot.ToRelative (project.BaseDirectory));
			if (existingPf != null) {
				if (existingPf.Subtype != Subtype.Directory) {
					MessageService.ShowWarning (GettextCatalog.GetString (
						"There is already a link with the name '{0}' in the target directory", srcRoot.FileName));
					return;
				}
			} else {
				project.Files.Add (new ProjectFile (targetRoot) { Subtype = Subtype.Directory });
				changedProject = true;
			}

			var foundFiles = Directory.GetFiles (srcRoot, "*", SearchOption.AllDirectories);
			
			var impdlg = new IncludeNewFilesDialog (GettextCatalog.GetString ("Select files to add from {0}", srcRoot.FileName), srcRoot.ParentDirectory);
			impdlg.AddFiles (foundFiles);
			if (MessageService.ShowCustomDialog (impdlg) == (int) ResponseType.Ok) {
				var srcFiles = impdlg.SelectedFiles;
				var targetFiles = srcFiles.Select (f => targetRoot.Combine (f.ToRelative (srcRoot)));
				if (IdeApp.ProjectOperations.AddFilesToProject (project, srcFiles.ToArray (), targetFiles.ToArray (), null).Any ())
					changedProject = true;
			}
			
			if (changedProject)
				IdeApp.ProjectOperations.Save (project);
		}
		
		[CommandHandler (ProjectCommands.NewFolder)]
		public void AddNewFolder ()
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			
			string baseFolderPath = GetFolderPath (CurrentNode.DataItem);
			string directoryName = Path.Combine (baseFolderPath, GettextCatalog.GetString("New Folder"));
			int index = -1;

			if (Directory.Exists(directoryName)) {
				while (Directory.Exists(directoryName + (++index + 1))) ;
			}
			
			if (index >= 0) {
				directoryName += index + 1;
			}
			
			Directory.CreateDirectory (directoryName);
			
			ProjectFile newFolder = new ProjectFile (directoryName);
			newFolder.Subtype = Subtype.Directory;
			project.Files.Add (newFolder);
			IdeApp.ProjectOperations.Save (project);

			CurrentNode.Expanded = true;
			Tree.AddNodeInsertCallback (new ProjectFolder (directoryName, project), new TreeNodeCallback (OnFileInserted));
		}
	}	
}
