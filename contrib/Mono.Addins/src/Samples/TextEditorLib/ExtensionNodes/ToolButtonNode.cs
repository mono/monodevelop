
using System;
using Mono.Addins;

namespace TextEditor
{
	public class ToolButtonNode: ToolbarNode
	{
		[NodeAttribute]
		string icon;
		
		[NodeAttribute]
		string commandType;
		
		public override Gtk.ToolItem GetToolItem ()
		{
			Gtk.ToolButton but = new Gtk.ToolButton (icon);
			but.Clicked += OnClicked;
			return but;
		}
		
		void OnClicked (object s, EventArgs a)
		{
			ICommand command = (ICommand) Addin.CreateInstance (commandType);
			command.Run ();
		}
	}
}
