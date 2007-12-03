
using System;
using Mono.Addins;

namespace TextEditor
{
	public abstract class MenuNode: ExtensionNode
	{
		// Abstract method to be implemented by subclasses, and which
		// should return a menu item.
		public abstract Gtk.MenuItem GetMenuItem ();
	}
}
