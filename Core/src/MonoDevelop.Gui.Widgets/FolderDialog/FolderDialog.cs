//
// Author: John Luke  <jluke@cfl.rr.com>
// License: LGPL
//

using System;
using Gtk;

namespace MonoDevelop.Gui.Widgets
{
	public class FolderDialog : FileSelector
	{
		public FolderDialog (string title) : base (title, FileChooserAction.SelectFolder)
		{
			this.SelectMultiple = false;
		}
	}
}
