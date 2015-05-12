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
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Projects
{
	/// <summary>
	/// Dialog for viewing the project options.
	/// </summary>
	public class ProjectOptionsDialog : MultiConfigItemOptionsDialog
	{
		public ProjectOptionsDialog (Gtk.Window parentWindow, SolutionItem project) : base (parentWindow, project)
		{
			this.Title = GettextCatalog.GetString ("Project Options") + " - " + project.Name;
			this.DefaultWidth = 960;
			this.DefaultHeight = 680;
		}
		
		public static void RenameItem (IWorkspaceFileObject item, string newName)
		{
			if (newName == item.Name)
				return;
			
			if (!NewProjectConfiguration.IsValidSolutionName (newName)) {
				MessageService.ShowError (GettextCatalog.GetString ("Illegal project name.\nOnly use letters, digits, space, '.' or '_'."));
				return;
			}
			
			FilePath oldFile = item.FileName;
			string oldName = item.Name;
			FilePath newFile = oldFile.ParentDirectory.Combine (newName + oldFile.Extension);
			
			// Rename the physical file first as changing the name of an IWorkspaceFileObject
			// can result in the filesystem being probed for a file with that name.
			if (!RenameItemFile (oldFile, newFile))
				return;

			try {
				item.Name = newName;
				item.NeedsReload = false;
				// We renamed it to the wrong thing...
				if (item.FileName != newFile) {
					LoggingService.LogError ("File {0} was renamed to {1} instead of {2}.", item.FileName, item.FileName.FileName, newFile.FileName);
					// File name changed, rename the project file
					if (!RenameItemFile (newFile, item.FileName)) {
						RenameItemFile (newFile, oldFile);
						item.Name = oldName;
						item.NeedsReload = false;
						return;
					}
				}
			} catch (Exception ex) {
				if (File.Exists (item.FileName))
					FileService.RenameFile (item.FileName, oldFile);
				else if (File.Exists (newFile))
					FileService.RenameFile (newFile, oldFile);
				item.Name = oldName;
				MessageService.ShowError (GettextCatalog.GetString ("The project could not be renamed."), ex);
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
