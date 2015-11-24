//
// Author: John Luke  <jluke@cfl.rr.com>
// License: LGPL
//

using System;

namespace MonoDevelop.Components
{
	class FolderDialog : FileSelector
	{
		public FolderDialog (string title) : base (title, Gtk.FileChooserAction.SelectFolder)
		{
			this.SelectMultiple = false;
		}
	}
}
