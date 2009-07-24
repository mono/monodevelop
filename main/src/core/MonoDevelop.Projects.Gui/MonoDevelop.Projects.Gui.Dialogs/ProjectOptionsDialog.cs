// ProjectOptionsDialog.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.IO;
using System.Collections;
using System.ComponentModel;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui.Dialogs;

namespace MonoDevelop.Projects.Gui.Dialogs {

	/// <summary>
	/// Dialog for viewing the project options.
	/// </summary>
	public class ProjectOptionsDialog : MultiConfigItemOptionsDialog
	{
		public ProjectOptionsDialog (Gtk.Window parentWindow, SolutionEntityItem project) : base (parentWindow, project)
		{
			this.Title = GettextCatalog.GetString ("Project Options") + " - " + project.Name;
		}
		
		public static void RenameItem (IWorkspaceFileObject item, string newName)
		{
			if (newName == item.Name)
				return;
			
			if (!FileService.IsValidFileName (newName)) {
				MessageService.ShowError (GettextCatalog.GetString ("Illegal project name.\nOnly use letters, digits, space, '.' or '_'."));
				return;
			}
			
			FilePath oldFile = item.FileName;
			string oldName = item.Name;
			
			try {
				item.Name = newName;
				item.NeedsReload = false;
				if (oldFile != item.FileName) {
					// File name changed, rename the project file
					if (!RenameItemFile (oldFile, item.FileName)) {
						item.Name = oldName;
						item.NeedsReload = false;
						return;
					}
				}
				else if (oldFile.FileNameWithoutExtension == oldName) {
					FilePath newFile = oldFile.ParentDirectory.Combine (newName + oldFile.Extension);
					if (newFile != oldFile) {
						if (!RenameItemFile (oldFile, newFile)) {
							item.Name = oldName;
							item.NeedsReload = false;
							return;
						}
						item.FileName = newFile;
					}
				}
			} catch (Exception ex) {
				item.Name = oldName;
				MessageService.ShowException (ex, GettextCatalog.GetString ("The project could not be renamed."));
				return;
			}
			item.NeedsReload = false;
		}
		
		static bool RenameItemFile (FilePath oldFile, FilePath newFile)
		{
			if (File.Exists (newFile)) {
				string msg = GettextCatalog.GetString ("The file '{0}' already exist. Do you want to replace it?", newFile.FileName);
				if (!MessageService.Confirm (msg, AlertButton.Replace))
				    return false;
				FileService.DeleteFile (newFile);
			}
			FileService.RenameFile (oldFile, newFile.FileName);
			return true;
		}
	}
}
