// FileSelector.cs
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
using Gtk;

using MonoDevelop.Core;

namespace MonoDevelop.Components
{
	public class FileSelector : FileChooserDialog
	{
		const string LastPathProperty = "MonoDevelop.FileSelector.LastPath";

		public FileSelector () : this (GettextCatalog.GetString ("Open file..."), FileChooserAction.Open)
		{
		}

		public FileSelector (string title) : this (title, FileChooserAction.Open)
		{
		}
		
		//for some reason GTK# needs this to create wrapper objects
		protected FileSelector (IntPtr ptr) : base (ptr) {}

		public FileSelector (string title, FileChooserAction action) : base (title, null, action)
		{
			switch (action) {
				case FileChooserAction.Open:
					AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
					AddButton (Gtk.Stock.Open, ResponseType.Ok);
					break;
				case FileChooserAction.SelectFolder:
					AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
					AddButton (GettextCatalog.GetString ("Select Folder"), ResponseType.Ok);
					break;
				case FileChooserAction.Save:
					AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
					AddButton (Gtk.Stock.Save, ResponseType.Ok);
					break;
				default:
					break;
			}
			DefaultResponse = ResponseType.Ok;

			CommonSetup ();
		}

		void CommonSetup ()
		{
			// Restore the last active directory
			string last = PropertyService.Get<string> (LastPathProperty);
			if (last != null && last.Length > 0)
				this.SetCurrentFolder (last);
			else
				this.SetCurrentFolder (Environment.GetFolderPath (Environment.SpecialFolder.Personal));

			// add default project path as a MD bookmark
			string pathName = PropertyService.Get ("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", Environment.GetFolderPath (Environment.SpecialFolder.Personal));

			if (FileService.IsDirectory (pathName)) {
				try {
					this.AddShortcutFolder (pathName);
				} catch {
					// This may fail if the folder is already registered, and the ShortcutFolders is not
					// giving the correct values, so there isn't another way to check it.
				}
			}

			// FIXME: only set this once per-dialog
			// perhaps in Dispose ()? or only when a file or dir is selected
			this.CurrentFolderChanged += new EventHandler (OnCurrentFolderChanged);
		}

		void OnCurrentFolderChanged (object o, EventArgs args)
		{
			PropertyService.Set (LastPathProperty, this.CurrentFolder);
		}
	}
}

