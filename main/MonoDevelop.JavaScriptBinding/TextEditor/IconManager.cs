using MonoDevelop.Ide;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoDevelop.JavaScript.TextEditor
{
	public class IconManager
	{
		Hashtable icons = new Hashtable ();

		public Gdk.Pixbuf GetIcon (string id)
		{
			Gdk.Pixbuf icon = icons[id] as Gdk.Pixbuf;
			if (icon == null) {
				icon = ImageService.GetPixbuf (id, Gtk.IconSize.Menu);
				icons[id] = icon;
			}
			return icon;
		}
	}
}
