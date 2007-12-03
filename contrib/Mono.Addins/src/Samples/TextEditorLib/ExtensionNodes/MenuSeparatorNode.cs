
using System;
using Mono.Addins;

namespace TextEditor
{
	[ExtensionNode ("MenuSeparator")]
	public class MenuSeparatorNode: MenuNode
	{
		public override Gtk.MenuItem GetMenuItem ()
		{
			return new Gtk.SeparatorMenuItem ();
		}
	}
}
