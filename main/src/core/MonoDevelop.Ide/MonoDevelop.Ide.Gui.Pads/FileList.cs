//  FileList.cs
//
// Author:
//   John Luke  <jluke@cfl.rr.com>
//
// Copyright (c) 2004 John Luke
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

using MonoDevelop.Core;
using MonoDevelop.Components;

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
            store = new Gtk.ListStore (typeof (string), typeof (string), typeof (string), typeof (FileListItem), typeof (Gdk.Pixbuf));
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

            AppendColumn (name_column);
            AppendColumn (size_column);
            AppendColumn (modi_column);

            this.PopupMenu += new Gtk.PopupMenuHandler (OnPopupMenu);
            this.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler (OnButtonReleased);
            this.Selection.Changed += new EventHandler (OnSelectionChanged);

            watcher = new FileSystemWatcher ();
            watcher.EnableRaisingEvents = false;
            watcher.NotifyFilter = NotifyFilters.FileName;

            watcher.Created += DispatchService.GuiDispatch (new FileSystemEventHandler (fileCreated));
            watcher.Deleted += DispatchService.GuiDispatch (new FileSystemEventHandler (fileDeleted));
            watcher.Changed += DispatchService.GuiDispatch (new FileSystemEventHandler (fileChanged));
            watcher.Renamed += DispatchService.GuiDispatch (new RenamedEventHandler (fileRenamed));
        }

        private void fileCreated (Object o, FileSystemEventArgs e)
        {
            FileInfo fileInfo = new FileInfo (e.FullPath);
            Items.Add(new FileListItem(e.FullPath, String.Format("{0} KB", fileInfo.Length / 512 * 2), fileInfo.LastWriteTime.ToString()));
        }

        private void fileDeleted (Object o, FileSystemEventArgs e)
        {
            foreach (FileListItem fileListItem in Items) {
                if (String.Compare (fileListItem.FullName, e.FullPath, StringComparison.InvariantCultureIgnoreCase) == 0)
                    Items.Remove (fileListItem);
            }
        }

        private void fileChanged (Object o, FileSystemEventArgs e)
        {
            foreach (FileListItem fileListItem in Items) {
                if (String.Compare (fileListItem.FullName, e.FullPath, StringComparison.InvariantCultureIgnoreCase) == 0) {
                    FileInfo info = new FileInfo(e.FullPath);
                    fileListItem.Size = String.Format("{0} KB", info.Length / 512 * 2);
                    fileListItem.LastModified = info.LastWriteTime.ToString ();
                }
            }
        }

        private void fileRenamed (Object o, RenamedEventArgs e)
        {
            foreach (FileListItem fileListItem in Items) {
                if (String.Compare (fileListItem.FullName, e.OldFullPath, StringComparison.InvariantCultureIgnoreCase) == 0) {
                    fileListItem.FullName = e.FullPath;
                }
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
		
		
		
		
		
		private void OnRenameFile (object sender, EventArgs e)
		{
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
	
