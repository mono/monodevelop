//  IncludeFilesDialog.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

using Gtk;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class IncludeFilesDialog: Gtk.Dialog
	{
		public ListStore store;
		
		// regular members
		StringCollection newFiles;
		Project         project;
		
		public IncludeFilesDialog(Project project, StringCollection newFiles)
		{
			Build ();
			
			// set up dialog title
			Title = GettextCatalog.GetString ("Found new files in {0}", project.Name);
			
			newFilesOnlyRadioButton.Active = true;
			this.newFiles = newFiles;
			this.project  = project;
			
			this.InitialiseIncludeFileList();
			
			// wire in event handlers
			okbutton.Clicked += new EventHandler(AcceptEvent);
			cancelbutton.Clicked += new EventHandler(CancelEvent);
			selectAllButton.Clicked += new EventHandler(SelectAll);
			deselectAllButton.Clicked += new EventHandler(DeselectAll);

			// FIXME: I'm pretty sure that these radio buttons 
			// don't actually work in SD 0.98 either, so disabling them
			newFilesOnlyRadioButton.Sensitive = false;
			allFilesRadioButton.Sensitive = false;
		}
		
		#region includeFileListView methods and events 
		
		// initialises and populates the include file list tree view
		private void InitialiseIncludeFileList()
		{
			// set up the list store and treeview
			store = new ListStore (typeof(bool), typeof(string));
			IncludeFileListView.Selection.Mode = SelectionMode.None;
			IncludeFileListView.Model = store;
			CellRendererToggle rendererToggle = new CellRendererToggle ();
			rendererToggle.Activatable = true;
			rendererToggle.Toggled += new ToggledHandler (ItemToggled);
			IncludeFileListView.AppendColumn ("Choosen", rendererToggle, "active", 0);
			IncludeFileListView.AppendColumn ("Name", new CellRendererText (), "text", 1);
			
			// add the found files to the check list box						
			foreach (string file in newFiles) {
				string name = FileService.AbsoluteToRelativePath(project.BaseDirectory, file);
				store.AppendValues (false, name);
			}
		}
		
		private void ItemToggled (object o, ToggledArgs args)
		{
			const int column = 0;
			Gtk.TreeIter iter;
			
			if (store.GetIterFromString(out iter, args.Path)) {
				bool val = (bool) store.GetValue(iter, column);
				store.SetValue(iter, column, !val);
			}
		}
		
		#endregion
		
		void AcceptEvent(object sender, EventArgs e)
		{
			TreeIter first;	
			store.GetIterFirst(out first);
			TreeIter current = first;
 			for (int i = 0; i < store.IterNChildren() ; ++i) {
				// get column raw values
				bool isSelected = (bool) store.GetValue(current, 0);
				string fileName = (string) store.GetValue(current, 1);
			
				// process raw values into actual project details
				string file = FileService.RelativeToAbsolutePath(project.BaseDirectory,fileName);
				ProjectFile finfo = new ProjectFile(file);
				if (isSelected) {
					finfo.BuildAction = project.IsCompileable(file) ? BuildAction.Compile : BuildAction.Nothing;
				} else {
					finfo.BuildAction = BuildAction.Exclude;
				}
				project.Files.Add(finfo);
				
				store.IterNext(ref current);
			}
			
			IdeApp.Workspace.Save ();
			
			Destroy();
		}
		
		void CancelEvent(object sender, EventArgs e)
		{
			Destroy();
		}
		
		void SelectAll(object sender, EventArgs e)
		{
			SetAllCheckedValues(true);
		}
		
		void DeselectAll(object sender, EventArgs e)
		{
			SetAllCheckedValues(false);
		}
		
		private void SetAllCheckedValues(bool value)
		{
			TreeIter first;	
			store.GetIterFirst(out first);
			TreeIter current = first;
 			for (int i = 0; i < store.IterNChildren() ; ++i) {
				store.SetValue(current, 0, value);
				
				store.IterNext(ref current);
			}
		}
		
		public void ShowDialog()
		{
			this.Show ();
		}

	}
}

