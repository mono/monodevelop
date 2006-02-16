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
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Core.Gui.Utils;

namespace MonoDevelop.Core.Gui
{
	internal class DefaultFileService : AbstractService, IFileService
	{
		string currentFile;
		
		public string CurrentFile {
			get {
				return currentFile;
			}
			set {
				currentFile = value;
			}
		}
		
		public void RemoveFile(string fileName)
		{
			if (Directory.Exists(fileName)) {
				try {
					Directory.Delete (fileName, true);
				} catch (Exception e) {
					Services.MessageService.ShowError(e, String.Format (GettextCatalog.GetString ("Can't remove directory {0}"), fileName));
					return;
				}
				OnFileRemoved(new FileEventArgs(fileName, true));
			} else {
				try {
					File.Delete(fileName);
				} catch (Exception e) {
					Services.MessageService.ShowError(e, String.Format (GettextCatalog.GetString ("Can't remove file {0}"), fileName));
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
						Services.MessageService.ShowError(e, String.Format (GettextCatalog.GetString ("Can't rename directory {0}"), oldName));
						return;
					}
					OnFileRenamed(new FileEventArgs(oldName, newName, true));
				} else {
					try {
						File.Move(oldName, newName);
					} catch (Exception e) {
						Services.MessageService.ShowError(e, String.Format (GettextCatalog.GetString ("Can't rename file {0}"), oldName));
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
		
		public void NotifyFileChange (string path)
		{
			OnFileChanged (new FileEventArgs (path, false));
		}
		
		public void NotifyFileRemove (string path)
		{
			OnFileRemoved (new FileEventArgs (path, false));
		}
		
		public void NotifyFileRename (string path)
		{
			OnFileRenamed (new FileEventArgs (path, false));
		}
		
		public void NotifyFileCreate (string path)
		{
			OnFileCreated (new FileEventArgs (path, false));
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

		protected virtual void OnFileChanged (FileEventArgs e)
		{
			if (FileChanged != null) {
				FileChanged(this, e);
			}
		}

		public event FileEventHandler FileCreated;
		public event FileEventHandler FileRenamed;
		public event FileEventHandler FileRemoved;
		public event FileEventHandler FileChanged;
	}
}
