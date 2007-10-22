
using System;

namespace TextEditor
{
	public class ExitCommand: ICommand
	{
		public void Run ()
		{
			Gtk.Application.Quit ();
		}
	}
}
