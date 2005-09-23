using System;
using System.IO;
using Gtk;
using Gdk;

namespace MonoDevelop.Gui.Widgets {
	public class FolderEntry : BaseFileEntry {

		public FolderEntry (string name) : base (name)
		{
		}
		
		protected override string ShowBrowseDialog (string name, string start_in)
		{
			FolderDialog fd = new FolderDialog (name);
			if (start_in != null)
				fd.SetFilename (start_in);
			
			int response = fd.Run ();
			
			if (response == (int) ResponseType.Ok) {
				fd.Hide ();
				return fd.Filename;
			}
			fd.Hide ();
			
			return null;
		}
	}
}
