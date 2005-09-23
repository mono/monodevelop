// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Services;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Gui.Utils;

namespace MonoDevelop.Services
{
	internal class DefaultFileService : GuiSyncAbstractService, IFileService
	{
		string currentFile;
		RecentOpen       recentOpen = null;
	
		private class FileInformation
		{
			public IProgressMonitor ProgressMonitor;
			public string FileName;
			public bool BringToFront;
			public int Line;
			public int Column;
		}
		
		public RecentOpen RecentOpen {
			get {
				if (recentOpen == null)
					recentOpen = new RecentOpen ();
				return recentOpen;
			}
		}
		
		public string CurrentFile {
			get {
				return currentFile;
			}
			set {
				currentFile = value;
			}
		}
		
		class LoadFileWrapper
		{
			IDisplayBinding binding;
			Project project;
			FileInformation fileInfo;
			IViewContent newContent;
			
			public LoadFileWrapper(IDisplayBinding binding, FileInformation fileInfo)
			{
				this.fileInfo = fileInfo;
				this.binding = binding;
			}
			
			public LoadFileWrapper(IDisplayBinding binding, Project project, FileInformation fileInfo)
			{
				this.fileInfo = fileInfo;
				this.binding = binding;
				this.project = project;
			}
			
			public void Invoke(string fileName)
			{
				newContent = binding.CreateContentForFile(fileName);
				if (project != null)
				{
					newContent.HasProject = true;
					newContent.Project = project;
				}
				WorkbenchSingleton.Workbench.ShowView (newContent, fileInfo.BringToFront);
				Runtime.Gui.DisplayBindings.AttachSubWindows(newContent.WorkbenchWindow);
				
				if (fileInfo.Line != -1 && newContent is IPositionable) {
					GLib.Timeout.Add (10, new GLib.TimeoutHandler (JumpToLine));
				}
			}
			
			public bool JumpToLine ()
			{
				((IPositionable)newContent).JumpTo (Math.Max(1, fileInfo.Line), Math.Max(1, fileInfo.Column));
				return false;
			}
		}
		
		public IAsyncOperation OpenFile (string fileName)
		{
			return OpenFile (fileName, true);
		}
		
		public IAsyncOperation OpenFile (string fileName, bool bringToFront)
		{
			return OpenFile (fileName, -1, -1, bringToFront);
		}
		
		public IAsyncOperation OpenFile (string fileName, int line, int column, bool bringToFront)
		{
			foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
				if (content.ContentName == fileName || (content.ContentName == null && content.UntitledName == fileName)) {
					if (bringToFront)
						content.WorkbenchWindow.SelectWindow();
					if (line != -1 && content is IPositionable) {
						((IPositionable)content).JumpTo (line, column != -1 ? column : 0);
					}
					return NullAsyncOperation.Success;
				}
			}

			IProgressMonitor pm = Runtime.TaskService.GetStatusProgressMonitor (string.Format (GettextCatalog.GetString ("Opening {0}"), fileName), Stock.OpenFileIcon, false);
			FileInformation openFileInfo = new FileInformation();
			openFileInfo.ProgressMonitor = pm;
			openFileInfo.FileName = fileName;
			openFileInfo.BringToFront = bringToFront;
			openFileInfo.Line = line;
			openFileInfo.Column = column;
			Runtime.DispatchService.GuiDispatch (new StatefulMessageHandler (realOpenFile), openFileInfo);
			return pm.AsyncOperation;
		}
		
		void realOpenFile (object openFileInfo)
		{
			string fileName;
			FileInformation oFileInfo = openFileInfo as FileInformation;
			IProgressMonitor monitor = oFileInfo.ProgressMonitor;

			using (monitor)
			{
				fileName = oFileInfo.FileName;
				
				if (fileName == null) {
					monitor.ReportError (GettextCatalog.GetString ("Invalid file name"), null);
					return;
				}
	
				string origName = fileName;
	
				if (fileName.StartsWith ("file://"))
					fileName = fileName.Substring (7);
	
				if (!fileName.StartsWith ("http://"))
					fileName = System.IO.Path.GetFullPath (fileName);
				
				//Debug.Assert(Runtime.FileUtilityService.IsValidFileName(fileName));
				if (Runtime.FileUtilityService.IsDirectory (fileName)) {
					monitor.ReportError (string.Format (GettextCatalog.GetString ("{0} is a directory"), fileName), null);
					return;
				}
				// test, if file fileName exists
				if (!fileName.StartsWith("http://")) {
					// test, if an untitled file should be opened
					if (!Path.IsPathRooted(origName)) { 
						foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
							if (content.IsUntitled && content.UntitledName == origName) {
								content.WorkbenchWindow.SelectWindow();
								return;
							}
						}
					} else 
					if (!Runtime.FileUtilityService.TestFileExists(fileName)) {
						monitor.ReportError (string.Format (GettextCatalog.GetString ("File not found: {0}"), fileName), null);
						return;
					}
				}
				
				foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
					if (content.ContentName == fileName) {
						if (oFileInfo.BringToFront) {
							content.WorkbenchWindow.SelectWindow();
							if (oFileInfo.Line != -1 && content is IPositionable) {
								((IPositionable)content).JumpTo (oFileInfo.Line, oFileInfo.Column != -1 ? oFileInfo.Column : 0);
							}
						}
						return;
					}
				}
				
