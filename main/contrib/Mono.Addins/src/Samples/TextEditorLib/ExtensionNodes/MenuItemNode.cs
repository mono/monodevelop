
using System;
using Mono.Addins;

namespace TextEditor
{
	[ExtensionNode ("MenuItem")]
	public class MenuItemNode: MenuNode
	{
		[NodeAttribute]
		string label;
		
		[NodeAttribute]
		string icon;
		
		[NodeAttribute]
		string commandType;
		
		static Gtk.AccelGroup accelGroup = new Gtk.AccelGroup ();
		
		public override Gtk.MenuItem GetMenuItem ()
		{
			Gtk.MenuItem item;
			if (icon != null)
				item = new Gtk.ImageMenuItem (icon, accelGroup);
			else
				item = new Gtk.MenuItem (label);
			item.Activated += OnClicked;
			return item;
		}
		
		void OnClicked (object s, EventArgs a)
		{
			ICommand command = (ICommand) Addin.CreateInstance (commandType);
			command.Run ();
		}
	}
}
