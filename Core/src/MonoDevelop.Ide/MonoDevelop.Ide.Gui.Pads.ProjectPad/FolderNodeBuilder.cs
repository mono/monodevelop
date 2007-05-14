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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Search;

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
			string what, source;
			Project targetProject = (Project) CurrentNode.GetParentDataItem (typeof(Project), true);
			Project sourceProject;
			
			bool ask;
			
			if (dataObject is ProjectFolder) {
				source = ((ProjectFolder) dataObject).Path;
				sourceProject = ((ProjectFolder) dataObject).Project;
				what = Path.GetFileName (source);
				ask = true;
			}
			else if (dataObject is ProjectFile) {
				source = ((ProjectFile)dataObject).Name;
				sourceProject = ((ProjectFile) dataObject).Project;
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
				}
				else {
					if (targetPath == targetProject.BaseDirectory)
						q = GettextCatalog.GetString ("Do you really want to copy the folder '{0}' to the root folder of project '{1}'?", what, targetProject.Name);
					else
						q = GettextCatalog.GetString ("Do you really want to copy the folder '{0}' to the folder '{1}'?", what, Path.GetFileName (targetPath));
				}

				if (!Services.MessageService.AskQuestion (q))
					return;
			}
			
			ArrayList filesToSave = new ArrayList ();
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.IsDirty && (doc.FileName == source || doc.FileName.StartsWith (source + Path.DirectorySeparatorChar)))
					filesToSave.Add (doc);
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
				
				switch (Services.MessageService.AskQuestionWithCancel (question)) {
					case QuestionResponse.Cancel:
						return;
					case QuestionResponse.Yes:
						try {
							foreach (Document doc in filesToSave) {
								doc.Save ();
							}
						} catch (Exception ex) {
							Services.MessageService.ShowError (ex, GettextCatalog.GetString ("Save operation failed."));
							return;
						}
						break;
				}
			}
			
			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString("Copying files..."), MonoDevelop.Core.Gui.Stock.CopyIcon, true))
			{
				bool move = operation == DragOperation.Move;
				IdeApp.ProjectOperations.TransferFiles (monitor, sourceProject, source, targetProject, targetPath, move, false);
			}
			IdeApp.ProjectOperations.SaveCombine();
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
			IProgressMonitor monitor = null;
			
			try {
				result = fdiag.Run ();
				files = fdiag.Filenames;
				if (result != (int) ResponseType.Ok)
					return;
			} finally {
				fdiag.Destroy ();
			}
			
			int action = -1;
			
			if (files.Length > 10) {
				monitor = new MonoDevelop.Core.Gui.ProgressMonitoring.MessageDialogProgressMonitor (true);
				monitor.BeginTask (GettextCatalog.GetString("Adding files..."), files.Length);
			}
			
			using (monitor) {
				
				foreach (string file in files) {
					if (monitor != null)
						monitor.Log.WriteLine (file);
					if (file.StartsWith (project.BaseDirectory)) {
						MoveCopyFile (project, CurrentNode, file, true, true);
					} else {
						using (MessageDialog md = new MessageDialog (
							 IdeApp.Workbench.RootWindow,
							 DialogFlags.Modal | DialogFlags.DestroyWithParent,
							 MessageType.Question, ButtonsType.None,
							 GettextCatalog.GetString ("{0} is outside the project directory, what should I do?", file)))
						{
							CheckButton remember = null;
							if (files.Length > 1) {
								remember = new CheckButton (GettextCatalog.GetString ("Use the same action for all selected files."));
								md.VBox.PackStart (remember, false, false, 0);
							}
							
							int LINK_VALUE = 3;
							int COPY_VALUE = 1;
							int MOVE_VALUE = 2;
							
							md.AddButton (GettextCatalog.GetString ("_Link"), LINK_VALUE);
							md.AddButton (Gtk.Stock.Copy, COPY_VALUE);
							md.AddButton (GettextCatalog.GetString ("_Move"), MOVE_VALUE);
							md.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
							md.VBox.ShowAll ();
							
							int ret = -1;
							if (action < 0) {
								ret = md.Run ();
								md.Hide ();
								if (ret < 0) {
									IdeApp.ProjectOperations.SaveCombine();
									return;
								}
								if (remember != null && remember.Active) action = ret;
							} else {
								ret = action;
							}
							
							try {
								MoveCopyFile (project, CurrentNode, file,
											  (ret == MOVE_VALUE) || (ret == LINK_VALUE), ret == LINK_VALUE);
							}
							catch (Exception ex) {
								Services.MessageService.ShowError (ex, GettextCatalog.GetString ("An error occurred while attempt to move/copy that file. Please check your permissions."));
							}
						}
					}
					if (monitor != null)
						monitor.Step (1);
				}
			}
			IdeApp.ProjectOperations.SaveCombine();
		}
		
		static void MoveCopyFile (Project project, ITreeNavigator nav, string filename, bool move, bool alreadyInPlace)
		{
			if (Runtime.FileService.IsDirectory (filename))
			    return;

			ProjectFolder folder = nav.GetParentDataItem (typeof(ProjectFolder), true) as ProjectFolder;
			
			string name = System.IO.Path.GetFileName (filename);
			string baseDirectory = folder != null ? folder.Path : project.BaseDirectory;
			string newfilename = alreadyInPlace ? filename : Path.Combine (baseDirectory, name);

			if (filename != newfilename) {
				if (File.Exists (newfilename)) {
					if (!Services.MessageService.AskQuestion (GettextCatalog.GetString ("The file '{0}' already exists. Do you want to replace it?", newfilename), "MonoDevelop"))
						return;
				}
				Runtime.FileService.CopyFile (filename, newfilename);
				if (move)
					Runtime.FileService.DeleteFile (filename);
			}
			
			if (project.IsCompileable (newfilename)) {
				project.AddFile (newfilename, BuildAction.Compile);
			} else {
				project.AddFile (newfilename, BuildAction.Nothing);
			}
		}		

		[CommandHandler (ProjectCommands.AddNewFiles)]
		public void AddNewFileToProject()
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			IdeApp.ProjectOperations.CreateProjectFile (project, GetFolderPath (CurrentNode.DataItem));
			IdeApp.ProjectOperations.SaveProject (project);
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
			project.ProjectFiles.Add (newFolder);

			Tree.AddNodeInsertCallback (new ProjectFolder (directoryName, project), new TreeNodeCallback (OnFileInserted));
		}
		
		[CommandHandler (SearchCommands.FindInFiles)]
		public void OnFindInFiles ()
		{
			string path = GetFolderPath (CurrentNode.DataItem);
			SearchReplaceInFilesManager.SearchOptions.SearchDirectory = path;
			SearchReplaceInFilesManager.ShowFindDialog ();
		}
	}	
}
