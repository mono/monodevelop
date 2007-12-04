
using System;
using Mono.Addins;

namespace TextEditor
{
	public abstract class ToolbarNode: ExtensionNode
	{
		public abstract Gtk.ToolItem GetToolItem ();
	}
}
