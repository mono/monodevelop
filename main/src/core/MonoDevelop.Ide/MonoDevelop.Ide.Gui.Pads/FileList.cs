//  FileList.cs
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
using System.IO;
using System.Collections;

using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui.Pads
{
	internal class FileList : Gtk.TreeView
	{
		private FileSystemWatcher watcher;
		private ArrayList Items;
		private Gtk.ListStore store;
		FileListItem selectedItem = null;
		Gtk.TreeIter selectedIter;
		Gtk.CellRendererText textRender = new Gtk.CellRendererText ();
		
		public FileList ()
		{
			Items = new ArrayList ();
			store = new Gtk.ListStore (typeof (string), typeof (string), typeof(string), typeof(FileListItem), typeof (Gdk.Pixbuf));
			Model = store;

			HeadersVisible = true;
			HeadersClickable = true;
			RulesHint = true;

			Gtk.TreeViewColumn name_column = new Gtk.TreeViewColumn ();
			name_column.Title = GettextCatalog.GetString ("Files");
			
			Gtk.TreeViewColumn size_column = new Gtk.TreeViewColumn ();
			size_column.Title = GettextCatalog.GetString ("Size");

			Gtk.TreeViewColumn modi_column = new Gtk.TreeViewColumn ();
			modi_column.Title = GettextCatalog.GetString ("Last modified");

			Gtk.CellRendererPixbuf pix_render = new Gtk.CellRendererPixbuf ();
			name_column.PackStart (pix_render, false);
			name_column.AddAttribute (pix_render, "pixbuf", 4);
			
			name_column.PackStart (textRender, false);
			name_column.AddAttribute (textRender, "text", 0);
			
			size_column.PackStart (textRender, false);
			size_column.AddAttribute (textRender, "text", 1);
			
			modi_column.PackStart (textRender, false);
			modi_column.AddAttribute (textRender, "text", 2);
				
			AppendColumn(name_column);
			AppendColumn(size_column);
			AppendColumn(modi_column);

			this.PopupMenu += new Gtk.PopupMenuHandler (OnPopupMenu);
			this.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler (OnButtonReleased);
			this.Selection.Changed += new EventHandler (OnSelectionChanged);
			
			watcher = new FileSystemWatcher ();
			
			if(watcher != null) {
				watcher.NotifyFilter = NotifyFilters.FileName;
				watcher.EnableRaisingEvents = false;
				
				watcher.Renamed += (RenamedEventHandler) DispatchService.GuiDispatch (new RenamedEventHandler(fileRenamed));
				watcher.Deleted += (FileSystemEventHandler) DispatchService.GuiDispatch (new FileSystemEventHandler(fileDeleted));
				watcher.Created += (FileSystemEventHandler) DispatchService.GuiDispatch (new FileSystemEventHandler(fileCreated));
				watcher.Changed += (FileSystemEventHandler) DispatchService.GuiDispatch (new FileSystemEventHandler(fileChanged));
			}
		}
		
		internal void ItemAdded(FileListItem item) {
			store.AppendValues(item.Text, item.Size, item.LastModified, item, item.Icon);
		}
		
		void ItemRemoved (FileListItem item) {
			store.Remove (ref selectedIter);
		}
		
		internal void Clear() {
			store.Clear();
		}
		
		void fileDeleted(object sender, FileSystemEventArgs e)
		{
			foreach(FileListItem fileItem in Items)
			{
				if(fileItem.FullName.ToLower() == e.FullPath.ToLower()) {
					Items.Remove(fileItem);
					break;
				}
			}
		}
		
		void fileChanged(object sender, FileSystemEventArgs e)
		{
			foreach(FileListItem fileItem in Items)
			{
				if(fileItem.FullName.ToLower() == e.FullPath.ToLower()) {
					
					FileInfo info = new FileInfo(e.FullPath);
					fileItem.Size = Math.Round((double)info.Length / 1024).ToString() + " KB";
					fileItem.LastModified = info.LastWriteTime.ToString();
					break;
				}
			}
		}
		
		void fileCreated(object sender, FileSystemEventArgs e)
		{
			FileInfo info = new FileInfo (e.FullPath);
			
			FileListItem fileItem = new FileListItem (e.FullPath, Math.Round ((double) info.Length / 1024).ToString () + " KB", info.LastWriteTime.ToString ());
			
			Items.Add (fileItem);
		}
		
		void fileRenamed(object sender, RenamedEventArgs e)
		{
			foreach(FileListItem fileItem in Items)
			{
				if(fileItem.FullName.ToLower() == e.OldFullPath.ToLower()) {
					fileItem.FullName = e.FullPath;
					//fileItem.Text = e.Name;
					break;
				}
			}
		}
		
		private void OnRenameFile (object sender, EventArgs e)
		{
		/*
			if(SelectedItems.Count == 1) {
				//SelectedItems[0].BeginEdit();
			}
		*/
		}
		
		private void OnDeleteFiles (object sender, EventArgs e)
		{
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete this file?"), AlertButton.Delete))
			{
				try
				{
					FileService.DeleteFile (selectedItem.FullName);
					ItemRemoved (selectedItem);
				}
				catch (Exception ex)
				{
					MessageService.ShowException (ex, "Could not delete file '" + System.IO.Path.GetFileName (selectedItem.FullName) + "'");
				} 
			}
		}
		
		private void OnPopupMenu (object o, Gtk.PopupMenuArgs args)
		{
			ShowPopup ();
		}

		private void OnButtonReleased (object o, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3)
				ShowPopup ();
		}

		private void ShowPopup ()
		{
			Gtk.Menu menu = new Gtk.Menu ();

			Gtk.MenuItem deleteFile = new Gtk.MenuItem (GettextCatalog.GetString ("Delete file"));
			deleteFile.Activated += new EventHandler (OnDeleteFiles);

			Gtk.MenuItem renameFile = new Gtk.MenuItem (GettextCatalog.GetString ("Rename file"));
			renameFile.Activated += new EventHandler (OnRenameFile);
			renameFile.Sensitive = false;
			
			menu.Append (deleteFile);
			menu.Append (renameFile);

			menu.Popup (null, null, null, 3, Gtk.Global.CurrentEventTime);
			menu.ShowAll ();
		}

		void OnSelectionChanged (object o, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;

			if (this.Selection.GetSelected (out model, out iter))
			{
				selectedItem = (FileListItem) model.GetValue (iter, 3);
				selectedIter = iter;
			}
		}
		
		internal void SetCustomFont (Pango.FontDescription desc)
		{
			textRender.FontDesc = desc;
		}
	}
}
	