				IDisplayBinding binding = Runtime.Gui.DisplayBindings.GetBindingPerFileName(fileName);
				
				if (binding != null) {
					Project project = null;
					Combine combine = null;
					GetProjectAndCombineFromFile (fileName, out project, out combine);
					
					if (combine != null && project != null)
					{
						if (Runtime.FileUtilityService.ObservedLoad(new NamedFileOperationDelegate(new LoadFileWrapper(binding, project, oFileInfo).Invoke), fileName) == FileOperationResult.OK) {
							Runtime.FileService.RecentOpen.AddLastFile (fileName, project.Name);
						}
					}
					else
					{
						if (Runtime.FileUtilityService.ObservedLoad(new NamedFileOperationDelegate(new LoadFileWrapper(binding, null, oFileInfo).Invoke), fileName) == FileOperationResult.OK) {
							Runtime.FileService.RecentOpen.AddLastFile (fileName, null);
						}
					}
				} else {
					try {
						// FIXME: this doesn't seem finished yet in Gtk#
						//MimeType mimetype = new MimeType (new Uri ("file://" + fileName));
						//if (mimetype != null) {
						//	mimetype.DefaultAction.Launch ();
						//} else {
							Gnome.Url.Show ("file://" + fileName);
						//}
					} catch {
						if (Runtime.FileUtilityService.ObservedLoad(new NamedFileOperationDelegate (new LoadFileWrapper (Runtime.Gui.DisplayBindings.LastBinding, null, oFileInfo).Invoke), fileName) == FileOperationResult.OK) {
							Runtime.FileService.RecentOpen.AddLastFile (fileName, null);
						}
					}
				}
			}
		}
		
		protected void GetProjectAndCombineFromFile (string fileName, out Project project, out Combine combine)
		{
			combine = Runtime.ProjectService.CurrentOpenCombine;
			project = null;
			
			if (combine != null)
			{
				foreach (Project projectaux in combine.GetAllProjects())
				{
					if (projectaux.IsFileInProject (fileName))
						project = projectaux;
				}
			}
		}
		
		public void NewFile(string defaultName, string language, string content)
		{
			IDisplayBinding binding = Runtime.Gui.DisplayBindings.GetBindingPerLanguageName(language);
			
			if (binding != null) {
				IViewContent newContent = binding.CreateContentForLanguage(language, content, defaultName);
				if (newContent == null) {
					throw new ApplicationException(String.Format("Created view content was null{3}DefaultName:{0}{3}Language:{1}{3}Content:{2}", defaultName, language, content, Environment.NewLine));
				}
				newContent.UntitledName = defaultName;
				newContent.IsDirty      = false;
				WorkbenchSingleton.Workbench.ShowView(newContent, true);
				
				Runtime.Gui.DisplayBindings.AttachSubWindows(newContent.WorkbenchWindow);
			} else {
				throw new ApplicationException("Can't create display binding for language " + language);				
			}
		}
		
		public IWorkbenchWindow GetOpenFile(string fileName)
		{
			fileName = System.IO.Path.GetFullPath (fileName);
			foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
				// WINDOWS DEPENDENCY : ToUpper()
				if (content.ContentName != null &&
				    content.ContentName.ToUpper() == fileName.ToUpper()) {
					return content.WorkbenchWindow;
				}
			}
			return null;
		}
		
		public void RemoveFile(string fileName)
		{
			if (Directory.Exists(fileName)) {
				try {
					Directory.Delete (fileName, true);
				} catch (Exception e) {
					Runtime.MessageService.ShowError(e, String.Format (GettextCatalog.GetString ("Can't remove directory {0}"), fileName));
					return;
				}
				OnFileRemoved(new FileEventArgs(fileName, true));
			} else {
				try {
					File.Delete(fileName);
				} catch (Exception e) {
					Runtime.MessageService.ShowError(e, String.Format (GettextCatalog.GetString ("Can't remove file {0}"), fileName));
					return;
				}
				OnFileRemoved(new FileEventArgs(fileName, false));
			}
		}
		
		public void RenameFile(string oldName, string newName)
		{
			if (oldName != newName) {
				if (Directory.Exists(oldName)) {
					try {
						Directory.Move(oldName, newName);
					} catch (Exception e) {
						Runtime.MessageService.ShowError(e, String.Format (GettextCatalog.GetString ("Can't rename directory {0}"), oldName));
						return;
					}
					OnFileRenamed(new FileEventArgs(oldName, newName, true));
				} else {
					try {
						File.Move(oldName, newName);
					} catch (Exception e) {
						Runtime.MessageService.ShowError(e, String.Format (GettextCatalog.GetString ("Can't rename file {0}"), oldName));
						return;
					}
					OnFileRenamed(new FileEventArgs(oldName, newName, false));
				}
			}
		}
		
		[FreeDispatch]
		public void CopyFile (string sourcePath, string destPath)
		{
			File.Copy (sourcePath, destPath, true);
			OnFileCreated (new FileEventArgs (destPath, false));
		}

		[FreeDispatch]
		public void MoveFile (string sourcePath, string destPath)
		{
			File.Copy (sourcePath, destPath, true);
			OnFileCreated (new FileEventArgs (destPath, false));
			File.Delete (sourcePath);
			OnFileRemoved (new FileEventArgs (destPath, false));
		}
		
		[FreeDispatch]
		public void CreateDirectory (string path)
		{
			Directory.CreateDirectory (path);
			OnFileCreated (new FileEventArgs (path, true));
		}
		
		public void SaveFile (IWorkbenchWindow window)
		{
			if (window.ViewContent.ContentName == null) {
				SaveFileAs (window);
			} else {
				FileAttributes attr = FileAttributes.ReadOnly | FileAttributes.Directory | FileAttributes.Offline | FileAttributes.System;
				// FIXME
				// bug #59731 is if the file is moved out from under us, we were crashing
				// I changed it to make it ask for a new
				// filename instead, but maybe we should
				// detect the move and update the reference
				// to the name instead
				if (!File.Exists (window.ViewContent.ContentName) || (File.GetAttributes(window.ViewContent.ContentName) & attr) != 0) {
					SaveFileAs (window);
				} else {						
					Runtime.ProjectService.MarkFileDirty (window.ViewContent.ContentName);
					string fileName = window.ViewContent.ContentName;
					// save backup first						
					if((bool) Runtime.Properties.GetProperty ("SharpDevelop.CreateBackupCopy", false)) {
						Runtime.FileUtilityService.ObservedSave (new NamedFileOperationDelegate(window.ViewContent.Save), fileName + "~");
					}
					Runtime.FileUtilityService.ObservedSave (new NamedFileOperationDelegate(window.ViewContent.Save), fileName);
				}
			}
		}
		
		public void SaveFileAs (IWorkbenchWindow window)
		{
			if (window.ViewContent is ICustomizedCommands) {
				if (((ICustomizedCommands)window.ViewContent).SaveAsCommand()) {
					return;
				}
			}
			
			FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Save as..."), Gtk.FileChooserAction.Save);
			fdiag.SetFilename (window.ViewContent.ContentName);
			int response = fdiag.Run ();
			string filename = fdiag.Filename;
			fdiag.Hide ();
		
			if (response == (int)Gtk.ResponseType.Ok) {
				if (!Runtime.FileUtilityService.IsValidFileName (filename)) {
					Runtime.MessageService.ShowMessage(String.Format (GettextCatalog.GetString ("File name {0} is invalid"), filename));
					return;
				}
				// detect preexisting file
				if(File.Exists(filename)){
					if(!Runtime.MessageService.AskQuestion(String.Format (GettextCatalog.GetString ("File {0} already exists.  Overwrite?"), filename))){
						return;
					}
				}
				// save backup first
				if((bool) Runtime.Properties.GetProperty ("SharpDevelop.CreateBackupCopy", false)) {
					Runtime.FileUtilityService.ObservedSave (new NamedFileOperationDelegate(window.ViewContent.Save), filename + "~");
				}
				
				// do actual save
				if (Runtime.FileUtilityService.ObservedSave (new NamedFileOperationDelegate(window.ViewContent.Save), filename) == FileOperationResult.OK) {
					Runtime.FileService.RecentOpen.AddLastFile (filename, null);
				}
			}
		}
		
		protected virtual void OnFileCreated (FileEventArgs e)
		{
			if (FileCreated != null) {
				FileCreated (this, e);
			}
		}
		
		protected virtual void OnFileRemoved (FileEventArgs e)
		{
			if (FileRemoved != null) {
				FileRemoved(this, e);
			}
		}

		protected virtual void OnFileRenamed (FileEventArgs e)
		{
			if (FileRenamed != null) {
				FileRenamed(this, e);
			}
		}

		public event FileEventHandler FileCreated;
		public event FileEventHandler FileRenamed;
		public event FileEventHandler FileRemoved;
	}
}
