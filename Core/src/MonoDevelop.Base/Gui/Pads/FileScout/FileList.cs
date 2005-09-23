// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Gui.Utils;

namespace MonoDevelop.Gui.Pads
{
	public class FileList : Gtk.TreeView
	{
		private FileSystemWatcher watcher;
		private ArrayList Items;
		private Gtk.ListStore store;
		FileListItem selectedItem = null;
		Gtk.TreeIter selectedIter;
		
		public FileList ()
		{
			Items = new ArrayList ();
			store = new Gtk.ListStore (typeof (string), typeof (string), typeof(string), typeof(FileListItem), typeof (Gdk.Pixbuf));
			Model = store;

			HeadersVisible = true;
			HeadersClickable = true;
			Reorderable = true;
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
			
			Gtk.CellRendererText render1 = new Gtk.CellRendererText ();
			name_column.PackStart (render1, false);
			name_column.AddAttribute (render1, "text", 0);
			
			Gtk.CellRendererText render2 = new Gtk.CellRendererText ();
			size_column.PackStart (render2, false);
			size_column.AddAttribute (render2, "text", 1);
			
			Gtk.CellRendererText render3 = new Gtk.CellRendererText ();
			modi_column.PackStart (render3, false);
			modi_column.AddAttribute (render3, "text", 2);
				
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
				
				watcher.Renamed += new RenamedEventHandler(fileRenamed);
				watcher.Deleted += new FileSystemEventHandler(fileDeleted);
				watcher.Created += new FileSystemEventHandler(fileCreated);
				watcher.Changed += new FileSystemEventHandler(fileChanged);
			}
		}
		
		internal void ItemAdded(FileListItem item) {
			store.AppendValues(item.Text, item.Size, item.LastModified, item, item.Icon);
		}
		
		void ItemRemoved (FileListItem item) {
			Runtime.LoggingService.Info ("remove called");
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
			if (Runtime.MessageService.AskQuestion(GettextCatalog.GetString ("Are you sure you want to delete this file?"), GettextCatalog.GetString ("Delete files")))
			{
				try
				{
					File.Delete (selectedItem.FullName);
					ItemRemoved (selectedItem);
				}
				catch (Exception ex)
				{
					Runtime.MessageService.ShowError (ex, "Could not delete file '" + System.IO.Path.GetFileName (selectedItem.FullName) + "'");
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
	}
}
	
