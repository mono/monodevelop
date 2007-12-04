
using System;
using Mono.Addins;

namespace TextEditor
{
	[ExtensionNode ("Menu")]
	[ExtensionNodeChild (typeof(MenuItemNode))]
	[ExtensionNodeChild (typeof(MenuSeparatorNode))]
	[ExtensionNodeChild (typeof(SubmenuNode))]
	public class SubmenuNode: MenuNode
	{
		[NodeAttribute]
		string label;
		
		public override Gtk.MenuItem GetMenuItem ()
		{
			Gtk.MenuItem it = new Gtk.MenuItem (label);
			Gtk.Menu submenu = new Gtk.Menu ();
			foreach (MenuNode node in ChildNodes)
				submenu.Insert (node.GetMenuItem (), -1);
			it.Submenu = submenu;
			return it;
		}
	}
}
