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
		
		public override void Dispose ()
		{
			base.Dispose ();
			Destroy ();
		}
	}
}

