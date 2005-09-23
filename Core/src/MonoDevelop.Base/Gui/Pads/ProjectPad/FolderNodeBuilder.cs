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
using System.IO;
using System.Collections;
using System.Text;
using Gtk;

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Commands;
using MonoDevelop.Gui.Widgets;

namespace MonoDevelop.Gui.Pads.ProjectPad
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
			
			foreach (ProjectFile file in project.ProjectFiles)
			{
				string dir;
				
				// Resource files are shown in a special resource folder (?!?!).
				if (file.BuildAction == BuildAction.EmbedAsResource)
					continue;

				if (file.Subtype != Subtype.Directory) {
					dir = Path.GetDirectoryName (file.Name);
					if (dir == folder) {
						files.Add (file);
						continue;
					}
				} else
					dir = file.Name;
				
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
			if (project.ProjectFiles.Count > 500)
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
			
			if (dataObject is ProjectFile)
				return Path.GetDirectoryName (((ProjectFile)dataObject).Name) != targetPath;
			if (dataObject is ProjectFolder)
				return ((ProjectFolder)dataObject).Path != targetPath;
			return false;
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			string targetPath = GetFolderPath (CurrentNode.DataItem);
			string what, source, where, how;
			Project targetProject = (Project) CurrentNode.GetParentDataItem (typeof(Project), true);
			Project sourceProject;
			
			bool ask;
			if (operation == DragOperation.Move)
				how = GettextCatalog.GetString ("move");
			else
				how = GettextCatalog.GetString ("copy");
			
			if (dataObject is ProjectFolder) {
				source = ((ProjectFolder) dataObject).Path;
				sourceProject = ((ProjectFolder) dataObject).Project;
				what = string.Format (GettextCatalog.GetString ("the folder '{0}'"), Path.GetFileName(source));
				ask = true;
			}
			else if (dataObject is ProjectFile) {
				source = ((ProjectFile)dataObject).Name;
				sourceProject = ((ProjectFile) dataObject).Project;
				what = string.Format (GettextCatalog.GetString ("the file '{0}'"), Path.GetFileName(source));
				ask = false;
			} else {
				return;
			}
			
			if (targetPath == targetProject.BaseDirectory)
				where = string.Format (GettextCatalog.GetString ("root folder of project '{0}'"), targetProject.Name);
			else
				where = string.Format (GettextCatalog.GetString ("folder '{0}'"), Path.GetFileName (targetPath));
			
			if (ask) {
				if (!Runtime.MessageService.AskQuestion (String.Format (GettextCatalog.GetString ("Do you really want to {0} {1} to {2}?"), how, what, where)))
					return;
			}
			
			using (IProgressMonitor monitor = Runtime.TaskService.GetStatusProgressMonitor (GettextCatalog.GetString("Copying files ..."), Stock.CopyIcon, true))
			{
				bool move = operation == DragOperation.Move;
				Runtime.ProjectService.TransferFiles (monitor, sourceProject, source, targetProject, targetPath, move, false);
			}
			Runtime.ProjectService.SaveCombine();
		}
		
		[CommandHandler (ProjectCommands.AddFiles)]
		public void AddFilesToProject()
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			
			using (FileSelector fdiag  = new FileSelector (GettextCatalog.GetString ("Add files"))) {
				fdiag.SelectMultiple = true;
				
				int result = fdiag.Run ();
				try {
					if (result != (int) ResponseType.Ok)
						return;
					
					int action = -1;
					foreach (string file in fdiag.Filenames) {
						if (file.StartsWith (project.BaseDirectory)) {
							MoveCopyFile (project, CurrentNode, file, true, true);
						} else {
							using (MessageDialog md = new MessageDialog (
								 (Window) WorkbenchSingleton.Workbench,
								 DialogFlags.Modal | DialogFlags.DestroyWithParent,
								 MessageType.Question, ButtonsType.None,
								 String.Format (GettextCatalog.GetString ("{0} is outside the project directory, what should I do?"), file)))
							{
								CheckButton remember = null;
								if (fdiag.Filenames.Length > 1) {
									remember = new CheckButton (GettextCatalog.GetString ("Use the same action for all selected files."));
									md.VBox.PackStart (remember, false, false, 0);
								}
								md.AddButton (Gtk.Stock.Copy, 1);
								md.AddButton (GettextCatalog.GetString ("_Move"), 2);
								md.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
								md.VBox.ShowAll ();
								
								int ret = -1;
								if (action < 0) {
									ret = md.Run ();
									md.Hide ();
									if (ret < 0) return;
									if (remember != null && remember.Active) action = ret;
								} else {
									ret = action;
								}

								try {
									MoveCopyFile (project, CurrentNode, file, ret == 2, false);
								}
								catch (Exception ex) {
									Runtime.MessageService.ShowError (ex, GettextCatalog.GetString ("An error occurred while attempt to move/copy that file. Please check your permissions."));
								}
							}
						}
					}
				} finally {
					fdiag.Hide ();
				}
			}
		}
		
		public static void MoveCopyFile (Project project, ITreeNavigator nav, string filename, bool move, bool alreadyInPlace)
		{
			if (Runtime.FileUtilityService.IsDirectory (filename))
			    return;

			ProjectFolder folder = nav.GetParentDataItem (typeof(ProjectFolder), true) as ProjectFolder;
			
			string name = System.IO.Path.GetFileName (filename);
			string baseDirectory = folder != null ? folder.Path : project.BaseDirectory;
			string newfilename = alreadyInPlace ? filename : Path.Combine (baseDirectory, name);

			if (filename != newfilename) {
				if (File.Exists (newfilename)) {
					if (!Runtime.MessageService.AskQuestion (string.Format (GettextCatalog.GetString ("The file '{0}' already exists. Do you want to replace it?"), newfilename), "MonoDevelop"))
						return;
				}
				File.Copy (filename, newfilename, true);
				if (move)
					Runtime.FileService.RemoveFile (filename);
			}
			
			if (project.IsCompileable (newfilename)) {
				project.AddFile (newfilename, BuildAction.Compile);
			} else {
				project.AddFile (newfilename, BuildAction.Nothing);
			}

			Runtime.ProjectService.SaveCombine();
		}		

		[CommandHandler (ProjectCommands.AddNewFiles)]
		public void AddNewFileToProject()
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			ProjectFile file = Runtime.ProjectService.CreateProjectFile (project, GetFolderPath (CurrentNode.DataItem));
			if (file != null) {
				Runtime.ProjectService.SaveCombine();
				CurrentNode.Expanded = true;
				Tree.AddNodeInsertCallback (file, new TreeNodeCallback (OnFileInserted));
			}
		}
		
		void OnFileInserted (ITreeNavigator nav)
		{
			Tree.StealFocus ();
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
			project.ProjectFiles.Add (newFolder);

			Tree.AddNodeInsertCallback (new ProjectFolder (directoryName, project), new TreeNodeCallback (OnFileInserted));
		}
	}	
}
