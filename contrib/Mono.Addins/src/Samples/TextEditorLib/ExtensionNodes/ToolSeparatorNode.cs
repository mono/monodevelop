
using System;

namespace TextEditor
{
	public class ToolSeparatorNode: ToolbarNode
	{
		public override Gtk.ToolItem GetToolItem ()
		{
			return new Gtk.SeparatorToolItem ();
		}
	}
}
