using System;
using Gtk;

using MonoDevelop.Core;

namespace MonoDevelop.Components
{
	public class FileSelector : FileChooserDialog
	{
		const string LastPathProperty = "MonoDevelop.FileSelector.LastPath";
		PropertyService propertyService = (PropertyService) ServiceManager.GetService (typeof (PropertyService));
		FileUtilityService fileUtilityService = (FileUtilityService) ServiceManager.GetService (typeof (FileUtilityService));

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
					AddButton ("Select Folder", ResponseType.Ok);
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
			string last = (string) propertyService.GetProperty (LastPathProperty);
			if (last != null && last.Length > 0)
				this.SetCurrentFolder (last);
			else
				this.SetCurrentFolder (Environment.GetFolderPath (Environment.SpecialFolder.Personal));

			// add default project path as a MD bookmark
			string pathName = propertyService.GetProperty ("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", fileUtilityService.GetDirectoryNameWithSeparator (Environment.GetFolderPath (Environment.SpecialFolder.Personal))).ToString ();

			if (fileUtilityService.IsDirectory (pathName)) {
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
			propertyService.SetProperty (LastPathProperty, this.CurrentFolder);
		}
		
		public override void Dispose ()
		{
			Destroy ();
			base.Dispose ();
		}
	}
}

