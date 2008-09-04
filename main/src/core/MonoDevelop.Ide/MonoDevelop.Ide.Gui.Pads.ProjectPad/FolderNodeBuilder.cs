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
using System.Collections;
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public abstract class FolderNodeBuilder: TypeNodeBuilder
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
					dir = Path.GetDirectoryName (file.Name);
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
	
	public abstract class FolderCommandHandler: NodeCommandHandler
	{
		public abstract string GetFolderPath (object dataObject);

		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			string targetPath = GetFolderPath (CurrentNode.DataItem);
			
			if (dataObject is ProjectFile) {
				ProjectFile file = (ProjectFile) dataObject;
				return Path.GetDirectoryName (file.Name) != targetPath && file.DependsOnFile == null;
			} if (dataObject is ProjectFolder) {
				return ((ProjectFolder)dataObject).Path != targetPath;
			}
			return false;
		}
		
		public override void OnMultipleNodeDrop (object[] dataObjects, DragOperation operation)
		{
			Set<SolutionEntityItem> toSave = new Set<SolutionEntityItem> ();
			foreach (object dataObject in dataObjects)
				DropNode (toSave, dataObject, operation);
			IdeApp.ProjectOperations.Save (toSave);
		}
		
		void DropNode (Set<SolutionEntityItem> projectsToSave, object dataObject, DragOperation operation)
		{
			string targetPath = GetFolderPath (CurrentNode.DataItem);
			string what, source;
			Project targetProject = (Project) CurrentNode.GetParentDataItem (typeof(Project), true);
			Project sourceProject;
			System.Collections.Generic.IEnumerable<ProjectFile> groupedChildren = null;
			
			bool ask;
			
			if (dataObject is ProjectFolder) {
				source = ((ProjectFolder) dataObject).Path;
				sourceProject = ((ProjectFolder) dataObject).Project;
				what = Path.GetFileName (source);
				ask = true;
			}
			else if (dataObject is ProjectFile) {
				ProjectFile file = (ProjectFile) dataObject;
				source = file.Name;
				sourceProject = file.Project;
				groupedChildren = file.DependentChildren;
				what = null;
				ask = false;
			} else {
				return;
			}
			
			if (ask) {
				string q;
				if (operation == DragOperation.Move) {
					if (targetPath == targetProject.BaseDirectory)
						q = GettextCatalog.GetString ("Do you really want to move the folder '{0}' to the root folder of project '{1}'?", what, targetProject.Name);
					else
						q = GettextCatalog.GetString ("Do you really want to move the folder '{0}' to the folder '{1}'?", what, Path.GetFileName (targetPath));
					if (!MessageService.Confirm (q, AlertButton.Move))
						return;
				}
				else {
					if (targetPath == targetProject.BaseDirectory)
						q = GettextCatalog.GetString ("Do you really want to copy the folder '{0}' to the root folder of project '{1}'?", what, targetProject.Name);
					else
						q = GettextCatalog.GetString ("Do you really want to copy the folder '{0}' to the folder '{1}'?", what, Path.GetFileName (targetPath));
					if (!MessageService.Confirm (q, AlertButton.Copy))
						return;
				}

			}
			
			ArrayList filesToSave = new ArrayList ();
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.IsDirty) {
					if (doc.FileName == source || doc.FileName.StartsWith (source + Path.DirectorySeparatorChar)) {
						filesToSave.Add (doc);
					} else if (groupedChildren != null) {
						foreach (ProjectFile f in groupedChildren)
							if (doc.FileName == f.Name)
								filesToSave.Add (doc);
					}
				}
			}
			
			if (filesToSave.Count > 0) {
				StringBuilder sb = new StringBuilder ();
				foreach (Document doc in filesToSave) {
					if (sb.Length > 0) sb.Append (",\n");
					sb.Append (Path.GetFileName (doc.FileName));
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
			
			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString("Copying files..."), MonoDevelop.Core.Gui.Stock.CopyIcon, true))
			{
				bool move = operation == DragOperation.Move;
				IdeApp.ProjectOperations.TransferFiles (monitor, sourceProject, source, targetProject, targetPath, move, false);
			}
		}
		
		[CommandHandler (ProjectCommands.AddFiles)]
		public void AddFilesToProject()
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			
			FileSelector fdiag  = new FileSelector (GettextCatalog.GetString ("Add files"));
			fdiag.SetCurrentFolder (GetFolderPath (CurrentNode.DataItem));
			fdiag.SelectMultiple = true;
			
			int result;
			string[] files;
			
			try {
				result = fdiag.Run ();
				files = fdiag.Filenames;
				if (result != (int) ResponseType.Ok)
					return;
			} finally {
				fdiag.Destroy ();
			}
			
			ProjectFolder folder = CurrentNode.GetParentDataItem (typeof(ProjectFolder), true) as ProjectFolder;
			string baseDirectory = folder != null ? folder.Path : project.BaseDirectory;
			
			IdeApp.ProjectOperations.AddFilesToProject (project, files, baseDirectory);
			IdeApp.ProjectOperations.Save (project);
		}
		
		[CommandHandler (ProjectCommands.AddNewFiles)]
		public void AddNewFileToProject()
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			IdeApp.ProjectOperations.CreateProjectFile (project, GetFolderPath (CurrentNode.DataItem));
			IdeApp.ProjectOperations.Save (project);
			CurrentNode.Expanded = true;
		}
		
		void OnFileInserted (ITreeNavigator nav)
		{
			nav.Selected = true;
			Tree.StartLabelEdit ();
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
		
		[CommandHandler (SearchCommands.FindInFiles)]
		public void OnFindInFiles ()
		{
			string path = GetFolderPath (CurrentNode.DataItem);
			SearchReplaceInFilesManager.SearchOptions.SearchDirectory = path;
			SearchReplaceInFilesManager.ShowFindDialog ();
		}
		
		public static string TerminalCommand {
			get {
				return PropertyService.Get ("MonoDevelop.Shell", "gnome-terminal");
			}
		}
		
		[CommandHandler (FileCommands.OpenInTerminal)]
		[AllowMultiSelection]
		public void OnOpenInTerminal ()
		{
			Set<string> paths = new Set<string> ();
			foreach (ITreeNavigator node in CurrentNodes) {
				string path = GetFolderPath (node.DataItem);
				string terminal = TerminalCommand;
				if (paths.Add (path))
					Runtime.ProcessService.StartProcess (terminal, "", path, null);
			}
		}
		
		[CommandHandler (FileCommands.OpenFolder)]
		[AllowMultiSelection]
		public void OnOpenFolder ()
		{
			Set<string> paths = new Set<string> ();
			foreach (ITreeNavigator node in CurrentNodes) {
				string path = GetFolderPath (node.DataItem);
				if (paths.Add (path))
					System.Diagnostics.Process.Start ("file://" + path);
			}
		}
	}	
}
